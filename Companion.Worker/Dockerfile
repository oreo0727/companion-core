FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Companion.Worker/Companion.Worker.csproj", "Companion.Worker/"]
COPY ["Companion.Infrastructure/Companion.Infrastructure.csproj", "Companion.Infrastructure/"]
COPY ["Companion.Core/Companion.Core.csproj", "Companion.Core/"]
RUN dotnet restore "Companion.Worker/Companion.Worker.csproj"

COPY . .
WORKDIR /src/Companion.Worker
RUN dotnet publish "Companion.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Companion.Worker.dll"]
