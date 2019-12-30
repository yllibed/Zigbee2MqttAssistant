using System;
using System.Threading.Tasks;
using Zigbee2MqttAssistant.Models.Devices;
using Zigbee2MqttAssistant.Models.Mqtt;

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

		public async Task<ZigbeeDevice> RemoveDeviceById(string deviceId, bool forceRemove)
		{
			var device = _stateService.FindDeviceById(deviceId, out _);
			if (device == null)
			{
				return null;
			}

			await _mqtt.RemoveDeviceAndWait(device.FriendlyName, forceRemove);

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

		public async Task Bind(string id, string targetId)
		{
			var source = _stateService.FindDeviceById(id, out var state);
			var target = state.FindDevice(targetId);

			if (source == null || target == null)
			{
				return;
			}

			await _mqtt.Bind(source.FriendlyName, target.FriendlyName);
		}

		public async Task Unbind(string id, string targetId)
		{
			var source = _stateService.FindDeviceById(id, out var state);
			var target = state.FindDevice(targetId);

			if (source == null || target == null)
			{
				return;
			}

			await _mqtt.Unbind(source.FriendlyName, target.FriendlyName);
		}

		public async Task SetLogLevel(string level)
		{
			if (Enum.TryParse<MqttLogLevel>(level, ignoreCase: true, out var mqttLogLevel))
			{
				await _mqtt.SetLogLevel(mqttLogLevel);
			}
		}

		public Task ManualRefreshDevicesList() => _mqtt.SendDevicesRequest();

		public Task ManualRefreshNetworkScan() => _mqtt.SendNetworkScanRequest();
	}
}
