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

		public byte[] data0 { get; }
        public byte[] data1 { get; }
        public byte[] data2 { get; }
        public byte[] data3 { get; }

        public DataEventArgs(Guid SequenceId)
			: base()
		    => this.SequenceId = SequenceId;

        public DataEventArgs(Guid SequenceId, byte[] data0)
            : this(SequenceId)
            => this.data0 = data0;

        public DataEventArgs(Guid SequenceId, byte[] data0, byte[] data1)
            : this(SequenceId,  data0)
            => this.data1 = data1;

        public DataEventArgs(Guid SequenceId, byte[] data0, byte[] data1, byte[] data2)
            : this(SequenceId,  data0, data1)
            => this.data2 = data2;

        public DataEventArgs(Guid SequenceId, byte[] data0, byte[] data1, byte[] data2, byte[] data3)
            : this(SequenceId,  data0, data1, data3)
            => this.data3 = data3;
    }

	public class MessageDataData
		: DataBlock
	{
        private byte[] _data0;
        private byte[] _data1;
        private byte[] _data2;
        private byte[] _data3;

        #region Properties
        public byte[] data0 { get => _data0; set => SetProperty(ref _data0, value); }
        public byte[] data1 { get => _data1; set => SetProperty(ref _data1, value); }
        public byte[] data2 { get => _data2; set => SetProperty(ref _data2, value); }
        public byte[] data3 { get => _data3; set => SetProperty(ref _data3, value); }

        #endregion

        public override void Read(byte[] data)
		{
			int i = 0, read;
            _data0 = Helpers.ExtractArrayWithLength(data, i, out read);

            i += read;
            _data1 = Helpers.ExtractArrayWithLength(data, i, out read);

            i += read;
            _data2 = Helpers.ExtractArrayWithLength(data, i, out read);

            i += read;
            _data3 = Helpers.ExtractArrayWithLength(data, i, out read);
        }

		public override byte[] Write()
		{
			var Result = new List<byte>();

            Result.AddRange(BitConverter.GetBytes(_data0?.Length ?? 0));
            if (_data0 != null)
                Result.AddRange(_data0);
            
            Result.AddRange(BitConverter.GetBytes(_data1?.Length ?? 0));
            if (_data1 != null)
                Result.AddRange(_data1);
            
            Result.AddRange(BitConverter.GetBytes(_data2?.Length ?? 0));
            if (_data2 != null)
                Result.AddRange(_data2);

            Result.AddRange(BitConverter.GetBytes(_data3?.Length ?? 0));
            if (_data3 != null)
                Result.AddRange(_data3);

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
