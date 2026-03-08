FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "./"]
COPY [".globalconfig", "./"]
COPY ["ApiGeneratR/ApiGeneratR.csproj", "ApiGeneratR/"]
RUN dotnet restore "ApiGeneratR/ApiGeneratR.csproj"
COPY ["ApiGeneratR/", "ApiGeneratR/"]
RUN dotnet build "ApiGeneratR/ApiGeneratR.csproj" -c $BUILD_CONFIGURATION
COPY ["Core.Host/Core.Host.csproj", "Core.Host/"]
COPY ["Core.Application/Core.Application.csproj", "Core.Application/"]
COPY ["Core.Domain/Core.Domain.csproj", "Core.Domain/"]
COPY ["Api.Definitions/Api.Definitions.csproj", "Api.Definitions/"]
COPY ["Infrastructure.Persistence/Infrastructure.Persistence.csproj", "Infrastructure.Persistence/"]
COPY ["Infrastructure.Identity/Infrastructure.Identity.csproj", "Infrastructure.Identity/"]
RUN dotnet restore "Core.Host/Core.Host.csproj"
COPY . .
WORKDIR "/src/Core.Host"
RUN dotnet build "./Core.Host.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Core.Host.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Core.Host.dll"]
