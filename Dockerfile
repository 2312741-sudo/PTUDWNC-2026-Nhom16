# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Directory.Build.props
COPY Directory.Build.props ./

# Copy backend source files (including csproj, packages.lock.json, and code)
COPY src/backend/ src/backend/

# Publish directly
RUN dotnet publish src/backend/CulinaryBlog.API/CulinaryBlog.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# curl cho healthcheck (image aspnet không kèm curl/wget — staging run 1 unhealthy vì lý do này)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

# Default port
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CulinaryBlog.API.dll"]
