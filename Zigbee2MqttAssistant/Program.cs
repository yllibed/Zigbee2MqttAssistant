using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Zigbee2MqttAssistant
{
	public class Program
	{
		public static void Main(string[] args)
		{
			PrintProductVersion();
			CreateWebHostBuilder(args).Build().Run();
		}

		public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration((hostingContext, config) =>
				{
					config.AddEnvironmentVariables(prefix: "Z2MA_"); // for docker ex: Z2MA_SETTINGS__MQTTSERVER
					config.AddJsonFile("/data/options.json", optional: true); // for HASS.IO
				})
				.UseStartup<Startup>();

		private static void PrintProductVersion()
		{
			var assembly = typeof(Program).Assembly;
			var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
			var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

			Console.WriteLine($"Starting {product} v{version}...");
		}
	}
}
