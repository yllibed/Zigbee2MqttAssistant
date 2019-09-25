using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zigbee2MqttAssistant.Models;

namespace Zigbee2MqttAssistant.Services
{
	public class SettingsService : ISettingsService
	{
		public SettingsService(IConfiguration configuration, ILogger<SettingsService> logger)
		{
			var settings = GetFromConfiguration(configuration);
			if(settings == null)
			{
				logger.LogWarning(
					"Section 'settings' in configuration does not exists. Will use default settings instead.");
				CurrentSettings = Settings.Default;
			}
			else
			{
				CurrentSettings = settings;
			}
		}

		internal static Settings GetFromConfiguration(IConfiguration configuration)
		{
			var section = configuration.GetSection("settings");

			if (section.Exists())
			{
				var settingsBuilder = new Settings.Builder();
				section.Bind(settingsBuilder);
				return settingsBuilder;
			}

			return null;
		}

		public Settings CurrentSettings { get; }
	}
}
