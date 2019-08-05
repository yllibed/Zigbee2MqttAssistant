using System.Diagnostics;
using Uno;

namespace Zigbee2MqttAssistant.Models.Devices
{
	[GeneratedImmutable]
	[DebuggerDisplay("{Name}")]
	public partial class HomeAssistantEntity
	{
		[EqualityHash]
		public string EntityId { get; }
		[EqualityKey]
		public string Component { get; }
		public string Name { get; }
		public string DeviceClass { get; }
	}
}