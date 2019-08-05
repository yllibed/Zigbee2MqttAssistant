using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Uno;
using Zigbee2MqttAssistant.Models.Devices;

namespace Zigbee2MqttAssistant.Models.Mqtt
{
	[GeneratedImmutable]
	public partial class Bridge
	{
		/// <summary>
		/// If the bridge is connected to MQTT
		/// </summary>
		public bool Online { get; } = false;

		/// <summary>
		/// Log level of the bridge
		/// </summary>
		public MqttLogLevel LogLevel { get; } = MqttLogLevel.Info;

		/// <summary>
		/// If join is permitted on the bridge
		/// </summary>
		public bool PermitJoin { get; } = false;

		public ImmutableArray<ZigbeeDevice> Devices { get;  } = ImmutableArray<ZigbeeDevice>.Empty;
	}
}
