using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message.Defaults
{
    public enum DefaultMessageTypeEnum
    {
        EHLO = 0xFCA0,
        RESP = 0xFCA2,
        KEXC = 0xFCB1,
        KEYV = 0xFCB2,
    }
}
