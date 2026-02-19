FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "./"]
COPY ["Shared.Contracts.Generator/Shared.Contracts.Generator.csproj", "Shared.Contracts.Generator/"]
RUN dotnet restore "Shared.Contracts.Generator/Shared.Contracts.Generator.csproj"
COPY ["Shared.Contracts.Generator/", "Shared.Contracts.Generator/"]
RUN dotnet build "Shared.Contracts.Generator/Shared.Contracts.Generator.csproj" -c $BUILD_CONFIGURATION
COPY ["Presentation.Web/Presentation.Web.csproj", "Presentation.Web/"]
COPY ["Api.Definitions/Api.Definitions.csproj", "Api.Definitions/"]
COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
RUN dotnet restore "Presentation.Web/Presentation.Web.csproj"
COPY . .
WORKDIR "/src/Presentation.Web"
RUN dotnet build "./Presentation.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Presentation.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Presentation.Web.dll"]
