using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.CI.Windows
{
    public class Win32Exception
        : Exception
    {
        private readonly int nativeErrorCode;

        public int NativeErrorCode => nativeErrorCode;

        public Win32Exception(int error, string message)
            : base(message)
        {
            nativeErrorCode = error;
        }

        public Win32Exception(int error, string message, Exception innerException)
            : base(message, innerException)
        {
            nativeErrorCode = error;
        }
    }
}
