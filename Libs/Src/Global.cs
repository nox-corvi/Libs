using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox
{
    public class Global
    {
        protected static Global _self;

        protected IConfiguration _configuration;
        protected ILogger _logger;

        #region Properties
        public static IConfiguration Configuration { get => _self._configuration; }
        public static ILogger Logger { get => _self._logger; }
        #endregion

        static Global() =>
            _self = new Global();

        public Global()
        {
            _configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json")
                 .Build();

            _logger = Hosting.Hosting.CreateDefaultLogger<Global>(
                bool.Parse(_configuration["global:debug"] ?? "false"),
                bool.Parse(_configuration["global:single_line"] ?? "true"),
                bool.Parse(_configuration["global:include_scope"] ?? "true"));
        }
    }
}
