﻿using Nox;
using Nox.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.CI
{
    public partial class CI
    {
        protected Log4 _logger = null!;
        protected ConPrint _Con1 = null!;

        protected ProcessHandler _Process1 = null!;
        protected RegistryHandler _Registry1 = null!;

        protected SecurityHandler _Security1 = null!;

        protected IaCHandler _IaC1 = null!;

        protected Helpers _Helpers1 = null!;

        #region Properties
        public virtual ProcessHandler GetProcessHandler
        {
            get
            {
                if (_Process1 == null)
                {
                    _logger?.LogMessage("create process1 handler", Log4.Log4LevelEnum.Info);
                    _Process1 = new ProcessHandler(this, _logger);
                }
                return _Process1;
            }
        }

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

        public virtual SecurityHandler GetSecurityHandler
        {
            get
            {
                if (_Security1 == null)
                {
                    _logger?.LogMessage("create security1 handler", Log4.Log4LevelEnum.Info);
                    _Security1 = new SecurityHandler(this, _logger);
                }
                return _Security1;
            }
        }

        public virtual IaCHandler GetIaCHandler
        {
            get
            {
                if (_IaC1 == null)
                {
                    _logger?.LogMessage("create iac1 handler", Log4.Log4LevelEnum.Info);
                    _IaC1 = new IaCHandler(this, _logger);
                }
                return _IaC1;
            }
        }

        public virtual Helpers GetHelper
        {
            get
            {
                if (_Helpers1 == null)
                {
                    _logger?.LogMessage("create helper1 object", Log4.Log4LevelEnum.Info);
                    _Helpers1 = new Helpers(this, _logger);
                }

                return _Helpers1;
            }
        }
        #endregion

        #region Con
        public bool CancelWithMessage(string Message, Log4.Log4LevelEnum LogLevel = Log4.Log4LevelEnum.Error)
        {
            _logger?.LogMessage(Message, LogLevel);
            return false;
        }
        #endregion

        public CI() { }

        public CI(Log4 logger)
        {
            _logger = logger;
        }
    }


    public class CIBase
        : IDisposable

    {
        public enum MsgTypeEnum
        {
            _none = 0,
            _fatal = 1, _critical = 2, _error = 3, _warning = 4, _info = 5, _trace = 6,
        }

        public enum ResEnum
        {
            _none = 0,

            // global
            _invalid_argument = 1, _invalid_argument_A = 2, _invalid_argument_AB = 3, _invalid_argument_ABC = 4,

            _b = 0x10,
            _c = 0x20,

            // IaC
            _calc_crc = 0x30, _calc_crc_done = 0x31, 
            _validate_file_crc = 0x32,
            _encode = 0x33, _decode = 0x34, _encode_laverna = 0x35, _decode_laverna = 0x36,
            _calc_crc_fail = 0x3A, _encode_fail = 0x3B, _decode_fail = 0x3C
        }

        protected CI _CI;
        protected Log4 _logger = null!;

        #region Properties
        protected CI CI => _CI;
        #endregion

        #region Resources
        protected string GetMsgType(MsgTypeEnum msg)
        {
            switch (msg)
            {
                case MsgTypeEnum._fatal:
                    return "fatal";
                case MsgTypeEnum._critical:
                    return "critical";
                case MsgTypeEnum._error:
                    return "error";
                case MsgTypeEnum._warning:
                    return "warning";
                case MsgTypeEnum._info:
                    return "info";
                case MsgTypeEnum._trace:
                    return "trace";
                default:
                    return "unknown";
            }
        }
        protected string GetRes(ResEnum res)
        {
            switch ((int)res & 0xFFF0)
            {
                case 0:
                    switch (res)
                    {
                        case ResEnum._invalid_argument:
                            return "invalid argument";
                        case ResEnum._invalid_argument_A:
                            return "invalid argument: {0}";
                        default:
                            break;
                    }
                    break;
                case 1:
                    switch (res)
                    {
                        default:
                            break;
                    }
                    break;

                case 2:
                    switch (res)
                    {
                        default:
                            break;
                    }
                    break;

                case 3:
                    switch (res)
                    {
                        case ResEnum._calc_crc:
                            return "calc crc";
                        case ResEnum._calc_crc_done:
                            return "calc crc done with {0}";

                        case ResEnum._validate_file_crc:
                            return "validate file crc {0}";

                        case ResEnum._encode:
                            return "encode";
                        case ResEnum._decode:
                            return "decode";

                        case ResEnum._encode_laverna:
                            return "encode string LAV://{0}";
                        case ResEnum._decode_laverna:
                            return "decode string LAV://{0}";

                        // errors
                        case ResEnum._calc_crc_fail:
                            return "calc crc fail";
                        case ResEnum._encode_fail:
                            return "encode fail";
                        case ResEnum._decode_fail:
                            return "decode fail";
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            return "unknown";
        }

        protected string Message(string Message) =>
            Message;

        protected string Message(ResEnum res) =>
            $"{GetRes(res)}";

        protected string Message(MsgTypeEnum msg, string Message) =>
            $"{GetMsgType(msg)}: {Message}";

        protected string Message(MsgTypeEnum msg, ResEnum res) =>
            $"{GetMsgType(msg)}: {GetRes(res)}";

        protected string Message(string Message, params string[] Args) =>
           string.Format(Message, Args);

        protected string Message(ResEnum res, params string[] Args) =>
            string.Format($"{GetRes(res)}", Args);

        protected string Message(MsgTypeEnum msg, string Message, params string[] Args) =>
            string.Format($"{GetMsgType(msg)}: {Message}", Args);

        protected string Message(MsgTypeEnum msg, ResEnum res, params string[] Args) =>
            string.Format($"{GetMsgType(msg)}: {GetRes(res)}", Args);


        protected string NullStr(string Arg) =>
            Arg ?? "<null>";
        #endregion

        public virtual void Dispose() { }

        public CIBase(CI CI) =>
            this._CI = CI;

        public CIBase(CI CI, Log4 logger) :
            this(CI) =>
            _logger = logger;
    }
}
