# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["CentralBackend.csproj", "./"]
RUN dotnet restore "CentralBackend.csproj"
COPY . .
RUN dotnet build "CentralBackend.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "CentralBackend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5080
EXPOSE 5080
ENTRYPOINT ["dotnet", "CentralBackend.dll"]
