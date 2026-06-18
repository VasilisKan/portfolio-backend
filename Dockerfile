# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY portfolio-backend.csproj ./
RUN dotnet restore portfolio-backend.csproj

COPY . .
RUN dotnet publish portfolio-backend.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Kestrel listens on plain HTTP inside the container; NPM terminates TLS.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "portfolio-backend.dll"]
