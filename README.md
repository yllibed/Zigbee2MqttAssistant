# Zigbee2Mqtt Assistant
[![All Contributors](https://img.shields.io/badge/all_contributors-22-orange.svg?style=flat-square)](#contributors)

This project is a _Web GUI_ for the very good [Zigbee2Mqtt](https://www.zigbee2mqtt.io/) software
([github sources](https://github.com/Koenkk/zigbee2mqtt)).

## Features
* If you're using zigbee2mqtt for your devices, it's a must.
* Display zigbee devices and the status of each of them.
* Display an interactive map of the network
* Automatically turn off _allow join_ of Zigbee network - no matter how you turned it on (don't need to be turned on from Z2MA). Default is 20 minutes.
* Flexible installation:
  * Available as a _HASS.IO_ add-on (integration into _Home Assistant_). _Ingress_ is supported too.
    note: can be used without Home Assistant.
  * Published as a docker images for following architectures
    * Linux AMD64 (alpine): `linux-x64`
    * Linux ARM32 (buster-slim): `linux-arm32`
    * Linux ARM64 (apline): `linux-arm64`
    * Windows 64 bits (v10.0.17763+): `win-64`
    * Windows ARM32 (v10.0.17763+): `win-arm32`
    * Also published as a multi-arch manifest: `latest` (or `dev` for development version)
* Operations on devices:
  * Allow network join - no more need to setup virtual switches in HA just for that.
  * Rename devices
  * Remove devices from network
  * Bind device to another one (mostly used for Ikea TRÅDFRI devices - [documentation here](https://www.zigbee2mqtt.io/information/binding.html))
  * Visualize device health
* Based on _ASP.NET Core_ 3.0 (LTS).

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

# Branches
There is 2 branches for Zigbee2MqttAssistant: `dev` and `stable`. When a build version is considered stable enough, it will be pushed from `dev` to `stable` (there's no git branch dedicated to the _stable_ version).

If you want to try newest features, you can get the `dev` branch in the following way:
* On HASS.IO, pick `zigbee2mqttassistant-dev` package
* On Docker, pick the following package/tag: `carldebilly/zigbee2mqttassistant:dev`

# Settings

You can refer to [`Settings.cs` file](Zigbee2MqttAssistant/Models/Settings.cs) for more information
on allowed settings. Here's the important settings:

| Field                             | Default           | Usage                                                   |
| --------------------------------- | ----------------- | ------------------------------------------------------- |
| `BaseTopic`                       | `"zigbee2mqtt"`   | Base MQTT topic when Zigbee2Mqtt is publishing its data |
| `HomeAssistantDiscoveryBaseTopic` | `"homeassistant"` | Base MQTT topic for HASS Discovery                      |
| `MqttServer`                      | `"mqtt"`          | Name or IP address of the MQTT server. Put only the name or the address of the server here.  **DON'T USE THE `mqtt://` ADDRESS FORMAT**. |
| `MqttSecure`                      | `false`           | If should use TLS to connect to MQTT server. Valid options are `true`, `false` or `"insecure"`. _Insecure_ means it's using TLS, but without any server certificate check. |
| `MqttPort`                        | `1883` (normal) or `8883` (secured) | Port for MQTT server                  |
| `MqttUsername`                    | `""`              | Username for MQTT server                                |
| `MqttPassword`                    | `""`              | Password for MQTT server                                |
| `LowBatteryThreshold`             | `30`              | Threshold for triggering low-battery warning (%)        |
| `AllowJoinTimeout`                | `20`              | Timeout for turning off _allow join_ of Zigbee network. Set 0 to disable this feature |
| `AutosetLastSeen`                 | `false`           | Will turn on `last_Seen` on Zigbee2Mqtt automatically when detected as off. |
| `DevicesPollingSchedule`          | `*/12 * * * *`    | Schedule (cron expression) for device list refresh. Default value: every 12 minutes. |
| `NetworkScanSchedule`             | `0 */3 * * *`     | Schedule (cron expression) for device list refresh. Default value: every 3 hours. This network scan can have high cost on your network: [details here](https://github.com/Koenkk/zigbee2mqtt/issues/2118#issuecomment-541339790). |

For environment variables, you can use any of the previous fields, prefixed with `Z2MA_SETTINGS__`.  By example, you can specify the `MqttPort` with an environment variable in the following way:
```
Z2MA_SETTINGS__MQTTPORT=11883
Z2MA_SETTINGS__MQTTSECURE=INSECURE
```
Note: Uppercase is used here as a convention. It's actually case insensitive.

If you need to change _cron expression_ for other values, you should use a site like <https://crontab.guru/> to validate them. Attention: if you specify specific hours, take care of the time offset (timezone) inside the container!

# Roadmap
* [X] Build a CI + publish to docker hub
* [X] Shorter environment variables + config file (for docker image)
* [X] Create a `HASS.IO` add-on
  * [X] Support for `HASS.IO` Ingress
  * [X] Automatic update of repo on new version
* [X] Support _Zigbee Bindings_
* [X] Support _Docker Manifest_ (support for ARM + Windows)
* [X] Support mapping of network
* [X] Allow-join auto-off
* [ ] Support _Zigbee groups_
* [ ] Support for device images

# Requirements
* You need a running installation of `Zigbee2Mqtt` v1.5.0+
  * Also tested on v1.6.0, v1.7.0 and v1.7.1
  * Should work with next _herdsman_ version too
* Simple MQTT connection with username/password (TLS supported)
  * Client certificates not supported yet - open an issue if your need it.
* Zigbee2Mqtt required settings:
  * Home Assistant Discovery **MUST** be activated - event if you're not using it.
    This shouldn't have any side effect to your installation.

    **AN ACTUAL INSTALLATION OF HOME ASSISTANT IS NOT REQUIRED**

     To activate: `homeassistant: true` in Zigbee2Mqtt configuration
  * `last_seen` should be activated on Zigbee2Mqtt (any format supported). There's an option (`AutosetLastSeen`) to activate it automatically through MQTT.

# Contributing
* If you have suggestions or find bugs, don't hesitate to open and issue here, on Github.
* **PULL REQUESTS** are welcome! Please open an issue first and link it to your PR. If you're
  unsure about how to implement a feature, we should discuss it in the issue first.

## Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tr>
    <td align="center"><a href="https://github.com/VivantSenior"><img src="https://avatars3.githubusercontent.com/u/49829548?v=4" width="100px;" alt=""/><br /><sub><b>VivantSenior</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/commits?author=VivantSenior" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/alwashe"><img src="https://avatars3.githubusercontent.com/u/15383159?v=4" width="100px;" alt=""/><br /><sub><b>alwashe</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/commits?author=alwashe" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/agreenfield1"><img src="https://avatars3.githubusercontent.com/u/16204747?v=4" width="100px;" alt=""/><br /><sub><b>agreenfield1</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/commits?author=agreenfield1" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/neographikal"><img src="https://avatars1.githubusercontent.com/u/2643715?v=4" width="100px;" alt=""/><br /><sub><b>neographikal</b></sub></a><br /><a href="#ideas-neographikal" title="Ideas, Planning, & Feedback">🤔</a> <a href="https://github.com/yllibed/Zigbee2MqttAssistant/issues?q=author%3Aneographikal" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://github.com/wixoff"><img src="https://avatars1.githubusercontent.com/u/945097?v=4" width="100px;" alt=""/><br /><sub><b>wixoff</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/issues?q=author%3Awixoff" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://github.com/heubi76"><img src="https://avatars0.githubusercontent.com/u/25635057?v=4" width="100px;" alt=""/><br /><sub><b>heubi76</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/issues?q=author%3Aheubi76" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://gadget-freakz.com"><img src="https://avatars3.githubusercontent.com/u/649642?v=4" width="100px;" alt=""/><br /><sub><b>Remco van Geel</b></sub></a><br /><a href="#ideas-remb0" title="Ideas, Planning, & Feedback">🤔</a></td>
  </tr>
  <tr>
    <td align="center"><a href="http://abscond.org"><img src="https://avatars0.githubusercontent.com/u/425?v=4" width="100px;" alt=""/><br /><sub><b>James Darling</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/issues?q=author%3Ajames" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://github.com/nbogojevic"><img src="https://avatars2.githubusercontent.com/u/1485503?v=4" width="100px;" alt=""/><br /><sub><b>Nenad Bogojevic</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/issues?q=author%3Anbogojevic" title="Bug reports">🐛</a> <a href="https://github.com/yllibed/Zigbee2MqttAssistant/commits?author=nbogojevic" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/trekker25"><img src="https://avatars3.githubusercontent.com/u/24300944?v=4" width="100px;" alt=""/><br /><sub><b>trekker25</b></sub></a><br /><a href="#question-trekker25" title="Answering Questions">💬</a></td>
    <td align="center"><a href="https://github.com/brendanmullan"><img src="https://avatars3.githubusercontent.com/u/4569153?v=4" width="100px;" alt=""/><br /><sub><b>Brendan Mullan</b></sub></a><br /><a href="#ideas-brendanmullan" title="Ideas, Planning, & Feedback">🤔</a></td>
    <td align="center"><a href="https://github.com/seaverd"><img src="https://avatars3.githubusercontent.com/u/2743685?v=4" width="100px;" alt=""/><br /><sub><b>seaverd</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/issues?q=author%3Aseaverd" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://github.com/timdonovanuk"><img src="https://avatars0.githubusercontent.com/u/8156439?v=4" width="100px;" alt=""/><br /><sub><b>timdonovanuk</b></sub></a><br /><a href="#ideas-timdonovanuk" title="Ideas, Planning, & Feedback">🤔</a></td>
    <td align="center"><a href="https://www.linkedin.com/in/RafhaanShah/"><img src="https://avatars0.githubusercontent.com/u/16906440?v=4" width="100px;" alt=""/><br /><sub><b>Rafhaan Shah</b></sub></a><br /><a href="#ideas-RafhaanShah" title="Ideas, Planning, & Feedback">🤔</a></td>
  </tr>
  <tr>
    <td align="center"><a href="https://github.com/foXaCe"><img src="https://avatars2.githubusercontent.com/u/290678?v=4" width="100px;" alt=""/><br /><sub><b>foXaCe</b></sub></a><br /><a href="#ideas-foXaCe" title="Ideas, Planning, & Feedback">🤔</a></td>
    <td align="center"><a href="https://github.com/ciotlosm"><img src="https://avatars2.githubusercontent.com/u/7738048?v=4" width="100px;" alt=""/><br /><sub><b>Marius</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/issues?q=author%3Aciotlosm" title="Bug reports">🐛</a> <a href="#ideas-ciotlosm" title="Ideas, Planning, & Feedback">🤔</a></td>
    <td align="center"><a href="http://wol.ph/"><img src="https://avatars0.githubusercontent.com/u/270571?v=4" width="100px;" alt=""/><br /><sub><b>Rick van Hattem</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/issues?q=author%3AWoLpH" title="Bug reports">🐛</a></td>
    <td align="center"><a href="http://godisapj.com"><img src="https://avatars1.githubusercontent.com/u/10796588?v=4" width="100px;" alt=""/><br /><sub><b>PeeJay</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/issues?q=author%3Apejotigrek" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://github.com/jeromelaban"><img src="https://avatars0.githubusercontent.com/u/5839577?v=4" width="100px;" alt=""/><br /><sub><b>Jérôme Laban</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/commits?author=jeromelaban" title="Code">💻</a></td>
    <td align="center"><a href="http://johntdyer.com"><img src="https://avatars3.githubusercontent.com/u/58234?v=4" width="100px;" alt=""/><br /><sub><b>John Dyer</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/issues?q=author%3Ajohntdyer" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://www.djc.me"><img src="https://avatars2.githubusercontent.com/u/826556?v=4" width="100px;" alt=""/><br /><sub><b>Dan Chen</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/commits?author=djchen" title="Code">💻</a></td>
  </tr>
  <tr>
    <td align="center"><a href="https://github.com/dystechnic"><img src="https://avatars0.githubusercontent.com/u/36402924?v=4" width="100px;" alt=""/><br /><sub><b>dystechnic</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/issues?q=author%3Adystechnic" title="Bug reports">🐛</a></td>
    <td align="center"><a href="https://github.com/FutureCow"><img src="https://avatars3.githubusercontent.com/u/2134193?v=4" width="100px;" alt=""/><br /><sub><b>FutureCow</b></sub></a><br /><a href="https://github.com/yllibed/Zigbee2MqttAssistant/issues?q=author%3AFutureCow" title="Bug reports">🐛</a></td>
  </tr>
</table>

<!-- markdownlint-enable -->
<!-- prettier-ignore-end -->
<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!
