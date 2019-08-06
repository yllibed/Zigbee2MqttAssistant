using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Design;
using Uno;

namespace Zigbee2MqttAssistant.Models.Devices
{
	[GeneratedImmutable]
	[DebuggerDisplay("{ZigbeeId}-{FriendlyName}")]
	public partial class ZigbeeDevice
	{
		[EqualityKey]
		public string ZigbeeId { get; }

		[EqualityHash]
		public uint? NetworkAddress { get; }

		public ushort? LinkQuality { get; }

		public string ParentZigbeeId { get; }

		[EqualityKey]
		public string FriendlyName { get; }

		public string UniqueId { get; }

		public string Name { get; }

		[EqualityHash]
		public bool? IsAvailable { get; }

		public DateTimeOffset? LastSeen { get; }

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
	}
}
