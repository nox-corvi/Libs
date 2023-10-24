using Nox.Net.Com.Message;
using Nox.Net.Com;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nox;
using System.Security.Cryptography.X509Certificates;

namespace BitsCommon.Net.Message
{
	public class DataEventArgs : EventArgs
	{
		public Guid SequenceId { get; }

		public byte[] Data0 { get; }
        public byte[] Data1 { get; }
        public byte[] Data2 { get; }
        public byte[] Data3 { get; }

        public DataEventArgs(Guid SequenceId)
			: base()
		    => this.SequenceId = SequenceId;

        public DataEventArgs(Guid SequenceId, byte[] Data0)
            : this(SequenceId)
            => this.Data0 = Data0;

        public DataEventArgs(Guid SequenceId, byte[] Data0, byte[] Data1)
            : this(SequenceId, Data0)
            => this.Data1 = Data1;

        public DataEventArgs(Guid SequenceId, byte[] Data0, byte[] Data1, byte[] Data2)
            : this(SequenceId, Data0, Data1)
            => this.Data2 = Data2;

        public DataEventArgs(Guid SequenceId, byte[] Data0, byte[] Data1, byte[] Data2, byte[] Data3)
            : this(SequenceId, Data0, Data1, Data2)
            => this.Data3 = Data3;
    }

	public class MessageDataData
		: DataBlock
	{
        private byte[] _Data0;
        private byte[] _Data1;
        private byte[] _Data2;
        private byte[] _Data3;

        #region Properties
        public byte[] Data0 { get => _Data0; set => SetProperty(ref _Data0, value); }
        public byte[] Data1 { get => _Data1; set => SetProperty(ref _Data1, value); }
        public byte[] Data2 { get => _Data2; set => SetProperty(ref _Data2, value); }
        public byte[] Data3 { get => _Data3; set => SetProperty(ref _Data3, value); }

        #endregion

        public override void Read(byte[] data)
		{
			int i = 0, read;
            _Data0 = Helpers.ExtractArrayWithLength(data, i, out read);

            i += read;
            _Data1 = Helpers.ExtractArrayWithLength(data, i, out read);

            i += read;
            _Data2 = Helpers.ExtractArrayWithLength(data, i, out read);

            i += read;
            _Data3 = Helpers.ExtractArrayWithLength(data, i, out read);
        }

		public override byte[] Write()
		{
			var Result = new List<byte>();

            Result.AddRange(BitConverter.GetBytes(_Data0?.Length ?? 0));
            if (_Data0 != null)
                Result.AddRange(_Data0);
            
            Result.AddRange(BitConverter.GetBytes(_Data1?.Length ?? 0));
            if (_Data1 != null)
                Result.AddRange(_Data1);
            
            Result.AddRange(BitConverter.GetBytes(_Data2?.Length ?? 0));
            if (_Data2 != null)
                Result.AddRange(_Data2);

            Result.AddRange(BitConverter.GetBytes(_Data3?.Length ?? 0));
            if (_Data3 != null)
                Result.AddRange(_Data3);

            return Result.ToArray();
		}

		public MessageDataData(uint Signature)
			: base(Signature) { }
	}

	public class MessageData
	   : RawMessage<MessageDataData>
	{
		public MessageData(uint Signature)
			: base(Signature, (uint)MessageTypeEnum.DATA) { }
	}
}
