using System.Net.Http.Json;
using FakeStoreIngestor.Data;
using FakeStoreIngestor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ------------------------
// Configuration
// ------------------------
var cs = builder.Configuration.GetConnectionString("Default")
         ?? throw new InvalidOperationException("ConnectionStrings:Default is missing.");
var baseUrl = builder.Configuration["Ingest:BaseUrl"] ?? "https://fakestoreapi.com/";
var productsEndpoint = builder.Configuration["Ingest:ProductsEndpoint"] ?? "products";

// ------------------------
// Services
// ------------------------
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var serverVersion = ServerVersion.AutoDetect(cs);
    opt.UseMySql(cs, serverVersion);
});

builder.Services.AddHttpClient("fakestore", c =>
{
    c.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "FakeStore Ingestor", Version = "v1" });
});

var app = builder.Build();

// ------------------------
// Database bootstrapping
// ------------------------
// For demos: create schema if it doesn't exist. For production, prefer migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// ------------------------
// Middleware
// ------------------------
app.UseSwagger();
app.UseSwaggerUI();

// ------------------------
// Endpoints
// ------------------------

// Health
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Import/Upsert from FakeStore (all or first N)
app.MapPost("/import/{count?}", async (int? count, IHttpClientFactory httpFactory, AppDbContext db) =>
{
    var http = httpFactory.CreateClient("fakestore");
    var items = await http.GetFromJsonAsync<List<FakeStoreProductDto>>(productsEndpoint);
    if (items is null) return Results.Problem("Failed to fetch products from FakeStore API.");

    if (count is > 0) items = items.Take(count.Value).ToList();

    foreach (var dto in items)
    {
        var existing = await db.Products.Include(p => p.Rating)
                                        .FirstOrDefaultAsync(p => p.Id == dto.id);

        if (existing is null)
        {
            db.Products.Add(new Product
            {
                Id = dto.id,
                Title = dto.title,
                Price = dto.price,
                Description = dto.description,
                Category = dto.category,
                Image = dto.image,
                Rating = new Rating { Rate = dto.rating.rate, Count = dto.rating.count }
            });
        }
        else
        {
            existing.Title = dto.title;
            existing.Price = dto.price;
            existing.Description = dto.description;
            existing.Category = dto.category;
            existing.Image = dto.image;

            existing.Rating ??= new Rating { ProductId = existing.Id };
            existing.Rating.Rate = dto.rating.rate;
            existing.Rating.Count = dto.rating.count;
        }
    }

    var saved = await db.SaveChangesAsync();
    return Results.Ok(new { imported = items.Count, saved });
})
.WithSummary("Fetches products from fakestoreapi.com and stores/updates them in MySQL. Optional 'count' limits records.");

// Query all
app.MapGet("/products", async (AppDbContext db) =>
    await db.Products.AsNoTracking().Include(p => p.Rating).ToListAsync());

// Query by id
app.MapGet("/products/{id:int}", async (int id, AppDbContext db) =>
{
    var p = await db.Products.AsNoTracking().Include(p => p.Rating)
                             .FirstOrDefaultAsync(p => p.Id == id);
    return p is null ? Results.NotFound() : Results.Ok(p);
});

app.Run();
