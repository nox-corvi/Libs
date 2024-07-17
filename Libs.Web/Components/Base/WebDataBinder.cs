using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Web.Components.Base
{
    internal class WebDataBinder
        : Super.WebComponent
    {
        private Timer? LoadTimer;
        

        public int TimerWaitOnIntitialize { get; set; } = 100;

        private bool _LoadInProgress = false;
        public async Task TimerTickAsync()
        {
            _LoadInProgress = true;
            try
            {
                var cts = new CancellationTokenSource();

                _Logger.LogInformation($"{nameof(WebDataBinder)}::{nameof(TimerTickAsync)}");
                await OnLoadAsync(cts.Token); 
            }
            finally
            {
                _LoadInProgress = false;
            }
            LoadTimer?.Dispose();
        }

        protected override async Task OnInitializeExecuteAsync()
        {
            await base.OnInitializeExecuteAsync();
            LoadTimer = new Timer(new TimerCallback(async _ => await TimerTickAsync()), null,
                TimerWaitOnIntitialize, 0);
        }

        public override void Dispose()
        {
            LoadTimer?.Dispose();
        }


        public WebDataBinder(ILogger Logger)
            : base(Logger) { }

        public WebDataBinder(ILogger<WebDataBinder> Logger)
            : this((ILogger)Logger) { }
    }
}