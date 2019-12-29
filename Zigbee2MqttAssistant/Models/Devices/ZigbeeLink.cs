using Uno;

namespace Zigbee2MqttAssistant.Models.Devices
{
	[GeneratedImmutable]
	public class ZigbeeLink
	{
		[EqualityKey]
		public string TargetZigbeeId { get; }

		[EqualityHash]
		public byte LinkQuality { get; }

		public byte? Depth { get; }

		public ZigbeeLinkRelationship Relationship { get; } = ZigbeeLinkRelationship.Other;
	}
}
