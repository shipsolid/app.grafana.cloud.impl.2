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

```
