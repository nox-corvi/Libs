﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com
{
    public class LoopEventArgs<T>
        : CancelEventArgs
    {
        public ThreadSafeDataList<T> DataList { get; set; }

        public LoopEventArgs(ThreadSafeDataList<T> dataList)
        {
            DataList = dataList;
        }
    }


    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }

        public MessageEventArgs(string Message)
            : base() =>
            this.Message = Message;
    }
    public class MessageCancelEventArgs : CancelEventArgs
    {
        public string Message { get; set; }

        public MessageCancelEventArgs(string Message)
            : base()
            => this.Message = Message;
    }

    public class ObtainMessageEventArgs : EventArgs
    {
        public uint Identifier { get; set; }

        public string Message { get; set; }

        public ObtainMessageEventArgs()
            : base() =>
            this.Message = Message;
    }

    public class ObtainCancelMessageEventArgs : CancelEventArgs
    {
        public uint Identifier { get; set; }
        public string Message { get; set; }

        public ObtainCancelMessageEventArgs()
            : base() =>
            this.Message = Message;
    }

    public class PublicKeyEventArgs : CancelEventArgs
    {
        public byte[] publicKey { get; set; }

        public PublicKeyEventArgs()
            : base() { }
    }
}