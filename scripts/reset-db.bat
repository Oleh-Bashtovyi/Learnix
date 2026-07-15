@echo off
cd /d "%~dp0.."

echo =========================================
echo       Resetting PostgreSQL Database
echo =========================================

echo 1. Stopping postgres container...
docker stop learnix-postgres

echo 2. Removing postgres container...
docker rm learnix-postgres

echo 3. Removing postgres data volume...
docker volume rm learnix_postgres_data

echo 4. Starting postgres container again...
docker compose up -d postgres

echo 5. Waiting for postgres to initialize (5 seconds)...
timeout /t 5 /nobreak

echo 6. Running DbMigrator with Development profile...
cd Learnix.Backend\Learnix.DbMigrator
dotnet run --launch-profile Development

echo =========================================
echo       Database Reset Completed!
echo =========================================
pause
