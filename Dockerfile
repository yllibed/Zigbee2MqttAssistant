# ----------- Build stage (ASP .NET Core) -----------
# This file should be run after compiling the solution with the following command:
# msbuild /r /p:Configuration=Release /p:OutputPath=app /t:Publish
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS dotnet-build-env
WORKDIR /src

ENV DOTNET_CLI_TELEMETRY_OPTOUT 1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE 1

# restore
COPY ["Zigbee2MqttAssistant/Zigbee2MqttAssistant.csproj", "Zigbee2MqttAssistant/"]
RUN dotnet restore "Zigbee2MqttAssistant/Zigbee2MqttAssistant.csproj"

# copy src
COPY . .

# build
WORKDIR "/src/Zigbee2MqttAssistant"
RUN dotnet build "Zigbee2MqttAssistant.csproj" -c Release -o /app/build

# publish
RUN dotnet publish "Zigbee2MqttAssistant.csproj" -c Release -o /app/publish


# ----------- Runtime stage -----------
# You should run this file with the following parameters:
# docker build . --build-arg DOTNETTAG=<dotnettag> --build-arg OSTAG=<ostag> -t <image-tag>
# where:
#  <dotnettag> is the tag of the dotnet aspnet runtime image
#  <ostag> is the tag of the runtime for hass.io (amd64, armv7, aarch64...)
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
EXPOSE 80

ENV DOTNET_CLI_TELEMETRY_OPTOUT 1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE 1

ARG DOTNETTAG
ARG OSTAG

# Metadata for information about this software
LABEL description="Zigbee2MqttAssistant - A GUI for Zigbee2Mqtt" author="carl.debilly@gmail.com" "project.url"="https://github.com/yllibed/Zigbee2MqttAssistant"

# Additionnal metadata for HASS.IO
LABEL io.hass.version="172" io.hass.type="addon" io.hass.arch=$OSTAG

# copy file to runtime image
WORKDIR /app
COPY --from=dotnet-build-env /app/publish .
ENTRYPOINT ["dotnet", "Zigbee2MqttAssistant.dll"]
