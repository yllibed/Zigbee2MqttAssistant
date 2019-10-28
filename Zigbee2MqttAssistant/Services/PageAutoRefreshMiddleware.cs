using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Zigbee2MqttAssistant.Services
{
	/// <summary>
	/// This class will add "refresh" headers to http result.
	/// Implemented as a middleware
	/// </summary>
	public class PageAutoRefreshMiddleware
	{
		private readonly RequestDelegate _next;
		private int? _refresh;

		public PageAutoRefreshMiddleware(RequestDelegate next, ISettingsService settings)
		{
			_next = next;
			_refresh = settings.CurrentSettings.AutoRefreshRate;
		}

		public Task InvokeAsync(HttpContext ctx)
		{
			if (_refresh is int refresh)
			{
				ctx.Response.OnStarting(() =>
				{
					ctx.Response.Headers.Add("Refresh", refresh.ToString(CultureInfo.InvariantCulture));
					return Task.CompletedTask;
				});
			}

			return _next.Invoke(ctx);
		}
	}
}
