# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Directory.Build.props
COPY Directory.Build.props ./

# Copy csproj files for layer caching
COPY src/backend/CulinaryBlog.Domain/CulinaryBlog.Domain.csproj src/backend/CulinaryBlog.Domain/
COPY src/backend/CulinaryBlog.Application/CulinaryBlog.Application.csproj src/backend/CulinaryBlog.Application/
COPY src/backend/CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj src/backend/CulinaryBlog.Infrastructure/
COPY src/backend/CulinaryBlog.API/CulinaryBlog.API.csproj src/backend/CulinaryBlog.API/

# Restore dependencies
RUN dotnet restore src/backend/CulinaryBlog.API/CulinaryBlog.API.csproj

# Copy backend source files
COPY src/backend/ src/backend/

# Publish
RUN dotnet publish src/backend/CulinaryBlog.API/CulinaryBlog.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Default port
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CulinaryBlog.API.dll"]
