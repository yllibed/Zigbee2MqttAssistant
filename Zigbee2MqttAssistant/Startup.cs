using System.IO;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zigbee2MqttAssistant.Services;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace Zigbee2MqttAssistant
{
	public class Startup
	{
		private readonly ILogger<Startup> _logger;

		public Startup(IConfiguration configuration, ILogger<Startup> logger)
		{
			Configuration = configuration;
			_logger = logger;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.Configure<CookiePolicyOptions>(options =>
			{
				// This lambda determines whether user consent for non-essential cookies is needed for a given request.
				options.CheckConsentNeeded = context => true;
				options.MinimumSameSitePolicy = SameSiteMode.None;
			});

			var appSettings = SettingsService.GetFromConfiguration(Configuration);
			services.AddApplicationInsightsTelemetry(o =>
			{
				o.EnableHeartbeat = true;

				if (appSettings?.TelemetryOptOut != true)
				{
					_logger.LogWarning("Telemetry is activated using ApplicationInsight.\n --> See https://github.com/yllibed/Zigbee2MqttAssistant/blob/master/TELEMETRY.md for more information.");
				}
				else
				{
					o.InstrumentationKey = "";
				}
			});

			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

			services.AddSingleton<IBridgeStateService, BridgeStateService>();
			services.AddSingleton<IBridgeOperationService, BrigeOperationService>();
			services.AddSingleton<ISettingsService, SettingsService>();
			services.AddSingleton<ISystemInformation, SystemInformation>();

			services.AddSingleton<MqttConnectionService>();
			services.AddSingleton<IHostedService, MqttConnectionService>(sp => sp.GetService<MqttConnectionService>());

			//services.AddSingleton<IAppTelemetry, AppTelemetry>();

			services.Decorate<IUrlHelperFactory>((previous, _) => new RelativeUrlHelperFactory(previous));

			if(Directory.Exists("/data"))
			{
				services.AddDataProtection()
					.PersistKeysToFileSystem(new DirectoryInfo("/data"));
			}
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
			}

			app.UseStaticFiles();

			app.UseMvc(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}
