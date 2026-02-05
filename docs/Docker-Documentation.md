# Docker Documentation
This documentation will cover how to create an image from a Dockerfile and how to run that image.

## Create an image from Dockerfile
The following command is used to create the image
```
docker build -t <organization>/<application>:<version> .
```
The organization name is `jackhammers` and the application is `minitwit`. 

So far versioning hasn't been taught, so for building an image the following line can be used

```
docker build -t jackhammers/minitwit .
```

When the image has been built, you can view it by using `docker image ls`.

## Run an image with `Docker run`
When the image has been created, you can run the image in a container.
```
docker run -d -p <host port>:<internal port> <image>
```
Flags:
- -d: This makes the container is run in **detached** mode, meaning that it will run in the background.
- -p: This maps the host port to the container port. Currently the Dockerfile serves the app at port 8080,
meaning the app is running on port 8080 on the container. We can then map this port to any port we prefer
on the host. For simplicity this can be mapped to the same port. Hence we get
```
docker run -p 8080:8080 jackhammers/minitwit
```

## Run an image with `Docker Compose`
TBD