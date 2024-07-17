using Microsoft.Extensions.Logging;
using Nox.CI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Win32.CI
{
    public class WinUpdateControl(Nox.CI.CI CI, ILogger Logger)
        : CIBase(CI, Logger)
    {
       
        // DI-Constructor
        public WinUpdateControl(Nox.CI.CI CI, ILogger<WinUpdateControl> Logger)
            : this(CI, (ILogger)Logger) { }
    }
}
