using System.Threading.Tasks;
using Zigbee2MqttAssistant.Models.Devices;

namespace Zigbee2MqttAssistant.Services
{
	public interface IBridgeOperationService
	{
		Task<ZigbeeDevice> RemoveDeviceById(string deviceId);
		Task<ZigbeeDevice> RenameDeviceById(string deviceId, string newName);
	}
}
