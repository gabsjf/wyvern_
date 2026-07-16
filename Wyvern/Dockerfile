FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["Wyvern.Api/Wyvern.Api.csproj", "Wyvern.Api/"]
COPY ["Wyvern.Application/Wyvern.Application.csproj", "Wyvern.Application/"]
COPY ["Wyvern.Domain/Wyvern.Domain.csproj", "Wyvern.Domain/"]
COPY ["Wyvern.Infrastructure/Wyvern.Infrastructure.csproj", "Wyvern.Infrastructure/"]

RUN dotnet restore "Wyvern.Api/Wyvern.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Wyvern.Api"
RUN dotnet build "Wyvern.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Wyvern.Api.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "Wyvern.Api.dll"]
