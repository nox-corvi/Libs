using Nox.Net.Com.Message;
using Nox.Security;
using Nox.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Nox.Net.Com
{
    public abstract class NetServer<T>
        : NetBase, INetServer where T : SocketListener
    {
        private Log4 Log = Log4.Create();

        private TcpListener _Listener = null;
        private List<SocketListener> _ListOfListener = new List<SocketListener>();

        private BetterBackgroundWorker _Server;
        private int _ServerWaitTime = 10;

        private BetterBackgroundWorker _Purge;
        private int _PurgeWaitTime = 100;

        #region Properties
        public string ServerIP { get; set; } = "";
        public int ServerPort { get; set; } = -1;

        public string SocketMessage { get; set; }

        public byte[] publicKey { get; set; } = null!;

        public int ClientConnectionCount =>
            _ListOfListener?.Count() ?? 0;

        public int ReceiveBufferLength
        {
            get
            {
                int r = 0;
                for (int i = 0; i < _ListOfListener.Count; i++)
                    r += _ListOfListener[i]?.SocketBufferLength ?? 0;

                return r;
            }
        }
        public int MessageCount
        {
            get
            {
                int r = 0;
                for (int i = 0; i < _ListOfListener.Count; i++)
                    r += _ListOfListener[i]?.MessageBufferLength ?? 0;

                return r;
            }
        }
        #endregion

        #region Events
        public event EventHandler<MessageEventArgs> BindSocket;
        #endregion

        #region OnRaiseEvent Methods
        public void OnBindSocket(object sender, MessageEventArgs e)
            => BindSocket?.Invoke(sender, e);
        #endregion

        protected abstract void BindEvents(T SocketListener);

        public virtual void Bind(string IP, int Port)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, IP, Port);

            StopServer();

            ServerIP = IP; ServerPort = Port;
            _Listener = new TcpListener(new IPEndPoint(IPAddress.Parse(IP), Port));

            //_ListOfListener = new List<SocketListener>();
            _Listener.Start();

            _Server = new BetterBackgroundWorker();
            _Server.DoWork += new DoWorkEventHandler(Server_DoWork);
            _Server.Run();

            _Purge = new BetterBackgroundWorker();
            _Purge.DoWork += new DoWorkEventHandler(Purge_DoWork);
            _Purge.Run();
        }



        private void Server_DoWork(object sender, DoWorkEventArgs e)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, sender, e);

            var worker = sender as BetterBackgroundWorker;

            while (!worker.CancellationPending)
            {
                try
                {
                    if (_Listener.Pending())
                    {
                        var ClientSocket = _Listener.AcceptSocket();

                        // create using abstract method
                        var SocketListener = (T)Activator.CreateInstance(typeof(T), Signature1, ClientSocket);

                        BindEvents(SocketListener);
                        lock (_ListOfListener)
                        {
                            _ListOfListener.Add(SocketListener);
                        }

                        SocketListener.Run();
                    }
                    else
                        Thread.Sleep(_ServerWaitTime);
                }
                catch (SocketException)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void Purge_DoWork(object sender, DoWorkEventArgs e)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, sender, e);

            var worker = sender as BetterBackgroundWorker;

            while (!worker.CancellationPending)
            {
                lock (_ListOfListener)
                {
                    for (int i = _ListOfListener.Count - 1; i >= 0; i--)
                        if (_ListOfListener[i]?.CanPurge ?? false)
                        {
                            _ListOfListener[i].Done();
                            _ListOfListener.RemoveAt(i);
                        }
                }

                Thread.Sleep(_PurgeWaitTime);
            }
        }

        public void StopServer()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);

            // no further more connections
            if (_Server?.IsBusy ?? false)
            {
                _Listener.Stop();

                _Server.Cancel();
                while (_Server.IsBusy)
                    Thread.Sleep(100);
            }
            StopAllListers();


            if (_Purge?.IsBusy ?? false)
            {
                _Purge.Cancel();
                while (_Purge.IsBusy)
                    Thread.Sleep(100);
            }
        }

        private void StopAllListers()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);

            if (_ListOfListener != null)
            {
                for (int i = 0; i < _ListOfListener.Count; i++)
                    _ListOfListener[i].Done();

                while (_ListOfListener.Count > 0)
                    _ListOfListener.RemoveAt(0);
                //_ListOfListener = null;
            }
        }

        public bool SendBufferTo(int Index, byte[] byteBuffer)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, Index, byteBuffer);

            try
            {
                if (_ListOfListener[Index].IsInitialized)
                {
                    _ListOfListener[Index].SendBuffer(byteBuffer);
                    return true;
                }
                else
                    return true;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        public int SendBuffer(byte[] byteBuffer)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, byteBuffer);

            int Result = 0;

            for (int i = 0; i < _ListOfListener.Count; i++)
                if (_ListOfListener[i].IsInitialized)
                    if (SendBufferTo(i, byteBuffer))
                        Result++;

            return Result;
        }

        public void SendDataBlockTo(int Index, DataBlock data)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, Index, data);

            SendBufferTo(Index, data.Write());
        }

        public int SendDataBlock(DataBlock data)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, data);
            return SendBuffer(data.Write());
        }

        public override void Dispose()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);
            StopServer();
        }

        public NetServer(uint Signature1)
        : base(Signature1) { }

        ~NetServer()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);
            StopServer();
        }
    }

    public class NetGenericServer<T>
         : NetServer<T>, INetGenericMessages where T : GenericSocketListener
    {
        #region Events
        public event EventHandler<PingMessageEventArgs> PingMessage;
        public event EventHandler<EchoMessageEventArgs> EchoMessage;
        public event EventHandler<RespMessageEventArgs> RespMessage;
        public event EventHandler<ObtainMessageEventArgs> ObtainMessage;
        public event EventHandler<ObtainCancelMessageEventArgs> ObtainCancelMessage;
        #endregion

        #region OnRaiseEvent Methods
        public void OnPingMessage(object sender, PingMessageEventArgs e)
            => PingMessage?.Invoke(sender, e);

        public void OnEchoMessage(object sender, EchoMessageEventArgs e)
            => EchoMessage?.Invoke(sender, e);  

        public void OnRespMessage(object sender, RespMessageEventArgs e)
            => RespMessage?.Invoke(sender, e);

        public void OnObtainMessage(object sender, ObtainMessageEventArgs e)
            => ObtainMessage?.Invoke(sender, e);    

        public void OnObtainCancelMessage(object sender, ObtainCancelMessageEventArgs e)
            => ObtainCancelMessage?.Invoke(sender, e);  
        #endregion

        protected override void BindEvents(T SocketListener)
        {
            SocketListener.PingMessage += OnPingMessage;
            SocketListener.EchoMessage += OnEchoMessage;
            SocketListener.RespMessage += OnRespMessage;
            SocketListener.ObtainMessage += OnObtainMessage;
            SocketListener.ObtainCancelMessage += OnObtainCancelMessage;
        }

        public NetGenericServer(uint Signature1)
            : base(Signature1) { }
    }


    public class NetSecureServer<T>
        : NetGenericServer<T>, INetSecureServerMessages where T : SecureSocketListener    
    {


        #region Events
        public event EventHandler<RplyEventArgs> RplyMessage;
        public event EventHandler<SigvEventArgs> SigvMessage;
        public event EventHandler<KeyvEventArgs> KeyvMessage;
        public event EventHandler<PublicKeyEventArgs> ObtainPublicKey;
        #endregion

        #region OnRaiseEvent Methods
        public void OnRplyMessage(object sender, RplyEventArgs e)
            => RplyMessage?.Invoke(sender, e);

        public void OnSigvMessage(object sender, SigvEventArgs e)
            => SigvMessage?.Invoke(sender, e);
        public void OnKeyvMessage(object sender, KeyvEventArgs e)
            => KeyvMessage?.Invoke(sender, e);

        public void OnObtainPublicKey(object sender, PublicKeyEventArgs e)
            =>ObtainPublicKey?.Invoke(sender, e);
        #endregion

        protected override void BindEvents(T SocketListener)
        {
            base.BindEvents(SocketListener);

            SocketListener.RplyMessage += OnRplyMessage;
            SocketListener.SigvMessage += OnSigvMessage;
            SocketListener.KeyvMessage += OnKeyvMessage;
            SocketListener.ObtainPublicKey += OnObtainPublicKey;
        }

        public NetSecureServer(uint Signature1)
           : base(Signature1) { }
    }
}
