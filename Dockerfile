FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
EXPOSE 80
LABEL description="Zigbee2MqttAssistant - A GUI for Zigbee2Mqtt"
LABEL author="carl.debilly@gmail.com"
LABEL "project.url"="https://github.com/yllibed/Zigbee2MqttAssistant"

WORKDIR /app
# This is the result of a previously built project using this command line in the same folder:
# msbuild /r /p:Configuration=Release /p:OutputPath=app /t:Publish
COPY Zigbee2MqttAssistant/apppublish .
ENTRYPOINT ["dotnet", "Zigbee2MqttAssistant.dll"]