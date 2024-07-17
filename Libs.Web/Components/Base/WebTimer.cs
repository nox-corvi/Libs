using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Web.Components.Base
{
    public class WebTimer
        : Super.WebComponent
    {
        private DateTime NextLoad = DateTime.MaxValue;
        private Timer? RefreshTimer;
        private bool _LoadInProgress = false;

        public int TimerRepeatCounter { get; set; } = 500;
        public int TimerWaitOnIntitialize { get; set; } = 100;

        public int LoadRepeatCounter { get; set; } = 30;
        public int LoadWaitOnInitialize { get; set; } = 0;

        public async Task TimerTickAsync()
        {
            var Difference = (int)(NextLoad - DateTime.Now).TotalSeconds;
            if ((Difference <= 0) && (!_LoadInProgress))
            {
                _LoadInProgress = true;
                try
                {
                    var cts = new CancellationTokenSource();
                    if (LoadWaitOnInitialize > 0)
                    {
                        // wait for execution ... 
                        _Logger.LogInformation($"{nameof(TimerTickAsync)} will delay execution for {LoadWaitOnInitialize} seconds");
                        await Task.Delay(LoadWaitOnInitialize * 1000);
                    }
                    await OnLoadAsync(cts.Token);

                    // if a repetition is desired ...
                    if (LoadRepeatCounter > 0)
                    {
                        _Logger.LogInformation($"{nameof(TimerTickAsync)} will repeat again in {LoadRepeatCounter} seconds");
                        NextLoad = DateTime.Now + TimeSpan.FromSeconds(LoadRepeatCounter);
                        Difference = LoadRepeatCounter;
                    } else
                        _Logger.LogInformation($"{nameof(TimerTickAsync)} will not repeat any more");
                }
                finally
                {
                    _LoadInProgress = false;
                }
            }

            LoadRepeatCounter = Difference;
        }

        protected override async Task OnInitializeExecuteAsync()
        {
            await base.OnInitializeExecuteAsync();
            RefreshTimer = new Timer(new TimerCallback(async _ => await TimerTickAsync()), null,
                TimerWaitOnIntitialize, TimerRepeatCounter);
        }

        public override void Dispose()
        {
            RefreshTimer?.Dispose();
        }

        public WebTimer()
            : base()
        {

        }

        public WebTimer(ILogger Logger)
            : base(Logger) { }

        public WebTimer(ILogger<WebTimer> Logger) 
            : this((ILogger)Logger) { }
    }
}
