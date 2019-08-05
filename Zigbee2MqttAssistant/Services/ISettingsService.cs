using Zigbee2MqttAssistant.Models;

namespace Zigbee2MqttAssistant.Services
{
	public interface ISettingsService
	{
		Settings CurrentSettings { get; }
	}
}