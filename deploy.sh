#!/bin/bash
set -ev

TAG=$1
DOCKER_USERNAME=$2
DOCKER_PASSWORD=$3

# Create publish artifact
#dotnet publish -c Release

# Build the Docker images
docker build -t andead/dotnet.jwt:$TAG bin/Release/netcoreapp2.1/.
docker tag andead/dotnet.jwt:$TAG andead/dotnet.jwt:latest

# Login to Docker Hub and upload images
docker login -u="$DOCKER_USERNAME" -p="$DOCKER_PASSWORD"
docker push repository/project:$TAG
docker push repository/project:latest
