namespace Zigbee2MqttAssistant.Services
{
	public interface ISystemInformation
	{
		string OsType { get; }
		string OsVersion { get; }
		bool OsIs64Bits { get; }
		string ProcessorType { get; }
		int ProcessorCount { get; }
		bool IsDocker { get; }
		bool IsHassIo { get; }
		string Version { get; }
		string FullVersion { get; }
		string BuildType { get; }
		string EnvironmentType { get; }
		string Zigbee2MttVersion { get; }
		string CoordinatorVersion { get; }
		string CoordinatorZigbeeId { get; }
		string MqttBroker { get; }
		bool MqttBrokerConnected { get; }
		int NumberOfDevices { get; }
		bool Telemetry { get; }
	}
}
