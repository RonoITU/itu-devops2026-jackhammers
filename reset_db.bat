@echo off
REM Stop the database container
echo Stopping database container...
docker compose stop db

REM Remove the database container
echo Removing database container...
docker rm db

REM Remove the Docker volume
echo Removing database volume...
docker volume rm itu-devops2026-jackhammers_db_data

REM Start the database with the specific compose file
echo Starting database container...
docker compose -f docker-compose.db.yml up -d

echo Done!
pause