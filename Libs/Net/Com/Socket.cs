using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Metadata;

namespace Nox.Net.Com
{
    public class Socket
        : System.Net.Sockets.Socket
    {
        public Socket(SocketType socketType, ProtocolType protocolType)
            : base(socketType, protocolType) { }

        public Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType) 
            : base(addressFamily, socketType, protocolType) { }

        public Socket(SafeSocketHandle handle) 
            : base(handle) { }
    }
}
