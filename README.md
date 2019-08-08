# Zibee2Mqtt Assistant
This project is a _Web GUI_ for the very good [Zigbee2Mqtt system](https://www.zigbee2mqtt.io/) (Z2M).

3 ways to use it:
1. From compiled sources (here)
2. By starting a _docker container_ (not published yet)
3. By installing a _HASS-IO Add-on_ (not published yet)

> # DISCLAIMER
> This is a VERY VERY draf project. There's absolutely no garantee, use it at your own risk.

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

    `homeassistant: true` in Z2M configuration
  * MQTT 
