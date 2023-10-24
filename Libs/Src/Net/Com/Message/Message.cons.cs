using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class ConSEventArgs : EventArgs
    {
        public Guid SequenceId { get; }
        
        public ConSEventArgs(Guid SequenceId)
            : base() =>
            this.SequenceId = SequenceId;
    }
    public class ConSData
         : DataBlock
    {
        private Guid _SequenceId = Guid.NewGuid();

        #region Properties
        public Guid SequenceId { get => _SequenceId; set => SetProperty(ref _SequenceId, value); }
        #endregion

        public override void Read(byte[] data)
        {
            _SequenceId = Helpers.ExtractGuid(data, 0);
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_SequenceId.ToByteArray());

            return Result.ToArray();
        }

        public ConSData(uint Signature2)
            : base(Signature2) { }
    }

    public class MessageConS
       : RawMessage<ConSData>
    {
        public MessageConS(uint Signature1)
            : base(Signature1, (uint)SecureMessageTypeEnum.CONS) { }
    }
}
