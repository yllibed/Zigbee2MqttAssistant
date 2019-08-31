# Zibee2Mqtt Assistant
This project is a _Web GUI_ for the very good [Zigbee2Mqtt](https://www.zigbee2mqtt.io/) software
([github sources](https://github.com/Koenkk/zigbee2mqtt)).

## Features
* If you're using zigbee2mqtt for your devices, it's a must.
* Display zigbee devices and the status of each of them.
* Flexible installation:
  * Available as a _HASS.IO_ add-on (integration into _Home Assistant_). _Ingress_ is supported too.
    note: can be used without Home Assistant.
  * Published as a docker image (Linux & Windows, both for x64 + ARMv7)
* Operations on devices:
  * Allow network join - no more need to setup virtual switches in HA just for that.
  * Rename devices
  * Remove devices from network
  * Bind device to another one (mostly used for Ikea TRÅDFRI devices - [documentation here](https://www.zigbee2mqtt.io/information/binding.html))
  * Visualize device health
* Based on _ASP.NET Core_ 2.2.

[![Build Status](https://dev.azure.com/yllibed/Zigbee2MqttAssistant/_apis/build/status/yllibed.Zigbee2MqttAssistant?branchName=master)](https://dev.azure.com/yllibed/Zigbee2MqttAssistant/_build/latest?definitionId=4&branchName=master)
[![Release Status](https://vsrm.dev.azure.com/yllibed/_apis/public/Release/badge/35f7fc7c-f867-48e4-83b5-3381156a439a/1/1)](https://dev.azure.com/yllibed/Zigbee2MqttAssistant/_release?view=mine&definitionId=1)
[![Docker Pulls](https://img.shields.io/docker/pulls/carldebilly/zigbee2mqttassistant)](https://hub.docker.com/r/carldebilly/zigbee2mqttassistant)

# Screenshots
![](images/devices-list.png)
![](images/device-page.png)
![](images/status-page.png)

# Installation

## OPTION 1 - Installing as `HASS.IO` Add-on
1. Add the following repository url in HASS.IO:
   ```
   https://github.com/yllibed/hassio
   ```
2. Install `Zigbee2Mq2ttAssistant`
3. Configure your credentials for your MQTT server
4. Enjoy!

## OPTION 2 - Installing from docker
Run the following command by replacing `<mqttserver>`, `<mqttusername>`, `<mqttpassword>` with your correct values.
```bash
docker run -p 8880:80 -e Z2MA_SETTINGS__MQTTSERVER=<mqttserver> -e Z2MA_SETTINGS__MQTTUSERNAME=<mqttusername> -e Z2MA_SETTINGS__MQTTPASSWORD=<mqttpassword> --restart always carldebilly/zigbee2mqttassistant
```
> **draft note**: environment variables are currently the easiest way to set those settings.
> Open an issue if you need it to be in a configuration file/folder.

## OPTION 3 - Installing from sources & compiling using Visual Studio
1. Compile the solution file
2. Adjust settings in `appsettings.json` for your MQTT connection

> Note: it won't compile using the _dotnet core_ build yet. For now, MSBuild is required to build it.

# Settings

You can refer to [`Settings.cs` file](Zigbee2MqttAssistant/Models/Settings.cs) for more information
on allowed settings. Here's the important settings:

| Field                             | Default           | Usage                                                   |
| --------------------------------- | ----------------- | ------------------------------------------------------- |
| `BaseTopic`                       | `"zigbee2mqtt"`   | Base MQTT topic when Zigbee2Mqtt is publishing its data |
| `HomeAssistantDiscoveryBaseTopic` | `"homeassistant"` | Base MQTT topic for HASS Discovery                      |
| `MqttServer`                      | `"mqtt"`          | Name or IP address of the MQTT server                   |
| `MqttSecure`                      | `false`           | If should use TLS to connect to MQTT server             |
| `MqttPort`                        | `1883` (normal) or `8883` (secured) | Port for MQTT server                  |
| `MqttUsername`                    | `""`              | Username for MQTT server                                |
| `MqttPassword`                    | `""`              | Password for MQTT server                                |
| `LowBatteryThreshold`             | `30`              | Threshold for triggering low-battery warning (%)        |

# Roadmap
* [X] Build a CI + publish to docker hub
* [X] Shorter environment variables + config file (for docker image)
* [X] Create a `HASS.IO` add-on
  * [X] Support for `HASS.IO` Ingress
  * [X] Automatic update of repo on new version
* [X] Support _Zigbee Bindings_
* [X] Support _Docker Manifest_ (support for ARM + Windows)
* [ ] Support _Zigbee groups_ **WAITING FOR NEXT VERSION OF ZIGBEE2MQTT FOR THIS ONE**
* [ ] Better display of "routes to coordinator"
* [ ] Improve UI

# Requirements
* You need a running installation of `Zigbee2Mqtt` v1.5.0+
  * Developped & tested with Zigbee2Mqtt v1.5.1
* Simple MQTT connection with username/password (TLS supported)
  * Client certificates not supported yet - open an issue if your need it.
* Zigbee2Mqtt required settings:
  * Home Assistant Discovery **MUST** be activated - event if you're not using it.
    This shouldn't have any side effect to your installation.

    **AN ACTUAL INSTALLATION OF HOME ASSISTANT IS NOT REQUIRED**

     To activate: `homeassistant: true` in Zigbee2Mqtt configuration
  * `last_seen` should be set to `ISO_8601`. Not required, but you'll have a better experience when activated.

# Contributing
* If you have suggestions or find bugs, don't hesitate to open and issue here, on Github.
* **PULL REQUESTS** are welcome! Please open an issue first and link it to your PR. If you're
  unsure about how to implement a feature, we should discuss it in the issue first.
