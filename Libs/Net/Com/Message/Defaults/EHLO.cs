using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Security.Cryptography.Xml;
using System.Text;

namespace Nox.Net.Com.Message.Defaults
{
    public class EhloEventArgs : EventArgs
    { 
        public Guid Id { get; }

        public string Message { get; }

        public EhloEventArgs(Guid Id, string Message)
        {
            this.Id = Id;
            this.Message = Message;
        }
    }

    public class EhloData
        : DataBlock
    {
        public const int MAX_LENGTH = 64;

        private Guid _Id = Guid.NewGuid();
        private string _Message = "";

        private ASCIIEncoding ASC = new ASCIIEncoding();

        #region Properties
        public Guid Id { get => _Id; set => SetProperty(ref _Id, value); }
        public string Message { get => _Message; set => SetProperty(ref _Message, value.LimitLength(MAX_LENGTH)); }
        #endregion

        public override void Read(byte[] data)
        {
            _Id = Helpers.ExtractGuid(data, 0);

            int i = 16;           
            _Message = ASC.GetString(data, i, data.Length - i).LimitLength(MAX_LENGTH).Trim();
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_Id.ToByteArray());
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
