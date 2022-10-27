using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Hosting
{
    public class Config
    {
        public string Filename { get; set; } = "";
        public string Path { get; set; } = "";

        public static Config Simple(string Filename) =>
           new Config() { Filename = Filename};
    }

    public class Environment
    {
        public bool AddEnvironmentVariables { get; set; } = true;
    }

    public class HostedServiceConfiguration
    {
        public EventHandler<HostedServiceEventArgs> Initialize;

        public Config Config { get; } = new();
        public Environment Environment { get; } = new();

        public string[] CommandlineArgs { get; set; }

        public HostedServiceConfiguration() { }
        public HostedServiceConfiguration(Config config)
            : this() =>
            this.Config = config;

        public HostedServiceConfiguration(Config config, Action<object, HostedServiceEventArgs> initialize)
            : this(config) =>
            this.Initialize += (object sender, HostedServiceEventArgs e) =>
                initialize?.Invoke(sender, e);
            
    }
}
