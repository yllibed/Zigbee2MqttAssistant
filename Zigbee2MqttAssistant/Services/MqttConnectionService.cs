﻿using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Zigbee2MqttAssistant.Models;
using Zigbee2MqttAssistant.Models.Mqtt;

namespace Zigbee2MqttAssistant.Services
{
	public class MqttConnectionService : IHostedService, IDisposable, IMqttClientConnectedHandler, IMqttClientDisconnectedHandler, IMqttApplicationMessageReceivedHandler
	{
		private readonly ILogger<MqttConnectionService> _logger;
		private readonly ISettingsService _settings;
		private readonly IBridgeStateService _stateService;

		private readonly SerialDisposable _connection = new SerialDisposable();
		private readonly SerialDisposable _devicePolling = new SerialDisposable();
		private readonly MqttFactory _mqttFactory;

		private IManagedMqttClient _client;
		private static readonly Encoding _utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

		public MqttConnectionService(ILogger<MqttConnectionService> _logger, ISettingsService settings, IBridgeStateService stateService)
		{
			this._logger = _logger;
			_settings = settings;
			_stateService = stateService;

			_mqttFactory = new MqttFactory();
			BuildRegexes();
		}

		private void BuildRegexes()
		{
			var settings = _settings.CurrentSettings;
			var baseTopic = $"{settings.BaseTopic}/";
			var baseHassTopic = $"{settings.HomeAssistantDiscoveryBaseTopic}/";

			var regexOptions = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant;
			FriendlyNameExtractor = new Regex($"^{Regex.Escape(baseTopic)}(?<name>.+?)(?:/(?<state>(availability|state|config|config/devices|attributes)))?$", regexOptions);
			_hassDiscoveryExtractor = new Regex($"^{Regex.Escape(baseHassTopic)}(?<class>[^/]+)/(?<deviceId>[^/]+)/(?<component>[^/]+)/(?<config>config)?$", regexOptions);
			_setTopicRegex = new Regex($"^{Regex.Escape(baseTopic)}.+/set$", regexOptions);
		}

		public async Task StartAsync(CancellationToken ct)
		{
			await Connect();
		}

		public async Task StopAsync(CancellationToken ct)
		{
			_logger.LogInformation("Stopping MqttConnectionService...");
			await Task.Yield();
			Disconnect();
		}

		public async Task OtaUpdateDevice(string deviceFriendlyName)
		{
			var device = _stateService.FindDeviceById(deviceFriendlyName, out _);
			
			var msg = new MqttApplicationMessageBuilder()
				.WithTopic($"{_settings.CurrentSettings.BaseTopic}/bridge/ota_update/update")
				.WithPayload(device.FriendlyName)
				.Build();

			await _client.PublishAsync(msg);
		}

		public async Task TouchLinkReset()
		{
			var msg = new MqttApplicationMessageBuilder()
				.WithTopic($"{_settings.CurrentSettings.BaseTopic}/bridge/config/touchlink/factory_reset")
				.WithPayload(string.Empty)
				.Build();

			await _client.PublishAsync(msg).ConfigureAwait(false);
		}

		private async Task Connect()
		{
			Disconnect();

			_logger.LogInformation("Starting MqttConnectionService...");
			await Task.Yield();
			var settings = _settings.CurrentSettings;

			var options = new MqttClientOptionsBuilder()
				.WithTcpServer(settings.MqttServer, settings.MqttPort)
				.WithTls(x =>
				{
					x.UseTls = settings.MqttSecure != TlsMode.False;
					x.AllowUntrustedCertificates = settings.MqttSecure == TlsMode.Insecure;
				})
				.WithCredentials(settings.MqttUsername, settings.MqttPassword)
				.Build();

			var managedOptions = new ManagedMqttClientOptionsBuilder()
				.WithClientOptions(options)
				.WithAutoReconnectDelay(TimeSpan.FromSeconds(10))
				.Build();

			_client = _mqttFactory.CreateManagedMqttClient();

			_client.ConnectedHandler = this;
			_client.DisconnectedHandler = this;
			_client.ApplicationMessageReceivedHandler = this;
			await _client.StartAsync(managedOptions);

			_connection.Disposable = Disposable.Create(() =>
			{
				_logger.LogInformation("Disconnection from MQTT server...");
				_client.Dispose();
			});

			_logger.LogInformation("Subscribing to MQTT topics...");
			await Subscribe();
		}

		private void StartPolling()
		{
			var cancellableDisposable = new CancellationDisposable();
			_devicePolling.Disposable = cancellableDisposable;

			var cct = cancellableDisposable.Token;

			PollingDevicesTask(cct);
			PollingNetworkTask(cct);


			CronExpression ParseCronExpression(string cronExpression)
			{
				try
				{
					return CronExpression.Parse(cronExpression);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"Error parsing cron expression '{cronExpression}'.");
					return null;
				}
			}

			async void PollingDevicesTask(CancellationToken ct)
			{
				try
				{
					var cron = ParseCronExpression(_settings.CurrentSettings.DevicesPollingSchedule);

					await Task.Delay(5000, ct);

					while (!ct.IsCancellationRequested)
					{
						try
						{
							await SendDevicesRequest();
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Error sending device request");
						}

						var now = DateTimeOffset.Now;
						var next = cron?.GetNextOccurrence(now, TimeZoneInfo.Local) ?? now.AddMinutes(6);
						var delay = next - now;

						_logger.LogDebug(
							$"PollingDevicesTask: Waiting until {next} (currently is {now}. (cron={cron})");
						await Task.Delay(delay, ct);
					}
				}
				catch (TaskCanceledException)
				{
					// Don't crash on this exception
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "PollingDevicesTask() shutting down because of an exception.");
				}
			}

			async void PollingNetworkTask(CancellationToken ct)
			{
				try
				{
					var cron = ParseCronExpression(_settings.CurrentSettings.NetworkScanSchedule);

					// A random offset is used to prevent many instances to send the same request
					// exactly at the same time
					var randomOffset = TimeSpan.FromSeconds(new Random().NextDouble() * 6);

					await Task.Delay(30000, ct);

					while (!ct.IsCancellationRequested)
					{
						try
						{
							await SendNetworkScanRequest();
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Error sending network scan request");
						}

						var now = DateTimeOffset.Now;
						var next = (cron?.GetNextOccurrence(now, TimeZoneInfo.Local) ?? now.AddMinutes(20)) +
						           randomOffset;
						var delay = (next - now);

						_logger.LogDebug(
							$"PollingNetworkTask: Waiting until {next} (currently is {now}. (cron={cron})");
						await Task.Delay(delay, ct);
					}
				}
				catch (TaskCanceledException)
				{
					// Don't crash on this exception
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "PollingDevicesTask() shutting down because of an exception.");
				}
			}
		}

		private DateTimeOffset _lastDevicesRequest = DateTimeOffset.MinValue;
		private async Task SetLastSeen()
		{
			var msg = new MqttApplicationMessageBuilder()
				.WithTopic($"{_settings.CurrentSettings.BaseTopic}/bridge/config/last_seen")
				.WithPayload("epoch")
				.Build();

			await _client.PublishAsync(msg);
		}

		public async Task SendDevicesRequest()
		{
			_logger.LogInformation("Launching a request for an updated devices list...");

			if (_lastDevicesRequest > DateTimeOffset.Now.AddSeconds(30))
			{
				_logger.LogWarning("Another devices request already in progress.");

				return;
			}

			var msg = new MqttApplicationMessageBuilder()
				.WithTopic($"{_settings.CurrentSettings.BaseTopic}/bridge/config/devices/get")
				.Build();

			_lastDevicesRequest = DateTimeOffset.MinValue;

			await _client.PublishAsync(msg);
		}

		private bool _waitingForNetworkScanResponse;

		public async Task SendNetworkScanRequest()
		{
			_logger.LogInformation("Launching a request for a network scan...");

			if (_waitingForNetworkScanResponse)
			{
				_logger.LogWarning("Another network scan request already in progress.");

				return;
			}

			var msg = new MqttApplicationMessageBuilder()
				.WithTopic($"{_settings.CurrentSettings.BaseTopic}/bridge/networkmap")
				.WithPayload("raw")
				.Build();

			_waitingForNetworkScanResponse = true;

			await _client.PublishAsync(msg);
		}

		private void StopPolling() => _devicePolling.Disposable = null;

		private void Disconnect()
		{
			StopPolling();
			_lastDevicesRequest = DateTimeOffset.MinValue;
			_waitingForNetworkScanResponse = false;
			_connection.Disposable = null;
		}

		private async Task Subscribe()
		{
			var settings = _settings.CurrentSettings;
			await _client.SubscribeAsync($"{settings.BaseTopic}/#");
			await _client.SubscribeAsync($"{settings.HomeAssistantDiscoveryBaseTopic}/#");
		}

		public void Dispose()
		{
			_devicePolling.Dispose();
			_connection.Dispose();
		}

		private static readonly string[] TopicsToIgnore =
		{
			"/bridge/config/last_seen", // request to set "last_seen"
			"/bridge/config/log_level", // request to change log level
			"/bridge/config/permit_join", // setting allow join
			"/bridge/config/remove", // request for a device remove
			"/bridge/config/force_remove", // request for a device remove
			"/bridge/config/rename", // request to rename a device
			"/bridge/configure", // request to configure a device
			"/bridge/ota_update" // request to ota update a device
		};

		private Regex _setTopicRegex;

		public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
		{
			var msg = eventArgs.ApplicationMessage;

			var topic = msg.Topic;

			try
			{
				_logger.LogDebug($"Received MQTT message on topic '{topic}'");

				if (TopicsToIgnore.Any(s => topic.EndsWith(s)))
				{
					_logger.LogDebug($"MQTT message on topic '{topic}' is on ignored list.");
					return Task.CompletedTask; // this topic could be safely ignored
				}

				if (_setTopicRegex.IsMatch(topic))
				{
					_logger.LogDebug($"MQTT message on topic '{topic}' is a set topic, we can ignore it.");
					return Task.CompletedTask; // this one too
				}

				if (topic.EndsWith("/bridge/config/devices/get"))
				{
					// Something elsewhere asked for a devices list
					_lastDevicesRequest = DateTimeOffset.Now;
					return Task.CompletedTask;
				}

				if (topic.EndsWith("/bridge/networkmap"))
				{
					// Something elsewhere asked for a network scan
					_waitingForNetworkScanResponse = true;
					return Task.CompletedTask;
				}

				if (topic.EndsWith("/bridge/networkmap/graphviz"))
				{
					// Something elsewhere asked for a network scan
					_waitingForNetworkScanResponse = false;
					return Task.CompletedTask;
				}

				if (DispatchHassDiscoveryMessage(msg))
				{
					_logger.LogDebug(
						$"MQTT message on topic '{topic}' has been processed as a HASS discovery message.");
					return Task.CompletedTask;
				}

				if (DispatchDevicesMessage(msg))
				{
					_logger.LogDebug(
						$"MQTT message on topic '{topic}' has been processed as a device information message.");
					return Task.CompletedTask;
				}

				if (DispatchLogMessage(msg))
				{
					_logger.LogDebug($"MQTT message on topic '{topic}' has been processed as a Z2M log message.");
					return Task.CompletedTask;
				}

				if (DispatchZigbee2MqttMessage(msg))
				{
					return Task.CompletedTask;
				}

				_logger.LogInformation($"Unable to qualify a message received on topic '{msg.Topic}'.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error processing MQTT message on topic '{topic}'");
			}

			return Task.CompletedTask;
		}

		private Regex FriendlyNameExtractor;
		private static readonly Regex SetRemover = new Regex(@"^(?<friendlyName>.+)(?:(?:\/set\/.+))$", RegexOptions.Compiled | RegexOptions.Singleline);

		private bool DispatchZigbee2MqttMessage(MqttApplicationMessage msg)
		{
			var match = FriendlyNameExtractor.Match(msg.Topic);

			if (!match.Success)
			{
				return false;
			}

			var friendlyName = match.Groups["name"].Value;

			if (friendlyName.Equals("bridge"))
			{
				var stateGroup = match.Groups["state"];
				if (stateGroup.Success && msg.Payload != null)
				{
					var value = stateGroup.Value;
					var payload = _utf8.GetString(msg.Payload);
					if (value.Equals("state"))
					{
						var isOnline = payload.Equals("online");
						_stateService.SetBridgeState(isOnline: isOnline);

						if (isOnline)
						{
							StartPolling();
						}
					}
					else if (value.Equals("config"))
					{
						_stateService.SetBridgeConfig(configJson: payload, isJoinAllowed: out var isJoinAllowed, logLevel: out var logLevel);

						if (ImmutableInterlocked.TryRemove(ref _allowJoinWaitingList, isJoinAllowed, out var tcs))
						{
							tcs.TrySetResult(null);
						}

						if (ImmutableInterlocked.TryRemove(ref _logLevelWaitingList, logLevel, out var tcs2))
						{
							tcs2.TrySetResult(null);
						}
					}
					return true;
				}
				return false;
			}

			if (msg.Payload == null)
			{
				return true;
			}

			if (match.Groups["state"].Success)
			{
				var payload = _utf8.GetString(msg.Payload);
				_stateService.SetDeviceAvailability(friendlyName, payload.Equals("online"));
			}
			else
			{
				var setMatch = SetRemover.Match(friendlyName);
				var warnSetInName = false;
				if (setMatch.Success)
				{
					if (_stateService.CurrentState.Devices.Length == 0)
					{
						return false; // no devices received yet
					}

					var realFriendlyName = setMatch.Groups["friendlyName"].Value;
					if (_stateService.FindDeviceById(realFriendlyName, out _) != null)
					{
						// It's not the state of a device, it's a /set/<attribute_name> on a device!
						// (absolutely no interests for that here: it will just generate parsing exceptions)
						return false;
					}

					_logger.LogWarning($"Received a message for topic '{friendlyName}'. It looks like it's setting an attribute on a friendly name '{realFriendlyName}', but no such device is known. If the application is starting, you can safely discard this warning and the following potential parsing error.");
					warnSetInName = true;
				}

				var payload = _utf8.GetString(msg.Payload);
				if (_stateService.UpdateDevice(friendlyName: friendlyName, jsonPayload: payload,
					    forceLastSeen: out var setLastSeen) != null && setLastSeen && _settings.CurrentSettings.AutosetLastSeen)
				{
					_ = SetLastSeen();
				}

				if (warnSetInName)
				{
					_logger.LogWarning($"It look like you actually have a device named '{friendlyName}' after all. THAT'S A BAD NAME: consider changing it!");
				}
			}

			return true;
		}

		private Regex _hassDiscoveryExtractor;

		private bool DispatchHassDiscoveryMessage(MqttApplicationMessage msg)
		{
			var match = _hassDiscoveryExtractor.Match(msg.Topic);

			if (!match.Success)
			{
				return false;
			}

			var deviceClass = match.Groups["class"];
			var id = match.Groups["deviceId"];
			var component = match.Groups["component"];
			var config = match.Groups["config"];

			if (config.Success && msg.Payload != null && msg.Payload.Length > 2)
			{
				var payload = _utf8.GetString(msg.Payload);
				
				_stateService.SetDeviceEntity(
					zigbeeId: id.Value,
					entityClass: deviceClass.Value,
					component: component.Value,
					configPayload: payload,
					FriendlyNameFromTopic);
			}

			string FriendlyNameFromTopic(string topic)
			{
				var topicMatch = FriendlyNameExtractor.Match(topic);
				return topicMatch.Success ? topicMatch.Groups["name"].Value : null;
			}

			return true;
		}

		private bool DispatchDevicesMessage(MqttApplicationMessage msg)
		{
			var settings = _settings.CurrentSettings;
			if (msg.Topic.Equals($"{settings.BaseTopic}/bridge/config/devices"))
			{
				var payload = _utf8.GetString(msg.Payload);
				_stateService.UpdateDevices(payload);

				_lastDevicesRequest = DateTimeOffset.MinValue;

				return true;
			}
			if (msg.Topic.Equals($"{settings.BaseTopic}/bridge/networkmap/raw"))
			{
				var payload = _utf8.GetString(msg.Payload);
				_stateService.UpdateNetworkMap(payload);

				_waitingForNetworkScanResponse = false;

				return true;
			}

			return false;
		}

		private bool DispatchLogMessage(MqttApplicationMessage msg)
		{
			var settings = _settings.CurrentSettings;
			if (msg.Topic.Equals($"{settings.BaseTopic}/bridge/log") && msg.Payload != null)
			{
				var payload = _utf8.GetString(msg.Payload);
				var json = JObject.Parse(payload);
				var type = json["type"]?.Value<string>();
				var message = json["message"];
				var meta = json["meta"];

				switch (type)
				{
					case "device_renamed":
					{
						var from = message["from"]?.Value<string>();
						var to = message["to"]?.Value<string>();
						_stateService.UpdateRenamedDevice(from, to);

						if (ImmutableInterlocked.TryRemove(ref _renameWaitingList, from, out var tcs))
						{
							tcs.TrySetResult(null); // unblock waiting for rename
						}

						break;
					}

					case "device_removed":
					case "device_forced_removed":
					case "device_removed_failed":
					case "device_forced_removed_failed":
					{
						var removedDevice = message?.Value<string>();

						_stateService.RemoveDevice(removedDevice);

						if (ImmutableInterlocked.TryRemove(ref _removeWaitingList, removedDevice, out var tcs))
						{
							if (type.EndsWith("_failed"))
							{
								tcs.TrySetException(new Exception("Remove device failed."));
							}
							else
							{
								tcs.TrySetResult(null); // unblock waiting for rename
							}
						}
						break;
					}

					case "device_connected":
					{
						var friendlyName = message.Type == JTokenType.Object
							? message["friendly_name"]?.Value<string>()
							: message?.Value<string>();

						var model = meta?["modelID"]?.Value<string>();

						_stateService.NewDevice(friendlyName, friendlyName, model);
						break;
					}

					case "device_bind":
					{
						var from = message["from"]?.Value<string>();
						var to = message["to"]?.Value<string>();

						if (ImmutableInterlocked.TryRemove(ref _bindWaitingList, (from, to), out var tcs))
						{
							tcs.TrySetResult(null);
						}

						break;
					}

					case "device_unbind":
					{
						var from = message["from"]?.Value<string>();
						var to = message["to"]?.Value<string>();

						if (ImmutableInterlocked.TryRemove(ref _unbindWaitingList, (from, to), out var tcs))
						{
							tcs.TrySetResult(null);
						}

						break;
					}
				}

				return true;
			}

			return false;
		}

		public Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
		{
			_logger.LogInformation($"Successfully connected to MQTT server {_settings.CurrentSettings.MqttServer}.");

			_stateService.Clear();
			_disconnectWarned = false;

			return Task.CompletedTask;

		}

		private bool _disconnectWarned;

		public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
		{

			StopPolling();

			if (_disconnectWarned)
			{
				_logger.LogDebug(eventArgs.Exception, $"Error connecting to MQTT server {_settings.CurrentSettings.MqttServer}.");
				return Task.CompletedTask;
			}

			_disconnectWarned = true;
			if (eventArgs.ClientWasConnected)
			{
				_logger.LogWarning(eventArgs.Exception, $"Disconnected from MQTT server {_settings.CurrentSettings.MqttServer}.");
			}
			else
			{
				_logger.LogWarning(eventArgs.Exception, $"Unable to connect to MQTT server {_settings.CurrentSettings.MqttServer}.");
			}

			return Task.CompletedTask;

		}

		private ImmutableDictionary<string, TaskCompletionSource<object>> _renameWaitingList = ImmutableDictionary<string, TaskCompletionSource<object>>.Empty;

		public async Task RenameDeviceAndWait(string deviceFriendlyName, string newName)
		{
			var json = JsonConvert.SerializeObject(new { old = deviceFriendlyName, @new = newName });

			var tcs = new TaskCompletionSource<object>();
			if (!ImmutableInterlocked.TryAdd(ref _renameWaitingList, deviceFriendlyName, tcs))
			{
				throw new InvalidOperationException("Another rename in progress for this device.");
			}

			var msg = new MqttApplicationMessageBuilder()
				.WithTopic($"{_settings.CurrentSettings.BaseTopic}/bridge/config/rename")
				.WithPayload(json)
				.Build();

			await _client.PublishAsync(msg);

			await tcs.Task;
		}

		private ImmutableDictionary<string, TaskCompletionSource<object>> _removeWaitingList = ImmutableDictionary<string, TaskCompletionSource<object>>.Empty;

		public async Task RemoveDeviceAndWait(string deviceFriendlyName, bool forceRemove)
		{
			var tcs = new TaskCompletionSource<object>();
			if (!ImmutableInterlocked.TryAdd(ref _removeWaitingList, deviceFriendlyName, tcs))
			{
				throw new InvalidOperationException("Another remove in progress for this device.");
			}

			var msg = new MqttApplicationMessageBuilder()
				.WithTopic($"{_settings.CurrentSettings.BaseTopic}/bridge/config/{(forceRemove ? "force_remove" : "remove")}")
				.WithPayload(deviceFriendlyName)
				.Build();

			await _client.PublishAsync(msg);

			await tcs.Task;
		}

		private ImmutableDictionary<bool, TaskCompletionSource<object>> _allowJoinWaitingList = ImmutableDictionary<bool, TaskCompletionSource<object>>.Empty;

		public async Task AllowJoinAndWait(bool permitJoin)
		{
			var tcs = new TaskCompletionSource<object>();
			if (!ImmutableInterlocked.TryAdd(ref _allowJoinWaitingList, permitJoin, tcs))
			{
				tcs = _allowJoinWaitingList[permitJoin];
			}

			var msg = new MqttApplicationMessageBuilder()
				.WithTopic($"{_settings.CurrentSettings.BaseTopic}/bridge/config/permit_join")
				.WithPayload(permitJoin ? "true" : "false")
				.Build();

			await _client.PublishAsync(msg);

			await tcs.Task;
		}

		public async Task Reset()
		{
			_stateService.Clear();

			await Connect();
		}

		public async Task ConfigureDevice(string deviceFriendlyName)
		{
			var device = _stateService.FindDeviceById(deviceFriendlyName, out _);


			var msg = new MqttApplicationMessageBuilder()
				.WithTopic($"{_settings.CurrentSettings.BaseTopic}/bridge/configure")
				.WithPayload(device.FriendlyName)
				.Build();

			await _client.PublishAsync(msg);
		}

		private ImmutableDictionary<(string source, string target), TaskCompletionSource<object>> _bindWaitingList = ImmutableDictionary< (string, string), TaskCompletionSource<object>>.Empty;

		public async Task Bind(string sourceFriendlyName, string targetFriendlyName)
		{
			var tcs = new TaskCompletionSource<object>();

			var source = _stateService.FindDeviceById(sourceFriendlyName, out var state);
			var target = state.FindDevice(targetFriendlyName);

			var isAwaitable = !(string.IsNullOrEmpty(source.ZigbeeId) || string.IsNullOrEmpty(target.ZigbeeId));

			if (isAwaitable)
			{
				if (!ImmutableInterlocked.TryAdd(ref _bindWaitingList, (source.ZigbeeId, target.ZigbeeId), tcs))
				{
					throw new InvalidOperationException("Another bind in progress for this device.");
				}
			}

			var msg = new MqttApplicationMessageBuilder()
				.WithTopic($"{_settings.CurrentSettings.BaseTopic}/bridge/bind/{sourceFriendlyName}")
				.WithPayload(targetFriendlyName)
				.Build();

			await _client.PublishAsync(msg);


			if (isAwaitable)
			{
				await tcs.Task;
			}
		}

		private ImmutableDictionary<(string source, string target), TaskCompletionSource<object>> _unbindWaitingList = ImmutableDictionary<(string, string), TaskCompletionSource<object>>.Empty;

		public async Task Unbind(string sourceFriendlyName, string targetFriendlyName)
		{
			var tcs = new TaskCompletionSource<object>();

			var source = _stateService.FindDeviceById(sourceFriendlyName, out var state);
			var target = state.FindDevice(targetFriendlyName);

			var isAwaitable = !(string.IsNullOrEmpty(source.ZigbeeId) || string.IsNullOrEmpty(target.ZigbeeId));

			if (isAwaitable)
			{
				if (!ImmutableInterlocked.TryAdd(ref _unbindWaitingList, (source.ZigbeeId, target.ZigbeeId), tcs))
				{
					throw new InvalidOperationException("Another unbind in progress for this device.");
				}
			}

			var msg = new MqttApplicationMessageBuilder()
				.WithTopic($"{_settings.CurrentSettings.BaseTopic}/bridge/unbind/{sourceFriendlyName}")
				.WithPayload(targetFriendlyName)
				.Build();

			await _client.PublishAsync(msg);


			if (isAwaitable)
			{
				await tcs.Task;
			}
		}

		private ImmutableDictionary<MqttLogLevel, TaskCompletionSource<object>> _logLevelWaitingList = ImmutableDictionary<MqttLogLevel, TaskCompletionSource<object>>.Empty;

		public async Task SetLogLevel(MqttLogLevel level)
		{
			var tcs = new TaskCompletionSource<object>();

			if (!ImmutableInterlocked.TryAdd(ref _logLevelWaitingList, level, tcs))
			{
				tcs = _logLevelWaitingList[level];
			}

			var msg = new MqttApplicationMessageBuilder()
				.WithTopic($"{_settings.CurrentSettings.BaseTopic}/bridge/config/log_level")
				.WithPayload(level.ToString())
				.Build();

			await _client.PublishAsync(msg);

			await tcs.Task;
		}
	}
}
