using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zigbee2MqttAssistant.Models;
using Zigbee2MqttAssistant.Services;

namespace Zigbee2MqttAssistant.Controllers
{
	public class HomeController : Controller
	{
		private readonly IBridgeStateService _stateService;

		public HomeController(IBridgeStateService stateService)
		{
			_stateService = stateService;
		}

		public IActionResult Index()
		{
			var state = _stateService.CurrentState;

			return View(state);
		}

		public IActionResult Status()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
