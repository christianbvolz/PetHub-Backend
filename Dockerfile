# Build context is the repository root.
# CD workflow: docker/build-push-action with context: .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/PetHub.API/PetHub.API.csproj src/PetHub.API/
RUN dotnet restore src/PetHub.API/PetHub.API.csproj

COPY src/PetHub.API/ src/PetHub.API/
WORKDIR /src/src/PetHub.API
RUN dotnet publish PetHub.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV APPLY_MIGRATIONS=false

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "PetHub.API.dll"]
