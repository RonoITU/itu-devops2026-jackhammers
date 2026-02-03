# Chirp
## Preface
This project is part of our course on Analysis, Design, and Software Architecture at the IT University of Copenhagen.

This project was originally developed in collaboration with four other contributors as part of our course. After the course ended, I made additional modifications independently. These changes were made solely by me and do not necessarily reflect the views or approval of the other contributors.

## About the project
Chirp is a homemade Twitter-like system, allowing users to post and interact with short messages called cheeps. The platform supports various features to enhance user engagement and account management.


## Live Deployment
Currently there is no live deployment of this application, sorry.
~~The project is hosted on Azure and can be accessed [here](https://bdsagroup07chirprazor.azurewebsites.net/).~~

## Local Deployment
To run the project locally, follow these steps:

1) **Clone the Repository**
```bash
git clone https://github.com/ITU-BDSA2024-GROUP7/Chirp.git
```

2) **Navigate to the Web Application**
```bash
cd Chirp.Web
```

3) **Setup GitHub OAuth**<br>
[Register](https://github.com/settings/applications/new) a new OAuth application, and add the client id and secret to user-secrets
```
OAuth Settings:
Application name: <Whatever you like>
Homepage URL: http://localhost:5273/
Application description: <Whatever you like>
Authorization callback URL: <http://localhost:5273/signin-github>

Set the secret:
cd .\src\Chirp.Web\
dotnet user-secrets init
dotnet user-secrets set "AUTHENTICATION_GITHUB_CLIENTID" "your-client-id"
dotnet user-secrets set "AUTHENTICATION_GITHUB_CLIENTSECRET" "your-client-secret"
```


4) **Run the Application**
```bash
cd .\src\Chirp.Web\
dotnet watch
```

## **Authors & Contributors**
- [@DragonFlyersx](https://github.com/DragonFlyersx)
- [@niko391a](https://github.com/niko391a)  
- [@Nikolaj787](https://github.com/Nikolaj787)  
- [@RasmusAChr](https://github.com/RasmusAChr)
- [@Tipskind](https://github.com/Tipskind)


