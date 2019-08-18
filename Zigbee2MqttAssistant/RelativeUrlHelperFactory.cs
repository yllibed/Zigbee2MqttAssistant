using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Zigbee2MqttAssistant
{
	public class RelativeUrlHelperFactory : IUrlHelperFactory
	{
		private readonly IUrlHelperFactory _previous;

		public RelativeUrlHelperFactory(IUrlHelperFactory previous)
		{
			_previous = previous;
		}

		public IUrlHelper GetUrlHelper(ActionContext context)
		{
			var inner = _previous.GetUrlHelper(context);

			return new RelativeUrlHelper(inner, context.HttpContext);
		}


		private class RelativeUrlHelper : IUrlHelper
		{
			private readonly IUrlHelper _inner;
			private readonly HttpContext _contextHttpContext;

			public RelativeUrlHelper(IUrlHelper inner, HttpContext contextHttpContext)
			{
				_inner = inner;
				_contextHttpContext = contextHttpContext;
			}

			private string MakeUrlRelative(string url)
			{
				if (url == null)
				{
					return null;
				}

				if (url.Length == 0 || url[0] != '/')
				{
					return url; // that's an url going elsewhere: no need to be relative
				}

				if (url.Length > 2 && url[1] == '/')
				{
					return url; // That's a "//" url, means it's like an absolute one using the same scheme
				}

				// This is not a well-optimized algorithm, but it works!
				// You're welcome to improve it.
				var deepness = _contextHttpContext.Request.Path.Value.Split('/').Length - 2;

				if (deepness == 0)
				{
					return url.Substring(1);
				}
				else
				{
					for (var i = 0; i < deepness; i++)
					{
						url = i == 0 ? ".." + url : "../" + url;
					}
				}

				return url;
			}

			public string Action(UrlActionContext actionContext)
			{
				return MakeUrlRelative(_inner.Action(actionContext));
			}

			public string Content(string contentPath)
			{
				return MakeUrlRelative(_inner.Content(contentPath));
			}

			public bool IsLocalUrl(string url)
			{
				if (url?.StartsWith("../") ?? false)
				{
					return true;
				}

				return _inner.IsLocalUrl(url);
			}

			public string RouteUrl(UrlRouteContext routeContext) => _inner.RouteUrl(routeContext);

			public string Link(string routeName, object values) => _inner.Link(routeName, values);

			public ActionContext ActionContext => _inner.ActionContext;
		}
	}
}
