# Telemetry in Zigbee2MqttAssistant

The [Zigbee2MqttAssistant](https://github.com/yllibed/Zigbee2MqttAssistant) includes a
[telemetry feature]() that collects usage information.

The collected data is anonymous.

The telemetry behavior is based on the [.NET Core telemetry feature](https://docs.microsoft.com/en-us/dotnet/core/tools/telemetry).

## How to opt out

The Zigbee2MqttAssistant telemetry is enabled by default.  To opt out of the telemetry feature, set the
`TelemetryOptOut` feature to "1" or "true".

Using docker, you may specify the environment variable `Z2MA_SETTINGS__TELEMETRYOPTOUT=true`.

Using HASS.IO or any json configuration file, you may add this to the settings: `"TelemetryOptOut": true`.

## Collected data

The telemetry feature doesn't collect personal data, such as usernames or passwords. It doesn't scan your
machine nor your network to extract more information.  All the code responsible for the telemetry is
located in this file and you can review it if you want:
[Telemetry.cs](https://github.com/yllibed/Zigbee2MqttAssistant/tree/master/Zigbee2MqttAssistant/Telemetry.cs).

The telemetry feature collects the following data:
* Current date & time
* Compiled version of Zigbee2MqttAssistant
* OS & processor type (Windows/Linux) (amd64/arm)
* Installation type (Docker or HASS.IO)
* Versions: HASS.IO, Zigbee2Mqtt
* Crash reports with anonymized details
