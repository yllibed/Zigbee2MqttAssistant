using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zigbee2MqttAssistant.Services;

namespace Zigbee2MqttAssistant
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
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

			services.AddControllersWithViews(c =>
			{
				c.ReturnHttpNotAcceptable = true;
			});

			services.AddSingleton<IBridgeStateService, BridgeStateService>();
			services.AddSingleton<IBridgeOperationService, BrigeOperationService>();
			services.AddSingleton<ISettingsService, SettingsService>();

			services.AddSingleton<MqttConnectionService>();
			services.AddSingleton<IHostedService, MqttConnectionService>(sp => sp.GetService<MqttConnectionService>());

			services.Decorate<IUrlHelperFactory>((previous, _) => new RelativeUrlHelperFactory(previous));

			if(Directory.Exists("/data"))
			{
				services.AddDataProtection()
					.PersistKeysToFileSystem(new DirectoryInfo("/data"));
			}
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

			app.UseRouting();


			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=Home}/{action=Index}/{*id}");
			});
		}
	}
}
