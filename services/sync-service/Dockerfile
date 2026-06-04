FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /workspace
COPY . .
RUN dotnet publish src/LocalMind.Sync.Api/LocalMind.Sync.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "LocalMind.Sync.Api.dll"]
