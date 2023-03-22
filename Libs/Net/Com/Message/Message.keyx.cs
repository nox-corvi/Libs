using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class KeyxEventArgs : EventArgs
    {
        /// <summary>
        /// Random Guid to identify the sequence
        /// </summary>
        public Guid SequenceId { get; }
        public KeyxEventArgs()
            : base()
        {
            //this.SequenceId = SequenceId;
        }
    }
    public class MessageKeyxData
        : DataBlock
    {
        public Guid _SequenceId = Guid.Empty;

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

        public MessageKeyxData(uint Signature2)
            : base(Signature2) { }
    }

    public class MessageKeyx
       : RawMessage<MessageKeyxData>
    {
        public MessageKeyx(uint Signature1)
            : base(Signature1, (uint)SecureMessageTypeEnum.KEYX) { }
    }
}
