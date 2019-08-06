using System.Threading.Tasks;
using Zigbee2MqttAssistant.Models.Devices;

namespace Zigbee2MqttAssistant.Services
{
	public class BrigeOperationService : IBridgeOperationService
	{
		public Task<ZigbeeDevice> RemoveDeviceById(string deviceId)
		{
			throw new System.NotImplementedException();
		}
	}
}