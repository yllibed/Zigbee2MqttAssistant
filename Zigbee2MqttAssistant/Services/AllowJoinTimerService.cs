using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Zigbee2MqttAssistant.Models.Mqtt;

namespace Zigbee2MqttAssistant.Services
{
	public class AllowJoinTimerService : IHostedService, IDisposable
	{
		private readonly IBridgeStateService _stateService;
		private readonly ISettingsService _settingsService;
		private readonly IBridgeOperationService _operationsService;

		// SerialDisposable: when its .Disposable is null, it means there's no ongoing timer
		private readonly SerialDisposable _disposable = new SerialDisposable();

		public AllowJoinTimerService(
			IBridgeStateService stateService,
			ISettingsService settingsService,
			IBridgeOperationService operationsService)
		{
			_stateService = stateService;
			_settingsService = settingsService;
			_operationsService = operationsService;
		}

		public async Task StartAsync(CancellationToken ct)
		{
			if (_settingsService.CurrentSettings.AllowJoinTimout > 0)
			{
				_stateService.StateChanged += OnStateChanged;
			}
		}

		private void OnStateChanged(object sender, Bridge e)
		{
			if (e.PermitJoin && _disposable.Disposable == null)
			{
				// Allow join newly activated
				var cts = new CancellationTokenSource();
				_disposable.Disposable = Disposable.Create(Cancel);
				var t = StartTimer(cts.Token);

				void Cancel()
				{
					cts.Cancel();
					cts.Dispose();
				}
			}
			if (!e.PermitJoin && _disposable.Disposable != null)
			{
				// Allow join deactivated - no need to wait anymore
				_disposable.Disposable = null;
			}

		}

		private async Task StartTimer(CancellationToken ct)
		{
			// Get timeout from config
			var timeout = TimeSpan.FromMinutes(_settingsService.CurrentSettings.AllowJoinTimout);
			await Task.Delay(timeout, ct);

			// timeout reached here
			_disposable.Disposable = null;
			await _operationsService.AllowJoin(false);
		}

		public async Task StopAsync(CancellationToken ct)
		{
			// Unregister from event handler
			_stateService.StateChanged -= OnStateChanged;

			// Terminate any ongoing timer
			_disposable.Disposable = null;
		}

		public void Dispose() => _disposable.Dispose();
	}
}
