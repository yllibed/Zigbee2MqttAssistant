using System.Collections.Immutable;
using Uno;

namespace Zigbee2MqttAssistant.Models.Devices
{
	[GeneratedImmutable]
	public partial class DeviceGroup
	{
		public DeviceGroup(string id, string name, ImmutableArray<string> devices)
		{
			Id = id;
			FriendlyName = name;
			DeviceIds = devices;
		}

		[EqualityHash]
		public string Id { get; }

		public string FriendlyName { get; }

		public ImmutableArray<string> DeviceIds { get; }
	}
}
