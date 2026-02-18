# Certbot and HTTPS Setup

Steps taken to allow `devops-serv1` on Hetzner to use HTTPS as https://windysquirrels.dk/ when running the project in production. 

For this we are using Let's Encrypt certificates obtained through Certbot. Let's Encrypt will issue us a certificate for a 3 month period so long as we can prove current ownership of the domain. (DNS TXT challenge.)

(Also see `prod-compose/docker-compose.devops-serv1.yml` for details on exact environment and dependencies.)

## 1. Install Certbot

```
apt install certbot
```

## 2. Generate certificate

### Command

```
certbot certonly --manual --preferred-challenges dns --debug-challenges -d windysquirrels.dk --key-type rsa
```

- `certonly` : No web-server autoconfiguration. Just get the certificate. 
- `--manual` : Certificate to be validated via a manually managed DNS challenge. 
- `--preferred-challenges dns` : The DNS-01 challenge is preferred. 
- `-d windysquirrels.dk` : Domain the certificate is to match with. (Must prove ownership.)
- `--key-type rsa` : Key type supported by Kestrel servers. Other formats may be usable through a conversion step. 

### DNS-01 Challenge

To prove ownership of the domain, we are issued a challenge in this format: 

```sh
Please deploy a DNS TXT record under the name
_acme-challenge.your-domain.com with the following value:

ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789
```

To solve it, we need access to manage the domain name records through its provider. In this case the provider is Dandomain A/S, where we are using an existing domain. 

Once we confirm that the new record is live on DNS servers, follow through with the prompts on Certbot to get the SSL certificate. 

### Certificate Verification

Check that the SSL certificate is formatted as expected. 

```
openssl rsa -in /etc/letsencrypt/live/windysquirrels.dk/privkey.pem -check
```

## 3. Docker Container Configuration

```YAML
cheep:
    image: jackhammers/minitwit:latest
    container_name: cheep
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://*:8081;http://*:8080
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/cert/live/windysquirrels.dk/fullchain.pem
      - ASPNETCORE_Kestrel__Certificates__Default__KeyPath=/cert/live/windysquirrels.dk/privkey.pem
    volumes:
      - /etc/letsencrypt:/cert
    ports:
      - "443:8081"
      - "80:8080"
    depends_on:
      db:
        condition: service_healthy
```

In this case, we are only running a single application for a single domain. 

- We mount the location of the SSL certificates in the container as a volume. 
- `ASPNETCORE_URLS` : Configures ports to bind for HTTP and HTTPS. (HTTP must still be there for HSTS redirection responses only.)
- `ASPNETCORE_ENVIRONMENT` : Tells ASP.NET Core that this is a production environment. 
- `ASPNETCORE_Kestrel__Certificates__Default__Path`
- `ASPNETCORE_Kestrel__Certificates__Default__KeyPath`
- Expose ports 8080 and 8081, mapping them to the default incoming ports for HTTP and HTTPS traffic respectively. 

> If we had incoming traffic for several apps, going by different domains/subdomains to the same server, we would instead configure a _reverse proxy_ container such as Nginx or Apache.  This would then direct traffic to several app containers or even other servers on the premises!

## 4. Compose Up and Enjoy!

Start your containers up and check your website. 

