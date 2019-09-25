using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;

namespace Zigbee2MqttAssistant.Services
{
	public class SystemInformation : ISystemInformation
	{
		private readonly IBridgeStateService _stateService;

		public SystemInformation(IHostingEnvironment env, IBridgeStateService stateService, ISettingsService settingsService)
		{
			_stateService = stateService;

			OsType = RuntimeInformation.OSDescription + "/" + RuntimeInformation.OSArchitecture;
			OsVersion = Environment.OSVersion.Version.ToString();
			OsIs64Bits = Environment.Is64BitOperatingSystem;
			ProcessorCount = Environment.ProcessorCount;
			ProcessorType = RuntimeInformation.ProcessArchitecture.ToString();

			if (bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var isDocker))
			{
				IsDocker = isDocker;
			}

			if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HASSIO_TOKEN")))
			{
				IsHassIo = true;
			}

			var assembly = GetType().Assembly;
			Version =
				assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version
				?? assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
				?? "<unknown>";
			FullVersion =
				assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
				?? "<unknown>";
			BuildType =
				assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration
				?? "<unknown>";
			EnvironmentType = env.EnvironmentName;

			Telemetry = !settingsService.CurrentSettings.TelemetryOptOut;
		}

		public string OsType { get; }
		public string OsVersion { get; }
		public bool OsIs64Bits { get; }
		public int ProcessorCount { get; }
		public string ProcessorType { get; }
		public bool IsDocker { get; }
		public bool IsHassIo { get; }
		public string Version { get; }
		public string FullVersion { get; }
		public string BuildType { get; }
		public string EnvironmentType { get; }
		public string Zigbee2MttVersion => _stateService.CurrentState.Zigbee2MqttVersion;
		public string CoordinatorVersion => _stateService.CurrentState.CoordinatorVersion;
		public string CoordinatorZigbeeId => _stateService.CurrentState.CoordinatorZigbeeId;
		public string MqttBroker => _stateService.CurrentState.MqttBroker;
		public bool MqttBrokerConnected => _stateService.CurrentState.MqttConnected;
		public int NumberOfDevices => _stateService.CurrentState.Devices.Length;
		public bool Telemetry { get; }
		public DateTimeOffset StartTime { get; } = DateTimeOffset.Now;
	}
}
