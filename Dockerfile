FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/InventoryManagement.Web/*.csproj src/InventoryManagement.Web/
RUN dotnet restore src/InventoryManagement.Web/InventoryManagement.Web.csproj
COPY src/ src/
RUN dotnet publish src/InventoryManagement.Web -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "InventoryManagement.Web.dll"]
