using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message.Defaults
{
    public class RplyEventArgs : EventArgs
    {
        public Guid EhloId { get; }
        public string Message { get; }

        public RplyEventArgs(Guid EhloId, string Message)
        {
            this.EhloId = EhloId;
            this.Message = Message;
        }
    }  

    public class RplyData
        : DataBlock
    {
        public const int MAX_LENGTH = 64;

        private Guid _EhloId = Guid.Empty;
        private string _Message = "";

        private ASCIIEncoding ASC = new ASCIIEncoding();

        #region Properties
        public Guid EhloId { get => _EhloId; set => SetProperty(ref _EhloId, value); }
        public string Message { get => _Message; set => SetProperty(ref _Message, value.LimitLength(MAX_LENGTH)); }
        #endregion

        public override void Read(byte[] data)
        {
            int i = 0;
            _EhloId = Helpers.ExtractGuid(data, i);

            i += 16;
            _Message = ASC.GetString(data, 16, data.Length - 16).LimitLength(MAX_LENGTH).Trim();
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_EhloId.ToByteArray());
            Result.AddRange(ASC.GetBytes(_Message.LimitLength(MAX_LENGTH)));

            return Result.ToArray();
        }

        public RplyData(uint Signature2)
            : base(Signature2) { }
    }

    public class RPLY
       : RawMessage<RplyData>
    {
        public RPLY(uint Signature1)
            : base(Signature1, (uint)DefaultMessageTypeEnum.RPLY) { }
    }
}
