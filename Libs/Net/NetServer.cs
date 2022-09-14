using Nox.CI;
using Nox.Component;
using Nox.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nox.Net
{
    public abstract class NetServer<T> : IDisposable where T : SocketListener
    {
        public abstract string IP { get; }
        public abstract int Port { get; }

        private TcpListener _Listener = null;
        private Dictionary<Guid, SocketListener> _ListOfListener;

        private Thread _ServerThread;
        private Thread _PurgeThread;

        private bool _StopServer = false;
        private bool _StopPurge = false;


        public int ClientConnectionCount =>
            _ListOfListener?.Count() ?? 0;

        public void Initialize()
        {
            StopServer();

            _Listener = new TcpListener(new IPEndPoint(IPAddress.Parse(IP), Port));

            _ListOfListener = new Dictionary<Guid, SocketListener>();

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

        private void ServerThreadStart()
        {
            // Client Socket variable;
            Socket _ClientSocket = null;
            T _SocketListener = null;

            while (!_StopServer)
            {
                try
                {
                    if (_Listener.Pending())
                    {
                        _ClientSocket = _Listener.AcceptSocket();

                        // create using abstract method
                        _SocketListener = (T)Activator.CreateInstance(typeof(T), _ClientSocket);
                        lock (_ListOfListener)
                        {
                            _ListOfListener.Add(_SocketListener.Id, _SocketListener);
                        }

                        _SocketListener.StartListener();
                    }
                    else
                        Thread.Sleep(0);
                }
                catch (SocketException)
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
                    var Keys = _ListOfListener.Keys.ToArray();
                    for (int i = 0; i <  Keys.Length; i++)
                        if (_ListOfListener[Keys[i]].Delete)
                        {
                            _ListOfListener[Keys[i]].StopListener();
                            _ListOfListener.Remove(Keys[i]);
                        }
                }

                Thread.Sleep(500);
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        ~NetServer() =>
            StopServer();
    }

    public abstract class NetClient<T> : IDisposable where T : SocketListener
    {
        private SocketListener _Listener;
        private TcpClient _Client;

        public bool IsConnected =>
           _Listener?.IsConnected ?? false;

        public virtual void Connect(string IP, int Port)
        {
            StopClient();

            _Client = new TcpClient();
            _Client.Connect(new IPEndPoint(IPAddress.Parse(IP), Port));
            _Listener = (T)Activator.CreateInstance(typeof(T), _Client.Client);
            _Listener.StartListener();
        }

        public void SendBuffer(byte[] byteBuffer) =>
            _Listener.SendBuffer(byteBuffer);

        public void StopClient()
        {
            if (_Listener != null)
            {
                _Listener.StopListener(); ;
                _Listener = null;
            }
        }

        public void Dispose() =>
            StopClient();
        
        ~NetClient() =>
            StopClient();
    }

    public abstract class SocketListener
    {
        private const int BUFFER_SIZE = 1024;

        public abstract int ReceiveTimeout { get; }

        private Socket _Socket = null;
        private Thread _ListenerThread = null;

        private bool _StopClient = false;
        private bool _Delete = false;

        private DateTime _LastResponse = DateTime.UtcNow;

        public Guid Id { get; } = Guid.NewGuid();

        public bool IsConnected =>
            _Socket?.Connected ?? false;

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
            Byte[] byteBuffer = new Byte[BUFFER_SIZE];

            _LastResponse = DateTime.UtcNow;

            using (Timer t = new Timer(t =>
            {
                // stop if timout reached
                if ((ReceiveTimeout != 0) && (DateTime.UtcNow.Subtract(_LastResponse).TotalSeconds > ReceiveTimeout))
                    StopListener();
            }, null, ReceiveTimeout, ReceiveTimeout))
            {
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
            }
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

        public void SendBuffer(byte[] byteButter) =>
            _Socket.Send(byteButter);

        public abstract void ParseReceiveBuffer(Byte[] byteBuffer, int size);

        public SocketListener(Socket Socket) =>
            _Socket = Socket;

        ~SocketListener() =>
            StopListener();
    }

    public class Message : ObservableObject
    {
        private bool _Dirty = false;
        private Guid _Id = Guid.NewGuid();

        #region Properties
        public Guid Id { get => _Id; set => SetProperty(ref _Id, value); }
        #endregion

        public void Read(BinaryReader r)
        {

        }

        public void Write(BinaryWriter w)
        {

        }

        public Message() =>
            this.PropertyChanged += (object? sender, PropertyChangedEventArgs e) =>
                _Dirty = true;
    }

    public abstract class MessageCryptoProvider
    {

    }

    //public abstract class MessageBlock<T> where T : Message
    //{
    //    private MemoryStream _Stream;

    //    private bool _Dirty = false;

    //    public virtual void Read()
    //    {
    //        try
    //        {       
    //            var CryptoStream = new CryptoStream(_Stream, FSBase.CreateDecryptor(), CryptoStreamMode.Read);
    //            BinaryReader Reader = new BinaryReader(CryptoStream);

    //            ReadUserData(Reader);
    //            _Dirty = false;
    //        }
    //        catch (FSException)
    //        {
    //            // pass through
    //            throw;
    //        }
    //        catch (IOException IOe)
    //        {
    //            throw new FSException(IOe.Message);
    //        }
    //        catch (Exception e)
    //        {
    //            throw new FSException(e.Message);
    //        }
    //    }

    //    public abstract void ReadUserData(BinaryReader Reader);

    //    public virtual void Write()
    //    {
    //        if (Dirty)
    //        {
    //            try
    //            {
    //                FSBase.Handle.Position = (FSBase.Header.ClusterSize * Cluster) + FSBase.Header.FirstClusterOffset;

    //                var CryptoStream = new CryptoStream(FSBase.Handle, FSBase.CreateEncryptor(), CryptoStreamMode.Write);
    //                BinaryWriter Writer = new BinaryWriter(CryptoStream);

    //                WriteUserData(Writer);

    //                // Leeren des Schreib-Puffers erzwingen
    //                Writer.Flush();

    //                // CryptoStream gefüllt, Puffer leeren
    //                CryptoStream.Flush();

    //                // und abschliessen
    //                CryptoStream.FlushFinalBlock();

    //                Dirty = false;
    //            }
    //            catch (FSException)
    //            {
    //                // pass through
    //                throw;
    //            }
    //            catch (IOException IOe)
    //            {
    //                throw new FSException(IOe.Message);
    //            }
    //            catch (Exception e)
    //            {
    //                throw new FSException(e.Message);
    //            }
    //        }
    //    }
    //}
}