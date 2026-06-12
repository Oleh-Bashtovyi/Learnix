# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Learnix.Backend.slnx ./
COPY Learnix.Domain/Learnix.Domain.csproj Learnix.Domain/
COPY Learnix.Application/Learnix.Application.csproj Learnix.Application/
COPY Learnix.Infrastructure/Learnix.Infrastructure.csproj Learnix.Infrastructure/
COPY Learnix.API/Learnix.API.csproj Learnix.API/
COPY Learnix.Domain.UnitTests/Learnix.Domain.UnitTests.csproj Learnix.Domain.UnitTests/

RUN dotnet restore Learnix.API/Learnix.API.csproj

COPY . .

RUN dotnet publish Learnix.API/Learnix.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Learnix.API.dll"]
