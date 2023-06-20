#if NETCOREAPP
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Hosting
{
    public class HostedEventArgs
        : EventArgs
    {
        public IConfiguration Configuration { get; }

        public HostedEventArgs(IConfiguration configuration)
            : base() => Configuration = configuration;
    }
}
#endif