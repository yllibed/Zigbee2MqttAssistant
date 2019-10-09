using System;
using System.Collections.Generic;
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

		public ZigbeeDevice NewDevice(string friendlyName, string zigbeeId, string modelId)
		{
			ZigbeeDevice device = null;
			Bridge Update(Bridge state)
			{
				device = state.Devices.FirstOrDefault(d => d.FriendlyName.Equals(friendlyName));

				if (device == null)
				{

					device = new ZigbeeDevice.Builder
					{
						FriendlyName = friendlyName,
						IsAvailable = true,
						ZigbeeId = zigbeeId,
						ModelId = modelId
					};
					state = state.WithDevices(devices => devices.Add(device));
				}

				return state;
			}

			ImmutableInterlocked.Update(ref _currentState, Update);

			return device;
		}

		public ZigbeeDevice UpdateDevice(string friendlyName, string jsonPayload)
		{
			ZigbeeDevice device = null;
			var json = JObject.Parse(jsonPayload);
			var linkQuality = json["linkquality"]?.Value<ushort>();
			var lastSeen = json["last_seen"]?.Value<DateTime>();
			var battery = json["battery"]?.Value<decimal>();

			Bridge Update(Bridge state)
			{
				device = state.Devices.FirstOrDefault(d => d.FriendlyName.Equals(friendlyName));
				if (device == null)
				{
					device = new ZigbeeDevice.Builder
					{
						FriendlyName = friendlyName,
						LastSeen = lastSeen
					};

					state = state.WithDevices(devices => devices.Add(device));
				}

				if (lastSeen.HasValue || battery.HasValue)
				{
					ZigbeeDevice.Builder builder = device;
					if (lastSeen.HasValue)
					{
						builder.LastSeen = lastSeen;
					}

					if (battery.HasValue)
					{
						builder.BatteryLevel = battery;
					}

					ZigbeeDevice newDevice = builder;

					if (newDevice != device)
					{
						state = state.WithDevices(devices => devices.Replace(device, newDevice));
						device = newDevice;
					}
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

				ZigbeeDevice newDevice = device.WithIsAvailable(isOnline);

				state = state.WithDevices(devices => devices.Replace(device, newDevice));

				return state;
			}

			ImmutableInterlocked.Update(ref _currentState, Update);
		}

		public void SetBridgeState(bool isOnline)
		{
			if (!isOnline)
			{
				Clear();
				return;
			}

			Bridge Update(Bridge state)
			{
				return state.WithOnline(isOnline);
			}

			ImmutableInterlocked.Update(ref _currentState, Update);
		}

		public void SetBridgeConfig(string configJson, out bool isJoinAllowed, out MqttLogLevel logLevel)
		{
			var json = JObject.Parse(configJson);

			var version = json["version"]?.Value<string>();
			var coordinator = json["coordinator"];
			var coordinatorVersion = default(string);
			var coordinatorType = "zStack";

			switch (coordinator.Type)
			{
				case JTokenType.String:
				{
					coordinatorVersion = coordinator.Value<string>();
					break;
				}
				case JTokenType.Object:
				{
					coordinatorVersion = coordinator["meta"]?["revision"]?.Value<string>();
					coordinatorType = coordinator["type"]?.Value<string>();
					break;
				}
			}

			var permitJoin = json["permit_join"]?.Value<bool>() ?? false;
			var logLevel2 = logLevel = json["log_level"]?.ToObject<MqttLogLevel>() ?? MqttLogLevel.Info;

			isJoinAllowed = permitJoin;

			Bridge Update(Bridge state)
			{
				state = state
					.WithZigbee2MqttVersion(version)
					.WithCoordinatorVersion(coordinatorVersion)
					.WithCoordinatorType(coordinatorType)
					.WithPermitJoin(permitJoin)
					.WithLogLevel(logLevel2);

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
			var deviceIds = json["device"]["identifiers"]?.FirstOrDefault()?.Value<string>();

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

			return state.FindDevice(deviceId);
		}

		public void UpdateDevices(string payload)
		{
			var json = JArray.Parse(payload);

			Bridge Update(Bridge state)
			{
				foreach (var deviceJson in json)
				{
					var friendlyName = deviceJson["friendly_name"]?.Value<string>();
					var zigbeeId = deviceJson["ieeeAddr"]?.Value<string>();

					if (string.IsNullOrWhiteSpace(friendlyName))
					{
						if (deviceJson["type"]?.Value<string>().Equals("Coordinator", StringComparison.InvariantCultureIgnoreCase) ?? false)
						{
							state = state.WithCoordinatorZigbeeId(zigbeeId);
						}

						friendlyName = "Coordinator";
					}

					var device = state.Devices.FirstOrDefault(d => d.FriendlyName.Equals(friendlyName) || (d.ZigbeeId?.Equals(zigbeeId) ?? false));
					ZigbeeDevice newDevice = (device ?? new ZigbeeDevice.Builder { FriendlyName = friendlyName })
						.WithZigbeeId(zigbeeId)
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
			var json = JObject.Parse(payload);
			var nodes = json["nodes"] as JArray;
			var links = json["links"] as JArray;

			Bridge Update(Bridge state)
			{
				if (nodes != null)
				{
					foreach (var node in nodes)
					{
						var zigbeeId = node["ieeeAddr"]?.Value<string>();
						if (string.IsNullOrWhiteSpace(zigbeeId))
						{
							continue;
						}

						var device = state.Devices.FirstOrDefault(d => d.ZigbeeId?.Equals(zigbeeId) ?? false);
						if (device == null)
						{
							continue;
						}

						var parentZigbeeId = node["parent"]?.Value<string>();

						var newDevice = device
							.WithIsAvailable(node["status"]?.Value<string>().Equals("online"));

						if (device != newDevice)
						{
							state = state.WithDevices(devices => devices.Replace(device, newDevice));
						}
					}
				}

				if (links != null)
				{
					foreach (var link in links)
					{
						var source = link["sourceIeeeAddr"]?.Value<string>();
						if (string.IsNullOrWhiteSpace(source))
						{
							continue; // weird case (payload is invalid?)
						}

						var device = state.Devices.FirstOrDefault(d => d.ZigbeeId?.Equals(source) ?? false);
						if (device == null)
						{
							continue; // unknown device
						}

						var parent = link["targetIeeeAddr"]?.Value<string>();
						var linkQuality = link["lqi"]?.Value<ushort>();
						if (string.IsNullOrWhiteSpace(parent) || linkQuality == null)
						{
							continue; // weird case (payload is invalid?)
						}

						ZigbeeDevice newDevice;

						var existingParent = device.Parents.FirstOrDefault(p => p.zigbeeId == parent);

						if (existingParent != default)
						{
							newDevice = device
								.WithParents(parents => parents.Replace(existingParent, (parent, linkQuality.Value)));
						}
						else
						{
							newDevice = device
								.WithParents(parents => parents.Add((parent, linkQuality.Value)));
						}

						if (device != newDevice)
						{
							state = state.WithDevices(devices => devices.Replace(device, newDevice));
						}
					}
				}

				return state;
			}

			ImmutableInterlocked.Update(ref _currentState, Update);
		}

		public void UpdateRenamedDevice(string from, string to)
		{
			Bridge Update(Bridge state)
			{
				var device = state.Devices.FirstOrDefault(d => d.FriendlyName.Equals(from));
				if (device == null)
				{
					return state;
				}

				ZigbeeDevice newDevice = device.WithFriendlyName(to);
				if (newDevice != device)
				{
					state = state.WithDevices(devices => devices.Replace(device, newDevice));
				}

				return state;
			}

			ImmutableInterlocked.Update(ref _currentState, Update);
		}

		public void RemoveDevice(string removedDeviceFriendlyName)
		{
			Bridge Update(Bridge state)
			{
				var device = state.Devices.FirstOrDefault(d => d.FriendlyName.Equals(removedDeviceFriendlyName));
				if (device == null)
				{
					return state;
				}

				state = state.WithDevices(devices => devices.Remove(device));

				return state;
			}

			ImmutableInterlocked.Update(ref _currentState, Update);
		}

		public IReadOnlyCollection<IReadOnlyCollection<ZigbeeDevice>> GetRoutesToCoordinator(ZigbeeDevice forDevice)
		{
			var state = _currentState;

			var processedDevices = new List<ZigbeeDevice>(state.Devices.Length -1);

			IEnumerable<IReadOnlyCollection<ZigbeeDevice>> GetRoutes(ZigbeeDevice d)
			{
				if (processedDevices.Contains(d))
				{
					yield break; // already processed routed through this device
				}
			}

			return GetRoutes(forDevice).ToImmutableArray();
		}
	}
}
