# Dashboard build stage
FROM node:22-alpine AS web-build
WORKDIR /src/SecureFix.Web

COPY ["src/SecureFix.Web/package.json", "src/SecureFix.Web/package-lock.json", "./"]
RUN npm ci
COPY ["src/SecureFix.Web/", "./"]

ARG VITE_DATA_MODE=live
ARG VITE_AUTH_MODE=demo
ARG VITE_ENTRA_CLIENT_ID=
ARG VITE_ENTRA_TENANT_ID=
ARG VITE_ENTRA_API_SCOPE=
ARG VITE_ENTRA_REDIRECT_URI=
ENV VITE_DATA_MODE=$VITE_DATA_MODE \
    VITE_AUTH_MODE=$VITE_AUTH_MODE \
    VITE_ENTRA_CLIENT_ID=$VITE_ENTRA_CLIENT_ID \
    VITE_ENTRA_TENANT_ID=$VITE_ENTRA_TENANT_ID \
    VITE_ENTRA_API_SCOPE=$VITE_ENTRA_API_SCOPE \
    VITE_ENTRA_REDIRECT_URI=$VITE_ENTRA_REDIRECT_URI
RUN npm run build

# API build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SecureFix.slnx", "."]
COPY ["src/SecureFix.Core/", "src/SecureFix.Core/"]
COPY ["src/SecureFix.Api/", "src/SecureFix.Api/"]
COPY ["tests/SecureFix.Tests/", "tests/SecureFix.Tests/"]
COPY --from=web-build /src/SecureFix.Api/wwwroot/ src/SecureFix.Api/wwwroot/

RUN dotnet restore SecureFix.slnx
RUN dotnet build SecureFix.slnx -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "src/SecureFix.Api/SecureFix.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .

# Create app user for security (image may already define a uid 1000 'app' user)
RUN (id -u app >/dev/null 2>&1 || useradd -m -u 1000 app) && chown -R app:app /app
USER app

EXPOSE 5000
ENV ASPNETCORE_URLS=http://0.0.0.0:5000

ENTRYPOINT ["dotnet", "SecureFix.Api.dll"]
