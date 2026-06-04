FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /workspace

# Copy slnx and projects
COPY LocalMind.ApiGateway.slnx ./
COPY src/LocalMind.ApiGateway.Domain/LocalMind.ApiGateway.Domain.csproj src/LocalMind.ApiGateway.Domain/
COPY src/LocalMind.ApiGateway.Application/LocalMind.ApiGateway.Application.csproj src/LocalMind.ApiGateway.Application/
COPY src/LocalMind.ApiGateway.Infrastructure/LocalMind.ApiGateway.Infrastructure.csproj src/LocalMind.ApiGateway.Infrastructure/
COPY src/LocalMind.ApiGateway.Api/LocalMind.ApiGateway.Api.csproj src/LocalMind.ApiGateway.Api/

# Restore packages
RUN dotnet restore LocalMind.ApiGateway.slnx

# Copy source code and publish
COPY . .
RUN dotnet publish src/LocalMind.ApiGateway.Api/LocalMind.ApiGateway.Api.csproj -c Release -o /app/publish --no-restore

# Final image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "LocalMind.ApiGateway.Api.dll"]
