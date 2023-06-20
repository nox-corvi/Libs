#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nox.Hosting
{
    public interface IAsyncReader
    {
        Task Read(CancellationToken stoppingToken);
    }

    public interface IReader
    {
        void Read();
    }
}
#endif