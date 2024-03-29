﻿using Microsoft.Extensions.Logging;
using Nox.Net.Com.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com
{
    public class NetSocket
       : NetBase, INetSocket, IRunner
    {
        public readonly ILogger _Logger;

        #region Properties
        public virtual bool IsInitialized { get; protected set; } = false;
        #endregion

        #region Events
        public event EventHandler<MessageEventArgs> BindSocket;
        public event EventHandler<MessageEventArgs> ConnectClientSocket;
        #endregion

        #region OnRaiseEvent Methods
        public void OnBindSocket(object sender, MessageEventArgs e)
            => BindSocket?.Invoke(sender, e);

        public void OnConnectClientSocket(object sender, MessageEventArgs e)
            => ConnectClientSocket.Invoke(sender, e);
        #endregion

        public virtual void Initialize()
        {
            
        }

        public virtual void Run()
        {
            //
        }

        public virtual void Done()
        {
            //
        }

        public NetSocket(uint Signature1, ILogger<NetSocket> logger)
            : base(Signature1)
            => _Logger = logger;

        public NetSocket(uint Signature1)
            : this(Signature1, null) { } 
    }
}
