# =========================
#  Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj first for better layer caching
COPY FakeStoreIngestor/*.csproj ./FakeStoreIngestor/
RUN dotnet restore ./FakeStoreIngestor/FakeStoreIngestor.csproj

# Copy the rest and publish
COPY . .
RUN dotnet publish ./FakeStoreIngestor/FakeStoreIngestor.csproj -c Release -o /app/publish /p:UseAppHost=false

# =========================
#  Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# App listens on 8080 by default
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

# (Optional) default config you can override at runtime
# MySQL connection (override in prod via -e or compose)
ENV ConnectionStrings__Default="Server=localhost;Port=3306;Database=fakestore;User=appuser;Password=apppass;TreatTinyAsBoolean=false;DefaultCommandTimeout=30"
# Upstream API (override in CI to point at mock)
ENV Ingest__BaseUrl="https://fakestoreapi.com/"
ENV Ingest__ProductsEndpoint="products"

# Copy published output
COPY --from=build /app/publish ./

# Healthcheck hits your /health endpoint
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1

# Run
ENTRYPOINT ["dotnet", "FakeStoreIngestor.dll"]
