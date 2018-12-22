FROM microsoft/dotnet:2.2-aspnetcore-runtime

ADD . /app
WORKDIR /app

ENTRYPOINT ["dotnet", "andead.netcore.jwt.dll"]
