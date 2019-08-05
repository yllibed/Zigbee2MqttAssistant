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
		public string FriendlyName { get; }

		public string UniqueId { get; }

		public string Name { get; }

		[EqualityHash]
		public bool? IsAvailable { get; }

		public ushort? LinkQuality { get; }

		public DateTimeOffset? LastSeen { get; }

		public string Manufacturer { get; }

		public string Model { get; }

		public ImmutableArray<HomeAssistantEntity> Entities { get; } = ImmutableArray<HomeAssistantEntity>.Empty;
	}
}
