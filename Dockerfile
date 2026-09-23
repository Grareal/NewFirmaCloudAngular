# ==========================================
# ETAPA 1 - Compilación Angular
# ==========================================

# Imagen con Node.js para compilar Angular
FROM node:24-alpine AS web

# Directorio de trabajo
WORKDIR /src/web-angular

# Copia dependencias
COPY web-angular/package*.json ./

# Instala paquetes exactamente como aparecen en package-lock.json
RUN npm ci

# Copia código fuente Angular
COPY web-angular/ ./

# Genera el build de producción
RUN npm run build


# ==========================================
# ETAPA 2 - Compilación .NET
# ==========================================

# SDK completo de .NET para compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copia toda la solución
COPY . .

# Restaura paquetes NuGet
RUN dotnet restore FirmaOperaCloud.slnx

# Publica la API en modo Release
RUN dotnet publish src/FirmaOperaCloud.Api/FirmaOperaCloud.Api.csproj \
    -c Release \
    -o /app/publish \
    -p:SkipAngularBuild=true \
    --no-restore

# Copia el build de Angular al directorio wwwroot
# para servir Frontend y API desde el mismo contenedor
COPY --from=web /src/web-angular/dist/web-angular/browser/ /app/publish/wwwroot/


# ==========================================
# ETAPA 3 - Imagen final de ejecución
# ==========================================

# Imagen ligera que contiene únicamente el runtime ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

# Crear usuario sin privilegios (no root)
RUN adduser --disabled-password --home /app --gecos "" appuser

# Copiar archivos publicados desde la etapa build
COPY --from=build --chown=appuser:appuser /app/publish .

# Ejecutar aplicación con usuario restringido
USER appuser

# Puerto donde escuchará Kestrel
ENV ASPNETCORE_HTTP_PORTS=8080

# Documenta el puerto utilizado por la aplicación
EXPOSE 8080

# Punto de entrada del contenedor
ENTRYPOINT ["dotnet", "FirmaOperaCloud.Api.dll"]