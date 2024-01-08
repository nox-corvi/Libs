using Nox.IO.DF;
using Nox.IO.FS;
using Nox.Net.Com;
using Nox.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nox.Data
{
    public interface IRRDBMaster
    {

    }

    public interface IRRDBGuardian
    : IGuardian
    {

    }
     
    public class RRDB<T, U>
        : DF<IRRDBGuardian>
        where T : class, IRRDBGuardian
        where U : class, IRRDBMaster, new()
    {
        protected class RRDBGuardian
            : DFGuardian, IRRDBGuardian
        {
        }

        public RRDB(IRRDBGuardian Guardian, string Filename) 
            : base(Guardian, Filename)
        {
        }
    }
}
