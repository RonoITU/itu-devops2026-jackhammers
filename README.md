# Windy Squirrels - Minitwit
## Preface
This project, officially called Minitwit, is part of the course DevOps, Software Evolution and Software Maintenance at the IT University of Copenhagen.

## About the project
Minitwit is a homemade Twitter-like system, allowing users to post and interact with short messages. The platform supports various features to enhance user engagement and account management.


## Live Deployment
The project is hosted on Azure and can be accessed [here](https://windysquirrels.dk/).

## Local Deployment
To run the project locally, follow these steps:

1) **Clone the Repository**
```bash
git clone https://github.com/RonoITU/itu-devops2026-jackhammers
```

2) **Start the database container**
```bash
docker compose -f .\src\Chirp.Database\docker-compose.yml up -d
```

3) **Navigate to the Web Application and run**
```bash
cd src/Chirp.Web/
dotnet watch
```

## Run Tests
To run the tests for the project, navigate to the test project directory and execute the following command:
```bash
cd src/Chirp.Tests/
dotnet test
```

## More relevant documentation
[Docker Documentation](/docs/Docker-Documentation.md)