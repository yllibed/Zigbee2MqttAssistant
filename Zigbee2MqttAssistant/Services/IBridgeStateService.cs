﻿using System;
using Zigbee2MqttAssistant.Models.Devices;
using Zigbee2MqttAssistant.Models.Mqtt;

namespace Zigbee2MqttAssistant.Services
{
	public interface IBridgeStateService
	{
		Bridge CurrentState { get; }

		void Clear();
		ZigbeeDevice NewDevice(string friendlyName, string zigbeeId, string modelId);
		ZigbeeDevice UpdateDevice(string friendlyName, string jsonPayload);
		void SetDeviceAvailability(string friendlyName, bool isOnline);
		void SetBridgeState(bool isOnline);
		void SetBridgeConfig(string configJson, out bool isJoinAllowed, out MqttLogLevel logLevel);
		void SetMqttBrokerVersion(string version);
		void SetMqttConnected(bool isConnected);
		HomeAssistantEntity SetDeviceEntity(string zigbeeId, string entityClass, string component, string configPayload, Func<string, string> friendlyNameFromTopicDelegate);
		ZigbeeDevice FindDeviceById(string deviceId, out Bridge state);
		void UpdateDevices(string payload);
		void UpdateNetworkMap(string payload);
		void UpdateRenamedDevice(string @from, string to);
		void RemoveDevice(string removedDeviceFriendlyName);
	}
}
