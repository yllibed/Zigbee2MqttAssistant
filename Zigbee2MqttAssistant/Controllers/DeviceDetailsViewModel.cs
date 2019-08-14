using System.Collections.Immutable;
using Uno;
using Zigbee2MqttAssistant.Models.Devices;

namespace Zigbee2MqttAssistant.Controllers
{
	[GeneratedImmutable(GenerateEquality = false)]
	public partial class DeviceDetailsViewModel
	{
		[EqualityKey]
		public ZigbeeDevice Device { get; }
		public ImmutableArray<ZigbeeDevice> RouteToCoordinator { get; }
		public bool RouteReachCoordinator { get; }
	}
}
