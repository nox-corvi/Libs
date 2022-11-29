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
    public class NetServer<T>
        : NetBase
        where T : SocketListener

    {
        private Log4 Log = Log4.Create();

        private string _ServerIP = "";
        private int _ServerPort = -1;

        private TcpListener _Listener = null;
        private List<SocketListener> _ListOfListener;

        private BetterBackgroundWorker _Server;
        private int _ServerWaitTime = 10;

        private BetterBackgroundWorker _Purge;
        private int _PurgeWaitTime = 100;

        #region Properties
        public string ServerIP => _ServerIP;
        public int ServerPort => _ServerPort;

        public Guid Id { get; } = Guid.NewGuid();

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
                    r += _ListOfListener[i]?.ReceiveBufferLength ?? 0;

                return r;
            }
        }
        public int MessageCount
        {
            get
            {
                int r = 0;
                for (int i = 0; i < _ListOfListener.Count; i++)
                    r += _ListOfListener[i]?.MessageCount ?? 0;

                return r;
            }
        }
        #endregion

        public void Bind(string IP, int Port)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, IP, Port);

            StopServer();

            _ServerIP = IP; _ServerPort = Port;
            _Listener = new TcpListener(new IPEndPoint(IPAddress.Parse(IP), Port));

            _ListOfListener = new List<SocketListener>();
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

                        SocketListener.PingMessage += (object sender, MessagePingEventArgs e) =>
                            OnPingMessage(sender, e);
                        SocketListener.EchoMessage += (object sender, MessageEchoEventArgs e) =>
                            OnEchoMessage(sender, e);
                        SocketListener.EhloMessage += (object sender, MessageEhloEventArgs e) =>
                            OnEhloMessage(sender, e);
                        SocketListener.RplyMessage += (object sender, MessageRplyEventArgs e) =>
                            OnRplyMessage(sender, e);
                        SocketListener.SigxMessage += (object sender, MessageSigxEventArgs e) =>
                            OnSigxMessage(sender, e);
                        SocketListener.SigvMessage += (object sender, MessageSigvEventArgs e) =>
                            OnSigvMessage(sender, e);
                        SocketListener.RespMessage += (object sender, MessageRespEventArgs e) =>
                            OnRespMessage(sender, e);

                        SocketListener.ObtainMessage += (object sender, ObtainMessageEventArgs e) =>
                            OnObtainMessage(sender, e);
                        SocketListener.ObtainPublicKey += (object sender, ObtainPublicKeyEventArgs e) =>
                            OnObtainPublicKey(sender, e);

                        SocketListener.CloseSocket += (object sender, CloseSocketEventArgs e) =>
                            OnCloseSocket(sender, e);
                        SocketListener.Message += (object sender, MessageEventArgs e) =>
                            OnMessage(sender, e);

                        lock (_ListOfListener)
                        {
                            _ListOfListener.Add(SocketListener);
                        }

                        SocketListener.StartListener();
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
                        if (_ListOfListener[i]?.Remove ?? false)
                        {
                            _ListOfListener[i].StopListener();
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
                    _ListOfListener[i].StopListener();

                while (_ListOfListener.Count > 0)
                    _ListOfListener.RemoveAt(0);
                _ListOfListener = null;
            }
        }

        public bool SendBufferTo(int Index, byte[] byteBuffer)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, Index, byteBuffer);

            try
            {
                if (_ListOfListener[Index].IsConnected)
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
                if (_ListOfListener[i].IsConnected)
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
}
