# =========================
# Build Stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Projektdatei kopieren und restore
COPY LawOfWriter.csproj ./
RUN dotnet restore LawOfWriter.csproj

# Restliche Dateien kopieren
COPY . .
RUN dotnet publish LawOfWriter.csproj -c Release -o /app/publish

# =========================
# Runtime Stage (nginx)
# =========================
FROM nginx:alpine AS final

# Default nginx config löschen
RUN rm /etc/nginx/conf.d/default.conf

# Eigene nginx config für SPA
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Publish-Output kopieren
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]