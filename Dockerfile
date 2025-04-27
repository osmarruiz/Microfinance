FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Microfinance.csproj", "."]
RUN dotnet restore "./Microfinance.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Microfinance.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Microfinance.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev
WORKDIR /app
COPY ["Microfinance.csproj", "."]
RUN dotnet restore "./Microfinance.csproj"
COPY . .
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT [ "dotnet", "watch", "run", "--urls", "http://*:8080" ]

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Microfinance.dll"]
