using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nox;

namespace Nox.Net.Com.Message.Defaults
{
    public class ehlo_data
        : DataBlock
    {
        public const int MAX_LENGTH = 64;

        private string _Message = "";
        private ASCIIEncoding ASC = new();

        #region Properties
        public string Message { get => _Message; set => _Message = value.LimitLength(MAX_LENGTH); }
        #endregion

        public override void Read(byte[] data) =>
            _Message = ASC.GetString(data).LimitLength(MAX_LENGTH).Trim(); 

        public override byte[] Write() =>
            ASC.GetBytes(_Message.LimitLength(MAX_LENGTH));

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
