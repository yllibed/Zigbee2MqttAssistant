using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Zigbee2MqttAssistant.Models.Devices;
using Zigbee2MqttAssistant.Models.Mqtt;

namespace Zigbee2MqttAssistant.Services
{
	public class BridgeStateService : IBridgeStateService
	{
		private readonly ILogger<BridgeStateService> _logger;
		private Bridge _currentState = Bridge.Default;

		public Bridge CurrentState => _currentState;

		public BridgeStateService(ILogger<BridgeStateService> logger)
		{
			_logger = logger;
		}

		public void Clear()
		{
			_currentState = Bridge.Default;
		}

		public ZigbeeDevice UpdateDevice(string friendlyName, string jsonPayload)
		{
			ZigbeeDevice device = null;
			var json = JObject.Parse(jsonPayload);
			var linkQuality = json["linkquality"]?.Value<ushort>();
			var lastSeen = json["last_seen"]?.Value<DateTime>();

			Bridge Update(Bridge state)
			{
				device = state.Devices.FirstOrDefault(d => d.FriendlyName.Equals(friendlyName));
				if (device == null)
				{
					device = new ZigbeeDevice.Builder
					{
						FriendlyName = friendlyName,
						LinkQuality = linkQuality,
						LastSeen = lastSeen
					};

					state = state.WithDevices(devices => devices.Add(device));
				}

				if (linkQuality.HasValue || lastSeen.HasValue)
				{
					var newDevice = device;
					if (linkQuality.HasValue)
					{
						newDevice = newDevice.WithLinkQuality(linkQuality);
					}

					if (lastSeen.HasValue)
					{
						newDevice = newDevice.WithLastSeen(lastSeen);
					}

					state = state.WithDevices(devices => devices.Replace(device, newDevice));
					device = newDevice;
				}

				return state;
			}

			ImmutableInterlocked.Update(ref _currentState, Update);

			return device;
		}

		public void SetDeviceAvailability(string friendlyName, bool isOnline)
		{
			Bridge Update(Bridge state)
			{
				var device = state.Devices.FirstOrDefault(d => d.FriendlyName.Equals(friendlyName));

				if (device == null)
				{
					device = new ZigbeeDevice.Builder {FriendlyName = friendlyName, IsAvailable = isOnline};
					state = state.WithDevices(devices => devices.Add(device));
				}

				if (device.IsAvailable == isOnline)
				{
					return state;
				}

				var newDevice = device.WithIsAvailable(isOnline);

				state = state.WithDevices(devices => devices.Replace(device, newDevice));

				return state;
			}

			ImmutableInterlocked.Update(ref _currentState, Update);
		}

		public void SetBridgeState(bool isOnline)
		{
			Bridge Update(Bridge state)
			{
				return state.WithOnline(isOnline);
			}

			ImmutableInterlocked.Update(ref _currentState, Update);
		}

		public void SetBridgeConfig(string configJson)
		{
			Bridge Update(Bridge state)
			{
				return state;
			}

			ImmutableInterlocked.Update(ref _currentState, Update);
		}

		public HomeAssistantEntity SetDeviceEntity(
			string zigbeeId,
			string entityClass,
			string component,
			string configPayload,
			Func<string, string> friendlyNameFromTopicDelegate)
		{
			HomeAssistantEntity entity = null;

			var json = JObject.Parse(configPayload);

			var entityName = json["name"]?.Value<string>();
			var entityId = json["unique_id"]?.Value<string>();
			var deviceName = json["device"]["name"]?.Value<string>();
			var deviceIds = json["device"]["identifiers"]?.Value<string>();

			Bridge Update(Bridge state)
			{
				var device = state.Devices.FirstOrDefault(d => d.ZigbeeId != null && d.ZigbeeId.Equals(zigbeeId));

				if (device == null)
				{
					// no device with this zigbeeId is known, try to find device from payload
					var topic = json["state_topic"].Value<string>()
					            ?? json["json_attributes_topic"].Value<string>()
								?? json["availability_topic"].Value<string>();

					if (topic == null)
					{
						_logger.LogWarning($"Unable to find a source topic for entity {zigbeeId}-{entityClass}-{component}.");
						return state; // there's nothing we can do here
					}

					var friendlyName = friendlyNameFromTopicDelegate(topic);

					if (friendlyName == null)
					{
						_logger.LogWarning($"Unable to find friendly name from topic {topic} in entity {zigbeeId}-{entityClass}-{component}.");
						return state; // there's nothing we can do here
					}

					device = state.Devices.FirstOrDefault(d => d.FriendlyName.Equals(friendlyName));
					if (device == null)
					{
						device = new ZigbeeDevice.Builder {FriendlyName = friendlyName, UniqueId = zigbeeId, ZigbeeId = zigbeeId};
						state = state.WithDevices(devices => devices.Add(device));
					}
				}

				if (device.ZigbeeId != zigbeeId)
				{
					ZigbeeDevice newDevice = device.WithUniqueId(zigbeeId);

					if (newDevice != device)
					{
						state = state.WithDevices(devices => devices.Replace(device, newDevice));
						device = newDevice;
					}
				}

				entity = device.Entities.FirstOrDefault(e => e.Component.Equals(component));
				if (entity == null)
				{
					entity = new HomeAssistantEntity.Builder
					{
						EntityId = entityId,
						Component = component,
						Name = entityName,
						DeviceClass = entityClass
					};
					ZigbeeDevice newDevice = device.WithEntities(entities => entities.Add(entity));

					if (newDevice != device)
					{
						state = state.WithDevices(devices => devices.Replace(device, newDevice));
						device = newDevice;
					}
				}

				if (deviceName != null || deviceIds != null)
				{
					ZigbeeDevice newDevice = device
						.WithName(deviceName)
						.WithUniqueId(deviceIds);

					if (newDevice != device)
					{
						state = state.WithDevices(devices => devices.Replace(device, newDevice));
						device = newDevice;
					}
				}

				return state;
			}

			ImmutableInterlocked.Update(ref _currentState, Update);

			return entity;
		}

		public ZigbeeDevice FindDeviceById(string deviceId, out Bridge state)
		{
			state = _currentState;

			if (string.IsNullOrWhiteSpace(deviceId))
			{
				return null;
			}

			return state.Devices.FirstOrDefault(device =>
				device.FriendlyName.Equals(deviceId) || (device.ZigbeeId?.Equals(deviceId) ?? false));
		}

		public void UpdateDevices(string payload)
		{
			var json = JArray.Parse(payload);

			Bridge Update(Bridge state)
			{
				foreach (var deviceJson in json)
				{
					var friendlyName = deviceJson["friendly_name"]?.Value<string>();
					if (string.IsNullOrWhiteSpace(friendlyName))
					{
						if (deviceJson["type"]?.Value<string>().Equals("Coordinator", StringComparison.InvariantCultureIgnoreCase) ?? false)
						{
							state = state.WithCoordinatorZigbeeId(deviceJson["ieeeAddr"]?.Value<string>());
						}

						continue;
					}

					var device = state.Devices.FirstOrDefault(d => d.FriendlyName.Equals(friendlyName));
					var newDevice = (device ?? new ZigbeeDevice.Builder { FriendlyName = friendlyName })
						.WithZigbeeId(deviceJson["ieeeAddr"]?.Value<string>())
						.WithType(deviceJson["type"]?.Value<string>())
						.WithModel(deviceJson["modelId"]?.Value<string>().Trim().Trim((char)0))
						.WithModelId(deviceJson["model"]?.Value<string>().Trim().Trim((char)0))
						.WithNetworkAddress(deviceJson["nwkAddr"]?.Value<uint>())
						.WithHardwareVersion(deviceJson["hwVersion"]?.Value<long>())
						.WithManufacturer(deviceJson["manufName"]?.Value<string>().Trim().Trim((char)0));

					if (device != newDevice)
					{
						state = device == null
							? state.WithDevices(devices => devices.Add(newDevice))
							: state.WithDevices(devices => devices.Replace(device, newDevice));
					}
				}

				return state;
			}

			ImmutableInterlocked.Update(ref _currentState, Update);
		}

		public void UpdateNetworkMap(string payload)
		{
			var json = JArray.Parse(payload);

			Bridge Update(Bridge state)
			{
				foreach (var deviceJson in json)
				{
					var zigbeeId = deviceJson["ieeeAddr"]?.Value<string>();
					if (string.IsNullOrWhiteSpace(zigbeeId))
					{
						continue;
					}

					var device = state.Devices.FirstOrDefault(d => d.ZigbeeId?.Equals(zigbeeId) ?? false);
					if (device == null)
					{
						continue;
					}

					var parentZigbeeId = deviceJson["parent"]?.Value<string>();

					var newDevice = device
						.WithLinkQuality(deviceJson["lqi"]?.Value<ushort>())
						.WithIsAvailable(deviceJson["status"]?.Value<string>().Equals("online"))
						.WithParentZigbeeId(deviceJson["parent"]?.Value<string>());

					if (device != newDevice)
					{
						state = state.WithDevices(devices => devices.Replace(device, newDevice));
					}
				}

				return state;
			}

			ImmutableInterlocked.Update(ref _currentState, Update);
		}
	}
}