# Fake Store Ingestor

## Runbook

### Create the project & add packages

```sh
dotnet new webapi -n FakeStoreIngestor -f net8.0
cd FakeStoreIngestor

dotnet clean
rd /s /q bin obj   # on mac/linux: rm -rf bin obj

# dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet remove package Pomelo.EntityFrameworkCore.MySql && \
dotnet remove package Microsoft.EntityFrameworkCore.Relational && \
dotnet remove package Microsoft.EntityFrameworkCore.Design && \
dotnet remove package Microsoft.EntityFrameworkCore && \
dotnet remove package Swashbuckle.AspNetCore

dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.3 && \
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.13 && \
dotnet add package Microsoft.EntityFrameworkCore.Relational --version 8.0.13 && \
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.13 && \
dotnet add package Swashbuckle.AspNetCore



.
.
.

dotnet tool install -g dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update

docker run -d --name mysql-fakestore \
  -e MYSQL_ROOT_PASSWORD=secret \
  -e MYSQL_DATABASE=fakestore \
  -e MYSQL_USER=appuser \
  -e MYSQL_PASSWORD=apppass \
  -p 3306:3306 \
  mysql:8.0

dotnet run


# http://localhost:5171/swagger/index.html

# Import first 5
curl -v -X 'POST' \
  'http://localhost:5171/import/5' \
  -H 'accept: */*'

# Read one
curl -X 'GET' \
  'http://localhost:5171/products/1' \
  -H 'accept: */*'

# List all
curl -X 'GET' \
  'http://localhost:5171/products' \
  -H 'accept: */*'


# Build
docker build -t fakestore-api .
docker build --no-cache -t fakestore-api .

DOCKER_BUILDKIT=1 docker build --progress=plain --no-cache -t fakestore-api .
# or disable BuildKit:
DOCKER_BUILDKIT=0 docker build -t fakestore-api -f ... .


# Run against local MySQL and real FakeStore
docker run -p 8080:8080 \
  -e ConnectionStrings__Default="Server=host.docker.internal;Port=3306;Database=fakestore;User=appuser;Password=apppass;TreatTinyAsBoolean=false;DefaultCommandTimeout=30" \
  -e Ingest__BaseUrl="https://fakestoreapi.com/" \
  -e Ingest__ProductsEndpoint="products" \
  fakestore-api

# Test
curl -s http://localhost:8080/health
curl -s -X POST http://localhost:8080/import/5
curl -s http://localhost:8080/products | jq .

```

```yml docker-compose-snippet
services:
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: rootpass
      MYSQL_DATABASE: fakestore
      MYSQL_USER: appuser
      MYSQL_PASSWORD: apppass
    ports: [ "3306:3306" ]

  mock:
    image: node:20-alpine
    working_dir: /data
    command: sh -c "npm i -g json-server@^0 && json-server --host 0.0.0.0 --port 3000 db.json"
    volumes:
      - ./.github/mock/db.json:/data/db.json:ro
    ports: [ "3000:3000" ]

  api:
    image: fakestore-api
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      ASPNETCORE_URLS: http://0.0.0.0:8080
      ConnectionStrings__Default: "Server=mysql;Port=3306;Database=fakestore;User=appuser;Password=apppass;TreatTinyAsBoolean=false;DefaultCommandTimeout=30"
      # switch to mock easily:
      # Ingest__BaseUrl: "http://mock:3000/"
      # Ingest__ProductsEndpoint: "products"
    ports: [ "8080:8080" ]
    depends_on:
      - mysql
      - mock

```
