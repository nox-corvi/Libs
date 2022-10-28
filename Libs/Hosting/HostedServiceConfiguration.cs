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

    public class HostedConfiguration
    {
        public EventHandler<HostedEventArgs> Initialize;

        public Config Config { get; } = new();
        public Environment Environment { get; } = new();

        public string[] CommandlineArgs { get; set; }

        public HostedConfiguration() { }
        public HostedConfiguration(Config config)
            : this() =>
            this.Config = config;

        public HostedConfiguration(Config config, Action<object, HostedEventArgs> initialize)
            : this(config) =>
            this.Initialize += (object sender, HostedEventArgs e) =>
                initialize?.Invoke(sender, e);
            
    }
}
