# syntax=docker/dockerfile:1
# LAB K23 - TV3: image API lab, build multi-stage. Build context = goc repo (can Directory.Build.props, global.json);
# file Dockerfile.dockerignore ben canh chi cho phep dung cac file can thiet vao context.

# ---- Stage 1: build/publish bang SDK (nang, ~1 GB, khong di vao image cuoi)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
# Copy csproj truoc va restore rieng -> layer restore duoc cache khi chi sua code
COPY labs/TV3/Lab.TV3.Api/Lab.TV3.Api.csproj labs/TV3/Lab.TV3.Api/
RUN dotnet restore labs/TV3/Lab.TV3.Api/Lab.TV3.Api.csproj
COPY labs/TV3/Lab.TV3.Api/ labs/TV3/Lab.TV3.Api/
RUN dotnet publish labs/TV3/Lab.TV3.Api/Lab.TV3.Api.csproj -c Release -o /out --no-restore -p:UseAppHost=false

# ---- Stage 2: runtime ASP.NET (nho, khong co SDK/ma nguon)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_gcServer=0
EXPOSE 8080
# Chay bang user khong phai root (APP_UID co san trong image aspnet)
USER $APP_UID
ENTRYPOINT ["dotnet", "Lab.TV3.Api.dll"]
