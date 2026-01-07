FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY Jellyfin.Plugin.AniDB.sln ./
COPY Directory.Build.props ./
COPY Jellyfin.Plugin.AniDB/Jellyfin.Plugin.AniDB.csproj Jellyfin.Plugin.AniDB/
RUN dotnet restore Jellyfin.Plugin.AniDB/Jellyfin.Plugin.AniDB.csproj

COPY . .
RUN dotnet publish Jellyfin.Plugin.AniDB/Jellyfin.Plugin.AniDB.csproj --configuration Release --output /out

FROM busybox:1.36 AS artifact
WORKDIR /plugin
COPY --from=build /out/ ./
