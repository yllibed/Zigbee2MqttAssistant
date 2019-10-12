# This file should be run after compiling the solution with the following command:
# msbuild /r /p:Configuration=Release /p:OutputPath=app /t:Publish

# You should run this file with the following parameters:
# docker build . --build-arg DOTNETTAG=<dotnettag> --build-arg OSTAG=<ostag> -t <image-tag>
# where:
#  <dotnettag> is the tag of the dotnet aspnet runtime image
#  <ostag> is the tag of the runtime for hass.io (amd64, armv7, aarch64...)

ARG DOTNETTAG
ARG OSTAG

FROM mcr.microsoft.com/dotnet/core/aspnet:$DOTNETTAG
EXPOSE 80

# Metadata for information about this software
LABEL description="Zigbee2MqttAssistant - A GUI for Zigbee2Mqtt" author="carl.debilly@gmail.com" "project.url"="https://github.com/yllibed/Zigbee2MqttAssistant"

# Additionnal metadata for HASS.IO
LABEL io.hass.version="172" io.hass.type="addon" io.hass.arch=$OSTAG

WORKDIR /app
COPY Zigbee2MqttAssistant/apppublish .
ENTRYPOINT ["dotnet", "Zigbee2MqttAssistant.dll"]
