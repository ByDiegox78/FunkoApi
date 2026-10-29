FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar el csproj en la raíz del contenedor de compilación
COPY ["FunkoApiV.2.csproj", "./"]
RUN dotnet restore "FunkoApiV.2.csproj"

# Copiar el resto del código fuente
COPY . .

# Como ya estamos en /src, compilamos directamente sin entrar a ninguna subcarpeta
RUN dotnet build "FunkoApiV.2.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "FunkoApiV.2.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

RUN mkdir -p /app/data && chown -R app:app /app && chmod -R 777 /app/data

USER app

ENTRYPOINT ["dotnet", "FunkoApiV.2.dll"]