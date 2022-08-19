using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Libs.Hosting
{
    public interface IReader
    {
        Task Read(CancellationToken stoppingToken);
    }
}
