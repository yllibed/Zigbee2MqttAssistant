
##############################################################
#
# Zigbee2MqttAssistant - Developement Dockerfile
#
# This file is used ONLY for development. Do not use it for
# CI or to deploy. Should only be used in VisualStudio
#
##############################################################

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
WORKDIR /src
COPY ["Zigbee2MqttAssistant/Zigbee2MqttAssistant.csproj", "Zigbee2MqttAssistant/"]
RUN dotnet restore "Zigbee2MqttAssistant/Zigbee2MqttAssistant.csproj"
COPY . .
WORKDIR "/src/Zigbee2MqttAssistant"
RUN dotnet build "Zigbee2MqttAssistant.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Zigbee2MqttAssistant.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Zigbee2MqttAssistant.dll"]
