using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Uno;

namespace Zigbee2MqttAssistant.Models.Devices
{
	[GeneratedImmutable]
	[DebuggerDisplay("{ZigbeeId}-{FriendlyName}")]
	public partial class ZigbeeDevice
	{
		[EqualityHash]
		public string ZigbeeId { get; }

		[EqualityHash]
		public uint? NetworkAddress { get; }

		public ushort LinkQuality => Parents
			//.Where(p => p.relationship < 3) // TODO fix that following https://github.com/yllibed/Zigbee2MqttAssistant/issues/113#issuecomment-552805477
			.Select(p => p.LinkQuality)
			.DefaultIfEmpty()
			.Max(); // This is a pattern for non-existent .MaxOrDefault()

		public ImmutableArray<ZigbeeLink> Parents { get; } = ImmutableArray<ZigbeeLink>.Empty;

		public string FriendlyName { get; }

		public string UniqueId { get; }

		public string Name { get; }

		public bool? IsAvailable { get; }

		public DateTimeOffset? LastSeen { get; }

		public decimal? BatteryLevel { get; }

		public string Manufacturer { get; }

		public string Model { get; }

		public string ModelId { get; }

		public long? HardwareVersion { get; }

		public string Type { get; }

		public ImmutableArray<HomeAssistantEntity> Entities { get; } = ImmutableArray<HomeAssistantEntity>.Empty;

		public bool? GetUnresponsiveDelay(out TimeSpan? delaySinceLastResponse, DateTimeOffset? now = null, TimeSpan? timeout = null)
		{
			if (LastSeen == null)
			{
				delaySinceLastResponse = null;
				return null; // unknown
			}

			var lastSeen = LastSeen.Value;

			var dtNow = (now ?? DateTimeOffset.Now);
			delaySinceLastResponse = dtNow - lastSeen;

			var dateTimeUnresponsive = dtNow - (timeout ?? TimeSpan.FromHours(2));

			return lastSeen < dateTimeUnresponsive;
		}

		public override string ToString() => FriendlyName != ZigbeeId ? $"{FriendlyName}-{ZigbeeId}" : FriendlyName;
	}
}
