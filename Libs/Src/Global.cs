using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mysqlx.Resultset;
using Nox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZstdSharp.Unsafe;

namespace Nox
{
    public class Global
    {
        private static readonly object _lock = new object();

        protected static Global _self;

        protected ILoggerFactory _LoggerFactory;

        protected IConfiguration _configuration;
        protected ILogger<Global> _logger;

        #region Properties
        public static IConfiguration Configuration { get => _self._configuration; }
        public static ILogger Logger { get => _self._logger; }

        public static Global Self
        {
            get
            {
                if (_self == null)
                {
                    lock (_lock)
                    {
                        if (_self == null)
                        {
                            _self = new Global();
                        }
                    }
                }
                return _self;
            }
        }
        #endregion

        public static ILogger<T> CreateLogger<T>()
            where T : class
        {
            return Self._LoggerFactory.CreateLogger<T>();
        }

        static Global()
        {
            //lock (_lock)
            //{
            //    _self = new Global();
            //}
        }

        public Global()
        {
            _configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json")
                 .Build();

            _LoggerFactory = LoggerFactory.Create(configure =>
            {
                configure.ClearProviders();

                var MinimumLevel = (LogLevel)Enum.Parse(typeof(LogLevel), _configuration["XLog:Level"] ?? 
                    nameof(LogLevel.Information));

                configure.SetMinimumLevel(MinimumLevel);
                configure.AddProvider(new XLogProvider(_configuration));
            });

            //_logger = Global.CreateLogger<Global>();
        }
    }
}
