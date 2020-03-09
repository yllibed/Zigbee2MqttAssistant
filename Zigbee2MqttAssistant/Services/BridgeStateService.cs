﻿using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
		public event EventHandler<Bridge> StateChanged;

		public BridgeStateService(ILogger<BridgeStateService> logger)
		{
			_logger = logger;
		}

		public void Clear()
		{
			_currentState = Bridge.Default;
		}

		private void UpdateState(Func<Bridge, Bridge> updater)
		{
			var isChanged = false;
			var updatedState = default(Bridge);

			Bridge Update(Bridge state)
			{
				try
				{
					updatedState = updater(state);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"Error while updating state.\nNew State: {state}");
					throw;
				}

				isChanged = updatedState != state;
				return updatedState;
			}

			if (ImmutableInterlocked.Update(ref _currentState, Update) && isChanged)
			{
				StateChanged?.Invoke(this, updatedState);
			}
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

			UpdateState(Update);

			return device;
		}

		private bool TryParseJson(string strInput, out JObject result)
		{
			result = null;
			strInput = strInput.Trim();
			if ((!strInput.StartsWith("{") || !strInput.EndsWith("}")) &&
			    (!strInput.StartsWith("[") || !strInput.EndsWith("]")))
			{
				//basic criteria for a JSON string/object not fulfilled
				_logger.LogDebug($"Basic criteria for a JSON object is not met '{strInput}'");
				return false;
			}
			
			try
			{
				result = JObject.Parse(strInput);
				return true;
			}
			catch (JsonReaderException ex)
			{
				//Invalid JSON
				_logger.LogDebug(ex, $"Invalid JSON payload '{strInput}'");
				return false;
			}
			catch (Exception ex) //some other exception
			{
				_logger.LogError(ex, $"Error validating JSON payload '{strInput}'");
				return false;
			}
		}

		public ZigbeeDevice UpdateDevice(string friendlyName, string jsonPayload, out bool forceLastSeen)
		{
			ZigbeeDevice device = null;
			if (!TryParseJson(jsonPayload, out var json))
			{
				forceLastSeen = false;
				return null;
			}

			var linkQuality = json["linkquality"]?.Value<ushort>();
			
			forceLastSeen = !ParseDateTimeOffset(json["last_seen"], out var lastSeen);

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

				var battery = json["battery"]?.Value<decimal>();
				var updateAvailable = json["update_available"]?.Value<bool?>();
				if (!lastSeen.HasValue && !battery.HasValue && !updateAvailable.HasValue)
				{
					return state;
				}

				ZigbeeDevice.Builder builder = device;
				if (lastSeen.HasValue)
				{
					builder.LastSeen = lastSeen;
				}

				if (battery.HasValue)
				{
					builder.BatteryLevel = battery;
				}

				if (updateAvailable.HasValue)
				{
					builder.IsOtaAvailable = updateAvailable;
				}

				ZigbeeDevice newDevice = builder;

				if (newDevice == device)
				{
					return state;
				}

				state = state.WithDevices(devices => devices.Replace(device, newDevice));
				device = newDevice;

				return state;
			}

			UpdateState(Update);

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

			UpdateState(Update);
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

			UpdateState(Update);
		}

		public void SetBridgeConfig(string configJson, out bool isJoinAllowed, out MqttLogLevel logLevel)
		{
			var json = JObject.Parse(configJson);

			var version = json["version"]?.Value<string>();
			var coordinator = json["coordinator"];
			string coordinatorVersion;
			var coordinatorType = "zStack";

			switch (coordinator?.Type)
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
				default:
				{
					coordinatorVersion = default;
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

			UpdateState(Update);
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
			var deviceName = json["device"]?["name"]?.Value<string>();
			var deviceIds = json["device"]?["identifiers"]?.FirstOrDefault()?.Value<string>();

			if (string.IsNullOrWhiteSpace(entityName)
			    || string.IsNullOrWhiteSpace(entityId)
			    || string.IsNullOrWhiteSpace(deviceName)
			    || string.IsNullOrWhiteSpace(deviceIds))
			{
				return null;
			}

			Bridge Update(Bridge state)
			{
				var device = state.Devices.FirstOrDefault(d => d.ZigbeeId != null && d.ZigbeeId.Equals(zigbeeId));

				if (device == null)
				{
					// no device with this zigbeeId is known, try to find device from payload
					var topic = json["state_topic"]?.Value<string>()
								?? json["json_attributes_topic"]?.Value<string>()
								?? json["availability_topic"]?.Value<string>();

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

			UpdateState(Update);

			return entity;
		}

		public ZigbeeDevice FindDeviceById(string deviceIdOrFriendlyName, out Bridge state)
		{
			state = _currentState;

			return state.FindDevice(deviceIdOrFriendlyName);
		}

		public void UpdateDevices(string payload)
		{
			var json = JToken.Parse(payload);

			if (json.Type == JTokenType.Object && json["type"]?.Value<string>() == "devices")
			{
				json = json["message"];
			}

			if (json.Type != JTokenType.Array)
			{
				_logger.LogWarning($"Invalid/unknown devices payload received. root element type is {json.Root.Type}: {payload.Substring(0, Math.Min(40, payload.Length))}... -- will be ignored.");
				return;
			}

			Bridge Update(Bridge state)
			{
				foreach (var deviceJson in json)
				{
					var friendlyName = deviceJson["friendly_name"]?.Value<string>();
					var zigbeeId = deviceJson["ieeeAddr"]?.Value<string>();

					var deviceType = deviceJson["type"]?.Value<string>();

					if (deviceType?.Equals("Coordinator", StringComparison.InvariantCultureIgnoreCase) == true)
					{
						state = state.WithCoordinatorZigbeeId(zigbeeId);
						friendlyName = "Coordinator";
					}

					if (string.IsNullOrWhiteSpace(friendlyName))
					{
						_logger.LogWarning($"Unable to understand device with json {deviceJson} -- will be ignored.");
						continue; // unable to qualify this device
					}

					var device = state.Devices.FirstOrDefault(d => d.FriendlyName.Equals(friendlyName) || (d.ZigbeeId?.Equals(zigbeeId) ?? false));
					var networkAddress = (deviceJson["nwkAddr"] ?? deviceJson["networkAddress"])?.Value<uint>();
					var model = (deviceJson["modelId"] ?? deviceJson["modelID"])?.Value<string>().Trim().Trim((char)0);
					var modelId = deviceJson["model"]?.Value<string>().Trim().Trim((char)0);
					var manufacturer = (deviceJson["manufName"] ?? deviceJson["manufacturerName"])?.Value<string>().Trim().Trim((char)0);
					var hardwareVersion = (deviceJson["hwVersion"] ?? deviceJson["hardwareVersion"])?.Value<long>();
					var firmwareVersion = (deviceJson["softwareBuildID"])?.Value<string>();

					ZigbeeDevice newDevice = (device ?? new ZigbeeDevice.Builder { FriendlyName = friendlyName })
						.WithZigbeeId(zigbeeId)
						.WithType(deviceType)
						.WithNetworkAddress(networkAddress)
						.WithModel(model)
						.WithModelId(modelId)
						.WithManufacturer(manufacturer)
						.WithHardwareVersion(hardwareVersion)
						.WithFirmwareVersion(firmwareVersion);

					if (ParseDateTimeOffset(deviceJson["lastSeen"], out var lastSeen))
					{
						newDevice = newDevice.WithLastSeen(lastSeen);
					}

					if (device != newDevice)
					{
						state = device == null
							? state.WithDevices(devices => devices.Add(newDevice))
							: state.WithDevices(devices => devices.Replace(device, newDevice));
					}
				}

				return state;
			}

			UpdateState(Update);
		}

		private bool ParseDateTimeOffset(JToken jtoken, out DateTimeOffset? dateTimeOffset)
		{
			switch (jtoken?.Type)
			{
				case JTokenType.Integer:
					dateTimeOffset =  DateTimeOffset.FromUnixTimeMilliseconds(jtoken.Value<long>());
					return true;
				case JTokenType.Date:
					dateTimeOffset = jtoken.Value<DateTime>();
					return true;
				case JTokenType.String:
					dateTimeOffset = jtoken.Value<DateTimeOffset>();
					return true;
				default:
					dateTimeOffset = null;
					return false;
			}
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
						var relationship = link["relationship"]?.Value<byte>();

						if (string.IsNullOrWhiteSpace(parent) || linkQuality == null)
						{
							continue; // weird case (payload is invalid?)
						}

						ZigbeeDevice newDevice;

						var existingParent = device.Parents.FirstOrDefault(p => p.zigbeeId == parent);

						if (existingParent != default) // found existing
						{
							newDevice = device
								.WithParents(parents => parents.Replace(existingParent, (parent, linkQuality.Value, relationship)));
						}
						else
						{
							newDevice = device
								.WithParents(parents => parents.Add((parent, linkQuality.Value, relationship)));
						}

						if (device != newDevice)
						{
							state = state.WithDevices(devices => devices.Replace(device, newDevice));
						}
					}
				}

				return state;
			}

			UpdateState(Update);
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

			UpdateState(Update);
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

			UpdateState(Update);
		}
	}
}
