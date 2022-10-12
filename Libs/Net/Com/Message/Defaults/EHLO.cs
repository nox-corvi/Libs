using System;
using System.Collections.Generic;
using System.Security.Cryptography.Xml;
using System.Text;

namespace Nox.Net.Com.Message.Defaults
{
    public class EHLOReplyEventHandler : EventArgs
    { 
        public Guid EhloId { get; }

        public DateTime EhloTime { get; }
        public DateTime RplyTime { get; }

        public string Message { get; }

        public EHLOReplyEventHandler(Guid EhloId, DateTime EhloTime, DateTime RplyTime, string Message)
        {
            this.EhloId = EhloId;
            this.EhloTime = EhloTime;
            this.RplyTime = RplyTime;
            this.Message = Message;
        }
    }

    public class ehlo_data
        : DataBlock
    {
        public const int MAX_LENGTH = 64;

        private DateTime _Timestamp = DateTime.UtcNow;
        private string _Message = "";


        private ASCIIEncoding ASC = new ASCIIEncoding();

        #region Properties
        public string Message { get => _Message; set => SetProperty(ref _Message, value.LimitLength(MAX_LENGTH)); }
        public DateTime Timestamp { get => _Timestamp; set => SetProperty(ref _Timestamp, value); }
        #endregion

        public override void Read(byte[] data)
        {
            int i = 0;

            byte[] buffer1 = new byte[16];
            Array.Copy(data, i, buffer1, i, 16);
            _Id = new Guid(buffer1);

            i += 16;
            _Timestamp = DateTime.FromBinary(BitConverter.ToInt64(data, i));

            i += sizeof(long);
            _Message = ASC.GetString(data, i, data.Length - i).LimitLength(MAX_LENGTH).Trim();
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_Id.ToByteArray());
            Result.AddRange(BitConverter.GetBytes(_Timestamp.ToBinary()));
            Result.AddRange(ASC.GetBytes(_Message.LimitLength(MAX_LENGTH)));

            return Result.ToArray();    
        }

        public ehlo_data(uint Signature2)
            : base(Signature2) { }
    }

    public class EHLO
       : RawMessage<ehlo_data>
    {
        public EHLO(uint Signature1)
            : base(Signature1, (uint)DefaultMessageTypeEnum.EHLO) { }
    }
}
