using System.Threading.Tasks;
using Zigbee2MqttAssistant.Models.Devices;

namespace Zigbee2MqttAssistant.Services
{
	public class BrigeOperationService : IBridgeOperationService
	{
		private readonly MqttConnectionService _mqtt;
		private readonly IBridgeStateService _stateService;

		public BrigeOperationService(MqttConnectionService mqtt, IBridgeStateService stateService)
		{
			_mqtt = mqtt;
			_stateService = stateService;
		}

		public async Task<ZigbeeDevice> RemoveDeviceById(string deviceId)
		{
			var device = _stateService.FindDeviceById(deviceId, out _);
			if (device == null)
			{
				return null;
			}

			await _mqtt.RemoveDeviceAndWait(device.FriendlyName);

			return device;
		}

		public async Task<ZigbeeDevice> RenameDeviceById(string deviceId, string newName)
		{
			var device = _stateService.FindDeviceById(deviceId, out _);
			if (device == null)
			{
				return null;
			}

			await _mqtt.RenameDeviceAndWait(device.FriendlyName, newName);

			return _stateService.FindDeviceById(newName, out _);
		}

		public Task AllowJoin(bool permitJoin)
		{
			return _mqtt.AllowJoinAndWait(permitJoin);
		}

		public Task Reset() => _mqtt.Reset();
	}
}
