FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Companion.Api/Companion.Api.csproj", "Companion.Api/"]
COPY ["Companion.Infrastructure/Companion.Infrastructure.csproj", "Companion.Infrastructure/"]
COPY ["Companion.Core/Companion.Core.csproj", "Companion.Core/"]
RUN dotnet restore "Companion.Api/Companion.Api.csproj"

COPY . .
WORKDIR /src/Companion.Api
RUN dotnet publish "Companion.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Companion.Api.dll"]
