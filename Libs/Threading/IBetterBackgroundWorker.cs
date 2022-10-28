using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nox.Threading
{
    public interface IBetterBackgroundWorker
    {
        event DoWorkEventHandler DoWork;

        void Run(object Argument = null);
        void Cancel();
    }
}
