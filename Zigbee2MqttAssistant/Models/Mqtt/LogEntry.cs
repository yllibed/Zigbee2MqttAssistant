using Uno;

namespace Zigbee2MqttAssistant.Models.Mqtt
{
	public partial class LogEntry
	{
		public string Type { get; }
		[EqualityHash]
		public string Message { get; }
	}
}
