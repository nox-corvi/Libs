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
        private static Global _self;

        private Log4 _Log;

        public static Log4 Log => _self._Log;

        static Global() =>
            _self = new Global();

        public Global()
        {
            _Log = new Log4($"{(new StackFrame(1)).GetMethod().DeclaringType.FullName}.log");
            _Log.LogMessage($"create global instance at {DateTime.UtcNow.ToString()}", Log4.Log4LevelEnum.Debug);
        }
    }
}
