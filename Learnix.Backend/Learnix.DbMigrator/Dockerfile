# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Learnix.Backend.slnx ./
COPY Learnix.Domain/Learnix.Domain.csproj Learnix.Domain/
COPY Learnix.Application/Learnix.Application.csproj Learnix.Application/
COPY Learnix.Infrastructure/Learnix.Infrastructure.csproj Learnix.Infrastructure/
COPY Learnix.API/Learnix.API.csproj Learnix.API/
COPY Learnix.DbMigrator/Learnix.DbMigrator.csproj Learnix.DbMigrator/
COPY Learnix.Domain.UnitTests/Learnix.Domain.UnitTests.csproj Learnix.Domain.UnitTests/

RUN dotnet restore Learnix.DbMigrator/Learnix.DbMigrator.csproj

COPY . .

RUN dotnet publish Learnix.DbMigrator/Learnix.DbMigrator.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

# The DbMigrator expects the Learnix.API configuration files to be in a specific relative path
# because it searches upward for the Learnix.API directory.
# In the Docker container, we'll place them exactly where they are expected.
COPY --chown=appuser:appgroup Learnix.API/appsettings.json /app/Learnix.API/appsettings.json

ENTRYPOINT ["dotnet", "Learnix.DbMigrator.dll"]
