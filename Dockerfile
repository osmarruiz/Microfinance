# Base image for the ASP.NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage for building the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Microfinance.csproj", "."]
RUN dotnet restore "./Microfinance.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Microfinance.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage for publishing the service project
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Microfinance.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage for the development environment to enable Hot Reload
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev
WORKDIR /app
# Copia el archivo de proyecto para restore
COPY ["Microfinance.csproj", "."]
RUN dotnet restore "./Microfinance.csproj"
# Copia el resto del código fuente
COPY . .
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080

# Run dotnet watch for Hot Reload
ENTRYPOINT [ "dotnet", "watch", "run", "--urls", "http://*:8080" ]
