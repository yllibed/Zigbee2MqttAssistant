using System;
using Uno;

namespace Zigbee2MqttAssistant.Models
{
	[GeneratedImmutable]
	public partial class Settings
	{
		/// <summary>
		/// Base MQTT topic for Zigbee2Mqtt
		/// </summary>
		public string BaseTopic { get; } = "zigbee2mqtt";

		/// <summary>
		/// Base MQTT topic for HASS Discovery
		/// </summary>
		/// <remarks>
		/// HASS Discovery MUST be enabled for this assistant to work.
		/// No matter if running on HASS or not.
		/// </remarks>
		public string HomeAssistantDiscoveryBaseTopic { get; } = "homeassistant";

		public TimeSpan PermitJoinTimeout { get; } = TimeSpan.FromMinutes(5);

		/// <summary>
		/// Name or IP of the MQTT server
		/// </summary>
		[EqualityHash]
		public string MqttServer { get; } = "mqtt";

		/// <summary>
		///
		/// </summary>
		public TlsMode MqttSecure { get; } = TlsMode.False;

		/// <summary>
		/// Port to use for MQTT Server if different than default value
		/// (1883 for non-secure, 8883 for secure through TLS)
		/// </summary>
		public ushort? MqttPort { get; }

		/// <summary>
		/// Username to use for MQTT server
		/// </summary>
		public string MqttUsername { get; } = "";

		/// <summary>
		/// Password to use for MQTT server
		/// </summary>
		public string MqttPassword { get; } = "";

		/// <summary>
		/// Threshold to report battery level as low.
		/// Set to zero to disable this feature.
		/// </summary>
		public decimal LowBatteryThreshold { get; } = 30;

		/// <summary>
		/// Number of minutes before turning off the ALLOW JOIN
		/// of of the network. 0=disable this feature.
		/// </summary>
		/// <remarks>
		/// If the allow join is turned on at app launch, it will
		/// wait this time before turning it off, since it's not
		/// possible to know when it was turned on.
		/// </remarks>
		public ushort AllowJoinTimeout { get; } = 20;

		/// <summary>
		/// Let Zigbee2MqttAssistant turn on the last_seen
		/// feature when detected as not activated.
		/// </summary>
		/// <remarks>
		/// The chosen mode is "epoch".
		/// Zigbee2MqttAssistant is compatible with other formats,
		/// it's just this one (epoch) is less parsing to use.
		/// </remarks>
		public bool AutosetLastSeen { get; } = false;

		/// <summary>
		/// Cron expression for polling for devices.
		/// Default value: every 12 minutes.
		/// </summary>
		public string DevicesPollingSchedule { get; } = "*/12 * * * *";

		/// <summary>
		/// Cron expression for polling for devices.
		/// Default value: every 40 minutes.
		/// </summary>
		public string NetworkScanSchedule { get; } = "0 */3 * * *";
	}
}
