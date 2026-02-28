# Windy Squirrels - Minitwit

## Preface

This project, officially called Minitwit, is part of the course DevOps, Software Evolution and Software Maintenance at the IT University of Copenhagen.

## About the project

Minitwit is a homemade Twitter-like system, allowing users to post and interact with short messages. The platform supports various features to enhance user engagement and account management.


## Live Deployment

The project is hosted on Azure and can be accessed [here](https://windysquirrels.dk/).

## Local development and deployment
These are methods to run the project locally:

### Develop using dotnet watch with .NET 10 and Docker

This is the "hot reload" for .NET web applications. 

1) **Clone the Repository**

```bash
git clone https://github.com/RonoITU/itu-devops2026-jackhammers
```

2) **Start the database container**

```bash
docker compose -f docker-compose.db.yml up -d
```

3) **Start the Web Application**

```bash
dotnet watch --project ./src/Chirp.Web/
```

### Run tests with .NET 10 and Docker

Run tests for the full solution by calling `dotnet test` in the root folder. 

```bash
dotnet test
```

Test projects are divided into `Unit`, `Integration` and `E2E` tests. 

- Unit tests: A few seconds. No dependencies. 
- Integration tests: 15 to 30 seconds. 
    - Uses Docker to host the database. Process must have privilege to access Docker. 
- End to End tests: Several minutes and high memory. 
    - Uses Docker to host the site. 
    - Playwright must first be installed using PowerShell. (`pwsh test/Chirp.TestE2E/bin/Debug/net10.0/playwright.ps1 install --with-deps`)

### Test deployment using Docker only

Using Docker Compose with the file `docker-compose.dev.yml`, you can host the Postgres database and site locally, as it would be in production. 

### Note about production deployment 

The compose instructions for real production deployments are setup differently.
 
1. An actual server volume is mounted for Postgres. The port to access Postgres is not exposed to The Web.
2. Paths to SSL certificates should be setup for single app deployment, or a reverse proxy should be configured for multi-app deployment.
3. Need to use the standard ports for HTTP and HTTPS, which normally take privileged access to bind. 

## More relevant documentation

[Docker Documentation](/docs/Docker-Documentation.md)
