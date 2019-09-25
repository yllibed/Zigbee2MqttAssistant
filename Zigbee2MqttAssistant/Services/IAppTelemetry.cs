using System;

namespace Zigbee2MqttAssistant.Services
{
	public interface IAppTelemetry
	{
		void ReportStart();
		void ReportStop();

		void ReportError(string message, Exception exception);
	}
}
