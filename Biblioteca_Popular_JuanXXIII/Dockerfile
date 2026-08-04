# 1. Imagen base para correr la app en producción
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

# 2. Imagen pesada con herramientas para compilar el código
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copiamos el archivo de proyecto y restauramos dependencias (NuGet)
COPY ["Biblioteca_Popular_JuanXXIII.csproj", "."]
RUN dotnet restore "./Biblioteca_Popular_JuanXXIII.csproj"
# Copiamos todo el resto del código
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Biblioteca_Popular_JuanXXIII.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 3. Publicamos la app (optimizada para que pese poco)
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Biblioteca_Popular_JuanXXIII.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 4. Metemos la app publicada en la imagen base liviana
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Biblioteca_Popular_JuanXXIII.dll"]