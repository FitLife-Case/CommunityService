FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Community.csproj", "."]
RUN dotnet restore "Community.csproj"

COPY . .
RUN dotnet build "Community.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Community.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Community.dll"]