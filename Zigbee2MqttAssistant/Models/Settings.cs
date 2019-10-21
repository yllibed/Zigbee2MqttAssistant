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
	}
}
