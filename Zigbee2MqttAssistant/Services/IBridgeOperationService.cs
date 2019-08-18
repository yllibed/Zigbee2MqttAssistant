using System.Threading.Tasks;
using Zigbee2MqttAssistant.Models.Devices;

namespace Zigbee2MqttAssistant.Services
{
	public interface IBridgeOperationService
	{
		Task<ZigbeeDevice> RemoveDeviceById(string deviceId);
		Task<ZigbeeDevice> RenameDeviceById(string deviceId, string newName);
		Task AllowJoin(bool permitJoin);
		Task Reset();
		Task Bind(string id, string targetId);
		Task Unbind(string id, string targetId);
	}
}
