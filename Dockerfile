# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and projects
COPY ["SecureFix.slnx", "."]
COPY ["src/SecureFix.Core/", "src/SecureFix.Core/"]
COPY ["src/SecureFix.Api/", "src/SecureFix.Api/"]
COPY ["tests/SecureFix.Tests/", "tests/SecureFix.Tests/"]

# Restore and build
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
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "SecureFix.Api.dll"]
