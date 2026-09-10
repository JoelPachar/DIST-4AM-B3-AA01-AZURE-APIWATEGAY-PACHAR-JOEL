FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos solo el archivo del proyecto y restauramos dependencias
COPY ["vehiculo.api.csproj", "./"]
RUN dotnet restore "vehiculo.api.csproj"

# Copiamos el resto del código
COPY . .

# Compilamos
RUN dotnet build "vehiculo.api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Generamos la imagen final ligera
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "vehiculo.api.dll"]