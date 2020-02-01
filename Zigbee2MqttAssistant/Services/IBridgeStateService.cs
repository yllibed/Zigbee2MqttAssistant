using System;
using Zigbee2MqttAssistant.Models.Devices;
using Zigbee2MqttAssistant.Models.Mqtt;

namespace Zigbee2MqttAssistant.Services
{
	public interface IBridgeStateService
	{
		Bridge CurrentState { get; }
		event EventHandler<Bridge> StateChanged;

		void Clear();
		ZigbeeDevice NewDevice(string friendlyName, string zigbeeId, string modelId);
		ZigbeeDevice UpdateDevice(string friendlyName, string jsonPayload, out bool forceLastSeen);
		void SetDeviceAvailability(string friendlyName, bool isOnline);
		void SetBridgeState(bool isOnline);
		void SetBridgeConfig(string configJson, out bool isJoinAllowed, out MqttLogLevel logLevel);
		HomeAssistantEntity SetDeviceEntity(string zigbeeId, string entityClass, string component, string configPayload, Func<string, string> friendlyNameFromTopicDelegate);
		ZigbeeDevice FindDeviceById(string deviceIdOrFriendlyName, out Bridge state);
		void UpdateDevices(string payload);
		void UpdateNetworkMap(string payload);
		void UpdateRenamedDevice(string @from, string to);
		void RemoveDevice(string removedDeviceFriendlyName);
	}
}
