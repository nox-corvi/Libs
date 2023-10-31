using Nox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Win32.CI
{
    public class CI
        : Nox.CI.CI
    {
        protected RegistryHandler _Registry1 = null!;
        protected WinUpdateControl _WinUpdateControl = null!;


        #region Properties
        public override ProcessHandler GetProcessHandler
        {
            get
            {
                if (_Process1 == null)
                {
                    _logger?.LogMessage("create process handler", Log4.Log4LevelEnum.Info);
                    _Process1 = new ProcessHandler(this, _logger);
                }
                return _Process1 as ProcessHandler;
            }
        }

        public override SecurityHandler GetSecurityHandler
        {
            get
            {
                if (_Security1 == null)
                {
                    _logger?.LogMessage("create security2 handler", Log4.Log4LevelEnum.Info);
                    _Security1 = new SecurityHandler(this, _logger);
                }
                return _Security1 as SecurityHandler;
            }
        }
        //public override IaCHandler GetIaCHandler
        //{
        //    get
        //    {
        //        if (_IaC1 == null)
        //        {
        //            _logger?.LogMessage("create iac handler", Log4.Log4LevelEnum.Info);
        //            _IaC1 = new IaCHandler(this, _logger);
        //        }
        //        return _IaC1;
        //    }
        //}

        public virtual RegistryHandler GetRegistryHandler
        {
            get
            {
                if (_Registry1 == null)
                {
                    _logger?.LogMessage("create registry1 handler", Log4.Log4LevelEnum.Info);
                    _Registry1 = new RegistryHandler(this, _logger);
                }
                return _Registry1;
            }
        }

        public virtual WinUpdateControl GetWinUpdateControl
        {
            get
            {
                if (_WinUpdateControl == null)
                {
                    _logger?.LogMessage("create winupdatecontrol handler", Log4.Log4LevelEnum.Info);
                    _WinUpdateControl = new WinUpdateControl(this, _logger);
                }

                return _WinUpdateControl;
            }
        }

        public override Helpers GetHelper
        {
            get
            {
                if (_Helpers1 == null)
                {
                    _logger?.LogMessage("create helper2 object", Log4.Log4LevelEnum.Info);
                    _Helpers1 = new Helpers(this, _logger);
                }

                return _Helpers1 as Helpers;
            }
        }
        #endregion

        public CI() 
        : base() { } 

        public CI(Log4 logger) 
            : base(logger) { }
    }
}
