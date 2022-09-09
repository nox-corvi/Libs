using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nox.Net
{
    public abstract class NetServer
    {
        public abstract string IP { get; }
        public abstract int Port { get; }

        private TcpListener _Listener = null;
        private List<SocketListener> _ListOfListener;

        private Thread _ServerThread;
        private Thread _PurgeThread;

        private bool _StopServer = false;
        private bool _StopPurge = false;

        public void Initialize()
        {
            StopServer();

            _Listener = new TcpListener(new IPEndPoint(IPAddress.Parse(IP), Port));

            _ListOfListener = new List<SocketListener>();

            _Listener.Start();
            _ServerThread = new Thread(new ThreadStart(ServerThreadStart));
            _ServerThread.Start();

            _PurgeThread = new Thread(new ThreadStart(PurgeThreadStart));
            _PurgeThread.Priority = ThreadPriority.Lowest;
            _PurgeThread.Start();
        }

        public void StopServer()
        {
            if (_Listener != null)
            {
                _StopServer = true;
                _Listener.Stop();

                _ServerThread.Join(1000);
                _ServerThread = null;

                _StopPurge = true;
                _PurgeThread.Join(1000);
                _PurgeThread = null;

                // Free Server Object.
                _Listener = null;

                // Stop All clients.
                StopAllListers();
            }
        }

        private void StopAllListers()
        {
            for (int i = 0; i < _ListOfListener.Count; i++)
                _ListOfListener[i].StopListener();

            _ListOfListener.Clear();
            _ListOfListener = null;
        }

        public abstract SocketListener CreateSocketListener(Socket socket);

        private void ServerThreadStart()
        {
            // Client Socket variable;
            Socket _ClientSocket = null;
            SocketListener _SocketListener = null;

            while (!_StopServer)
            {
                try
                {
                    if (_Listener.Pending())
                    {
                        _ClientSocket = _Listener.AcceptSocket();

                        // create using abstract method
                        _SocketListener = CreateSocketListener(_ClientSocket);
                        lock (_ListOfListener)
                        {
                            _ListOfListener.Add(_SocketListener);
                        }
                        
                        _SocketListener.StartListener();
                    }
                    else
                        Thread.Sleep(0);
                }
                catch (SocketException se)
                {
                    _StopServer = true;
                }
            }
        }

        private void PurgeThreadStart()
        {
            while (!_StopPurge)
            {
                lock (_ListOfListener)
                {
                    for (int i = _ListOfListener.Count; i >= 0; i--)
                    {
                        if (_ListOfListener[i].Delete)
                        {
                            _ListOfListener[i].StopListener();
                            _ListOfListener.RemoveAt(i);
                        }
                    }
                }

                Thread.Sleep(10000);
            }
        }

        ~NetServer() =>
            StopServer();
    }

    public abstract class SocketListener
    {
        public abstract int ReceiveTimeout { get; }

        private Socket _Socket = null;
        private Thread _ListenerThread = null;

        private bool _StopClient = false;
        private bool _Delete = false;

        private DateTime _LastResponse = DateTime.UtcNow;

        public bool Delete =>
            _Delete;

        public void StartListener()
        {
            if (_Socket != null)
                (_ListenerThread = new Thread(new ThreadStart(ListenerThreadStart))).Start();
        }

        private void ListenerThreadStart()
        {
            int size = 0;
            Byte[] byteBuffer = new Byte[1024];

            _LastResponse = DateTime.UtcNow;

            Timer t = new Timer(t =>
            {
                // stop if receivetimout reached
                if (DateTime.UtcNow.Subtract(_LastResponse).TotalSeconds > ReceiveTimeout)
                    StopListener();
            }, null, ReceiveTimeout, ReceiveTimeout);

            while (!_StopClient)
            {
                try
                {
                    size = _Socket.Receive(byteBuffer);
                    _LastResponse = DateTime.Now;

                    ParseReceiveBuffer(byteBuffer, size);
                }
                catch (SocketException)
                {
                    _StopClient = true;
                    _Delete = true;
                }
            }

            t.Change(Timeout.Infinite, Timeout.Infinite);
            t = null;
        }

        public void StopListener()
        {
            if (_Socket != null)
            {
                _StopClient = true;
                _Socket.Close();

                _ListenerThread.Join(1000);
                _ListenerThread = null;

                _Socket = null;
                _Delete = true;
            }
        }

        public abstract void ParseReceiveBuffer(Byte[] byteBuffer, int size);

        public SocketListener(Socket Socket) =>
            _Socket = Socket;

        ~SocketListener() =>
            StopListener();
    }
}