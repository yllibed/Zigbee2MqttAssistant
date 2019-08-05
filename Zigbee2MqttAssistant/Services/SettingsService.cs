using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zigbee2MqttAssistant.Models;

namespace Zigbee2MqttAssistant.Services
{
	public class SettingsService : ISettingsService
	{
		public SettingsService(IConfiguration configuration, ILogger<SettingsService> logger)
		{
			var section = configuration.GetSection("settings");

			if (section.Exists())
			{
				var settingsBuilder = new Settings.Builder();
				section.Bind(settingsBuilder);
				CurrentSettings = settingsBuilder;
			}
			else
			{
				logger.LogWarning(
					"Section 'settings' in configuration does not exists. Will use default settings instead.");
				CurrentSettings = Settings.Default;
			}
		}

		public Settings CurrentSettings { get; }
	}
}
