﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com
{
    public abstract class NetClientSocketListener
        : SocketListener
    {


        public NetClientSocketListener(uint Signature1, System.Net.Sockets.Socket Socket)
            : base(Signature1, Socket) { }
    }
}