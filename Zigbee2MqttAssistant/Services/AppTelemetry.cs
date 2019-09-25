using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Hosting;
using Zigbee2MqttAssistant.Models;

namespace Zigbee2MqttAssistant.Services
{
	public class AppTelemetry : IAppTelemetry, ITelemetryInitializer, IDisposable
	{
		private readonly TelemetryClient _client;

		private readonly CancellationTokenSource _cts = new CancellationTokenSource();

		public AppTelemetry(ISettingsService settings, ISystemInformation systemInformation)
		{
			TelemetryConfiguration.Active.TelemetryInitializers.Add(this);

			_client = new TelemetryClient(SettingsToTelemetryConfig(settings.CurrentSettings));
		}

		private static TelemetryConfiguration SettingsToTelemetryConfig(Settings settings)
		{
			return null;
			//return new TelemetryConfiguration(settings.TelemetryInstrumentationKey);
		}

		public async void ReportStart()
		{
			while (!_cts.IsCancellationRequested)
			{
				_client.TrackEvent("test");


				await Task.Delay(TimeSpan.FromMinutes(15), _cts.Token);
			}
		}

		public void ReportStop()
		{
		}

		public void ReportError(string message, Exception exception)
		{
		}

		void ITelemetryInitializer.Initialize(Microsoft.ApplicationInsights.Channel.ITelemetry telemetry)
		{
		}

		public void Dispose()
		{
			_cts.Dispose();
		}
	}
}
