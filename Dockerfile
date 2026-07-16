FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore Wyvern/Wyvern.Api/Wyvern.Api.csproj
# Build and publish a release
RUN dotnet publish Wyvern/Wyvern.Api/Wyvern.Api.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /App
COPY --from=build-env /App/out .

# Expose port 8080 (Render default)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Wyvern.Api.dll"]
