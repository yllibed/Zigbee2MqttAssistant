using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zigbee2MqttAssistant.Models;
using Zigbee2MqttAssistant.Models.Devices;
using Zigbee2MqttAssistant.Services;

namespace Zigbee2MqttAssistant.Controllers
{
	public class HomeController : Controller
	{
		private readonly IBridgeStateService _stateService;
		private readonly IBridgeOperationService _operationService;

		public HomeController(IBridgeStateService stateService, IBridgeOperationService operationService)
		{
			_stateService = stateService;
			_operationService = operationService;
		}

		public IActionResult Index()
		{
			var state = _stateService.CurrentState;

			return View(state);
		}

		public IActionResult Device(string id)
		{
			var device = _stateService.FindDeviceById(id, out var state);

			if (device == null)
			{
				return NotFound();
			}

			// find route to coordinator
			var routeToCoordinator = new List<ZigbeeDevice>();
			var parentDevice = device;
			var reachCoordinator = false;
			//while (parentDevice != null)
			//{
			//	if (routeToCoordinator.Any(d => d.ZigbeeId.Equals(parentDevice.ZigbeeId)))
			//	{
			//		break; // cyclic route
			//	}
			//	routeToCoordinator.Add(parentDevice);
			//	if (string.IsNullOrWhiteSpace(parentDevice.ZigbeeId))
			//	{
			//		break;
			//	}

			//	parentDevice = state.Devices.FirstOrDefault(d => d.ZigbeeId?.Equals(parentDevice.ParentZigbeeId) ?? false);
			//	if (parentDevice?.ZigbeeId.Equals(state.CoordinatorZigbeeId) ?? false)
			//	{
			//		reachCoordinator = true;
			//		break;
			//	}
			//}

			DeviceDetailsViewModel vm = new DeviceDetailsViewModel.Builder
			{
				Device = device,
				RouteToCoordinator = routeToCoordinator.ToImmutableArray(),
				RouteReachCoordinator = reachCoordinator
			};

			return View(vm);
		}

		[HttpPost]
		public async Task<IActionResult> RenameDevice(string id, string newName)
		{
			var device = await _operationService.RenameDeviceById(id, newName);

			if (device == null)
			{
				return NotFound();
			}

			return RedirectToAction("Index");
		}

		public async Task<IActionResult> RemoveDevice(string id)
		{
			var device = await _operationService.RemoveDeviceById(id);

			if (device == null)
			{
				return NotFound();
			}

			return RedirectToAction("Index");
		}

		public IActionResult Status()
		{
			return View(_stateService.CurrentState);
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}

		[HttpPost]
		public async Task<IActionResult> AllowJoin(bool permitJoin)
		{
			await _operationService.AllowJoin(permitJoin);


			return RedirectToAction("Status");
		}
	}
}
