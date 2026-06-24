FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AppaRently.Web.Owner/AppaRently.Web.Owner.csproj", "AppaRently.Web.Owner/"]
COPY ["AppaRently.Infrastructure/AppaRently.Infrastructure.csproj", "AppaRently.Infrastructure/"]
COPY ["AppaRently.Domain/AppaRently.Domain.csproj", "AppaRently.Domain/"]
RUN dotnet restore "AppaRently.Web.Owner/AppaRently.Web.Owner.csproj"
COPY . .
WORKDIR "/src/AppaRently.Web.Owner"
RUN dotnet build "AppaRently.Web.Owner.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AppaRently.Web.Owner.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AppaRently.Web.Owner.dll"]
