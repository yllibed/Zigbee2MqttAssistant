# Zibee2Mqtt Assistant
This project is a _Web GUI_ for the very good [Zigbee2Mqtt](https://www.zigbee2mqtt.io/) software
([github sources](https://github.com/Koenkk/zigbee2mqtt)).

3 ways to use it:
1. From compiled sources (here)
2. By starting a _docker container_ (not published yet)
3. By installing a _HASS-IO Add-on_ (not published yet)

> # DISCLAIMER
> This is a VERY VERY draf project. There's absolutely no garantee, use it at your own risk.

## Installation

1. Compile the solution file
2. Adjust settings in `appsettings.json` for your MQTT connection

## Settings

You can refer to [`Settings.cs` file](Zigbee2MqttAssistant/Models/Settings.cs) for more information
on allowed settings.

## Features
* Display all joined devices, event those unsupported by Zigbee2Mqtt
* Display staled devices
* Let you rename a device easily
* Let you remove a device easily
* Activate / deactivate ALLOW JOIN on Zigbee - no need to setup virtual switches in HA just for that.

## Roadmap
* Build a CI + publish to docker hub
* Create a HASS-IO add-on
* Support _Zigbee Bindings_ & _groups_
* Better display of "routes to coordinator"

## Requirements
* Simple MQTT connection with username/password (TLS supported)
  * Client certificates not supported yet
* Z2M Configuration:
  * Home Assistant Discovery **MUST** be activated - event if you're not using it.
    This shouldn't have any side effect to your installation.

    To activate: `homeassistant: true` in Z2M configuration
  * `last_seen` should be activated. Will work without, but you'll have a better experience.
  * Tested with Zigbee2Mqtt v1.5.1
