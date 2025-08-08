# =========================
#  Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Refresh CA certs (Debian/Ubuntu base)
USER root
# RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates \
#   && update-ca-certificates \
#   && rm -rf /var/lib/apt/lists/*

# ## Updates package lists and installs curl and CA certificates.
# RUN apt update && apt install -y curl ca-certificates

# ## Copy certificates from the build context
# COPY zcert.crt /usr/local/share/ca-certificates/

# ## Update certificates to ensure they are recognized
# RUN update-ca-certificates --fresh

# Copy csproj first for better layer caching
COPY FakeStoreIngestor.csproj ./

# RUN dotnet --info && dotnet nuget list source
# RUN curl -I https://api.nuget.org/v3/index.json || true
# RUN ls -la && echo "----" && cat FakeStoreIngestor.csproj || true

RUN dotnet restore FakeStoreIngestor.csproj

# Copy the rest and publish
COPY . .
RUN dotnet publish FakeStoreIngestor.csproj -c Release -o /app/publish /p:UseAppHost=false

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
