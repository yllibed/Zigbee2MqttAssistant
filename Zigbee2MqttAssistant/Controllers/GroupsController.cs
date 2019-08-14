using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zigbee2MqttAssistant.Models.Mqtt;
using Zigbee2MqttAssistant.Services;

namespace Zigbee2MqttAssistant.Controllers
{
    public class GroupsController : Controller
    {
	    private readonly IBridgeStateService _stateService;
	    private readonly IBridgeOperationService _operationService;

	    public GroupsController(IBridgeStateService stateService, IBridgeOperationService operationService)
	    {
		    _stateService = stateService;
		    _operationService = operationService;
	    }

        public IActionResult Index()
        {
	        return View(_stateService.CurrentState);
        }

        public IActionResult Group(string id)
        {
	        var state = _stateService.CurrentState;

	        if (!string.IsNullOrWhiteSpace(id))
	        {
		        var group = state.Groups.FirstOrDefault(g => g.Id.Equals(id));

		        if (group != null)
		        {
			        return View((state, group));
		        }
	        }

	        return RedirectToAction("Index");
		}

		[HttpPost]
        public async Task<IActionResult> New(string groupName)
        {
	        if (string.IsNullOrWhiteSpace(groupName))
	        {
		        return RedirectToAction("Index");
	        }

	        var id = await _operationService.NewGroup(groupName);

			throw new NotImplementedException();
        }
    }
}
