# Windy Squirrels - Minitwit

## Preface

This project, officially called Minitwit, is part of the course DevOps, Software Evolution and Software Maintenance at the IT University of Copenhagen.

## About the project

Minitwit is a homemade Twitter-like system, allowing users to post and interact with short messages. The platform supports various features to enhance user engagement and account management.


## Our live deployment

We are hosting the project [here](https://windysquirrels.dk/). We use [Hetzner](https://www.hetzner.com/) for the VPS and [Dandomain](https://dandomain.dk/) for domain name services. 

## Local development, testing and deployment
These are methods to run the project locally:

### Development with .NET 10 and Docker

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

### Automated testing with .NET 10 and Docker

Run all tests in the solution by running `dotnet test` in the root folder. 

Test projects are divided into `Unit`, `Integration` and `E2E` tests. 

- Unit tests: A few seconds. No dependencies. 
- Integration tests: 15 to 30 seconds. 
    - Uses Docker to host the database. (Process must have privilege to access Docker.)
- End to End tests: Several minutes and high memory. 
    - Uses Docker to host the site. 
    - Playwright must first be installed using PowerShell. (`pwsh test/Chirp.TestE2E/bin/Debug/net10.0/playwright.ps1 install --with-deps`)

Depending on your testing needs, these are run commands for each test project:

```bash
dotnet test test/Chirp.TestUnit/Chirp.TestUnit.csproj
dotnet test test/Chirp.TestIntegration/Chirp.TestIntegration.csproj
dotnet test test/Chirp.TestE2E/Chirp.TestE2E.csproj
```

### Test deployment using Docker only

Using Docker Compose with the file `docker-compose.dev.yml`, you can host the Postgres database and site locally, as it would be in production. 

### Note about production deployment 

The compose instructions for real production deployments are setup differently.
 
1. An actual server volume is mounted for Postgres. The port to access Postgres is not exposed to The Web.
2. Paths to SSL certificates should be setup for single app deployment, or a reverse proxy should be configured for multi-app deployment.
3. Need to use the standard ports for HTTP and HTTPS, which normally take privileged access to bind. 

## More relevant documentation

[Docker Documentation](/docs/Docker-Documentation.md)
