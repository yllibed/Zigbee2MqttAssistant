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
			var deviceModel = json["device"]["model"]?.Value<string>();
			var deviceManufacturer = json["device"]["manufacturer"]?.Value<string>();

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
					state = state.WithDevices(devices => devices.Replace(device, newDevice));
					device = newDevice;
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
					state = state.WithDevices(devices => devices.Replace(device, newDevice));
					device = newDevice;
				}

				if (deviceName != null || deviceIds != null || deviceManufacturer != null)
				{
					ZigbeeDevice newDevice = device
						.WithName(deviceName)
						.WithUniqueId(deviceIds)
						.WithManufacturer(deviceManufacturer)
						.WithModel(deviceModel);
					state = state.WithDevices(devices => devices.Replace(device, newDevice));
					device = newDevice;
				}

				return state;
			}

			ImmutableInterlocked.Update(ref _currentState, Update);

			return entity;
		}
	}
}