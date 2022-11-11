using Microsoft.EntityFrameworkCore.Infrastructure;
using Nox.Security;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.Security.Cryptography.Xml;
using System.Text;

namespace Nox.Net.Com.Message.Defaults
{
    public class EhloEventArgs : EventArgs
    { 
        public Guid Id { get; }

        public tinyKey Key { get; }

        public string Message { get; }

        public EhloEventArgs(Guid Id, tinyKey Key, string Message)
        {
            this.Id = Id;
            this.Key = Key;
            this.Message = Message;
        }
    }

    public class EhloData
        : DataBlock
    {
        public const int MAX_LENGTH = 64;

        private Guid _Id = Guid.NewGuid();

        public tinyKey _publicKey;

        private string _Message = "";

        private ASCIIEncoding ASC = new ASCIIEncoding();

        #region Properties
        public Guid Id { get => _Id; set => SetProperty(ref _Id, value); }

        public tinyKey Key { get => _publicKey; set => SetProperty(ref _publicKey, value); }

        public string Message { get => _Message; set => SetProperty(ref _Message, value.LimitLength(MAX_LENGTH)); }
        #endregion

        public override void Read(byte[] data)
        {
            _Id = Helpers.ExtractGuid(data, 0);

            int i = 16, j = 0;

            j = BitConverter.ToInt32(data, i); i += sizeof(int);

            byte[] k = new byte[j];
            Array.Copy(data, i, k, 0, j);
            i += j;

            j = BitConverter.ToInt32(data, i); i += sizeof(int);

            byte[] h = new byte[j];
            Array.Copy(data, i, h, 0, j);
            i += j;

            Key = new tinyKey(k, h);

            _Message = ASC.GetString(data, i, data.Length - i).LimitLength(MAX_LENGTH).Trim();
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_Id.ToByteArray());

            Result.AddRange(BitConverter.GetBytes(Key.Key.Length));
            Result.AddRange(Key.Key);
            Result.AddRange(BitConverter.GetBytes(Key.Hash.Length));
            Result.AddRange(Key.Hash);

            Result.AddRange(ASC.GetBytes(_Message.LimitLength(MAX_LENGTH)));

            return Result.ToArray();    
        }

        public EhloData(uint Signature2)
            : base(Signature2) { }
    }

    public class EHLO
       : RawMessage<EhloData>
    {
        public EHLO(uint Signature1)
            : base(Signature1, (uint)DefaultMessageTypeEnum.EHLO) { }
    }
}
