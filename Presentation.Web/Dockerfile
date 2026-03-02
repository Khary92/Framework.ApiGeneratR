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
COPY ["ApiGeneratR.CodeGen/ApiGeneratR.CodeGen.csproj", "ApiGeneratR.CodeGen/"]
RUN dotnet restore "ApiGeneratR.CodeGen/ApiGeneratR.CodeGen.csproj"
COPY ["ApiGeneratR.CodeGen/", "ApiGeneratR.CodeGen/"]
RUN dotnet build "ApiGeneratR.CodeGen/ApiGeneratR.CodeGen.csproj" -c $BUILD_CONFIGURATION
COPY ["Presentation.Web/Presentation.Web.csproj", "Presentation.Web/"]
COPY ["ApiGeneratR.Definitions/ApiGeneratR.Definitions.csproj", "ApiGeneratR.Definitions/"]
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
