using Nox.CI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Win32.CI
{
    public class WinUpdateControl
        : CIBase
    {


        public WinUpdateControl(Nox.CI.CI CI)
            : base(CI) { }

        public WinUpdateControl(Nox.CI.CI CI, Log4 logger)
            : base(CI, logger) { }
    }
}
