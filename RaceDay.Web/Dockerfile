# ---------- Stage 1: build and publish the website ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file first, so the package restore is cached
# and is not repeated every time a source file changes.
COPY RaceDay.Web.csproj ./
RUN dotnet restore

# Copy the rest of the source and publish a release build.
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Stage 2: run the website ----------
# The runtime image is much smaller than the SDK image and does not contain the compiler.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish ./

# Do not run as root inside the container.
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
