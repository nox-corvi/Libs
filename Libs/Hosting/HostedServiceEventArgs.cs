using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Hosting
{
    public class HostedServiceEventArgs
        : EventArgs
    {
        public IConfiguration Configuration { get; }

        public HostedServiceEventArgs(IConfiguration configuration)
            : base() => Configuration = configuration;
    }
}
