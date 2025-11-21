FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG TAG_VERSION
WORKDIR /src
COPY ["src/SiteMonitor/SiteMonitor.csproj", "src/SiteMonitor/"]
RUN dotnet restore "src/SiteMonitor/SiteMonitor.csproj"
COPY src/ .
WORKDIR "/src/SiteMonitor"
RUN dotnet build "SiteMonitor.csproj" -p:Version="$TAG_VERSION" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG TAG_VERSION
ARG GITHUB_TOKEN
ARG DESCRIPTION
RUN apt-get update && apt-get dist-upgrade -y && apt-get install zip jq -y

# Generate the executables
RUN dotnet publish "SiteMonitor.csproj" -p:Version="$TAG_VERSION" -c $BUILD_CONFIGURATION -o /app/publish/win-x64 -r win-x64
RUN dotnet publish "SiteMonitor.csproj" -p:Version="$TAG_VERSION" -c $BUILD_CONFIGURATION -o /app/publish/win-x86 -r win-x86
RUN cd /app/publish/win-x64 && zip -r ../windows-x64.zip *
RUN cd /app/publish/win-x86 && zip -r ../windows-x86.zip *

# Create a tag and associate it with a release in GitHub. We don't need to check if it already exist, if it already
# exists the command won't do anything.
RUN curl -L \
      -X POST \
      -H "Accept: application/vnd.github+json" \
      -H "Authorization: Bearer $GITHUB_TOKEN" \
      https://api.github.com/repos/nullinside-development-group/nullinside-site-monitor/releases \
      -d '{"tag_name":"'$TAG_VERSION'","target_commitish":"main","name":"'$TAG_VERSION'","body":"'$DESCRIPTION'","draft":false,"prerelease":false,"generate_release_notes":false}' 

# Upload the files to the release. We need to get the ID for the release we created first and then upload the zips.
# This needs to be done in a single run command to provide the $RELEASE_ID to the rest of the commands. We don't need to
# check if it already exist, if it already exists the command won't do anything.
RUN export RELEASE_ID=$(curl -L \
                          -H "Accept: application/vnd.github+json" \
                          -H "Authorization: Bearer $GITHUB_TOKEN" \
                          "https://api.github.com/repos/nullinside-development-group/nullinside-site-monitor/releases/latest" \
                          | jq .id) && \
    echo "Release ID: "$RELEASE_ID && \
    curl -L \
      -X POST \
      -H "Accept: application/vnd.github+json" \
      -H "Authorization: Bearer $GITHUB_TOKEN" \
      -H "Content-Type: application/octet-stream" \
      "https://uploads.github.com/repos/nullinside-development-group/nullinside-site-monitor/releases/$RELEASE_ID/assets?name=windows-x64.zip" \
      --data-binary "@/app/publish/windows-x64.zip" && \
    curl -L \
    -X POST \
    -H "Accept: application/vnd.github+json" \
    -H "Authorization: Bearer $GITHUB_TOKEN" \
    -H "Content-Type: application/octet-stream" \
    "https://uploads.github.com/repos/nullinside-development-group/nullinside-site-monitor/releases/$RELEASE_ID/assets?name=windows-x86.zip" \
    --data-binary "@/app/publish/windows-x86.zip"