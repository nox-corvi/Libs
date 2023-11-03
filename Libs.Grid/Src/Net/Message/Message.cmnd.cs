using Nox.Net.Com.Message;
using Nox.Net.Com;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nox;
using System.Security.Cryptography.X509Certificates;

namespace Nox.Grid.Net.Message;

public enum CommandEnum
{
    Uptime = 0xFC01,
}

public class CmndEventArgs : EventArgs
{
    public Guid SequenceId { get; }

    public CommandEnum Command { get; set; }

    public byte[] Arg0 { get; set; }
    public byte[] Arg1 { get; set; }
    public byte[] Arg2 { get; set; }
    public byte[] Arg3 { get; set; }

    public CmndEventArgs(Guid SequenceId, CommandEnum Command)
            : base()
    {
        this.SequenceId = SequenceId;
        this.Command = Command;
    }

    public CmndEventArgs(Guid SequenceId, CommandEnum Command, byte[] Arg0)
        : this(SequenceId, Command)
        => this.Arg0 = Arg0;

    public CmndEventArgs(Guid SequenceId, CommandEnum Command, byte[] Arg0, byte[] Arg1)
        : this(SequenceId, Command, Arg0)
        => this.Arg1 = Arg1;

    public CmndEventArgs(Guid SequenceId, CommandEnum Command, byte[] Arg0, byte[] Arg1, byte[] Arg2)
        : this(SequenceId, Command, Arg0, Arg1)
        => this.Arg2 = Arg2;

    public CmndEventArgs(Guid SequenceId, CommandEnum Command, byte[] Arg0, byte[] Arg1, byte[] Arg2, byte[] Arg3)
        : this(SequenceId, Command, Arg0, Arg1, Arg2)
        => this.Arg3 = Arg3;
}

public class MessageCmndData
    : DataBlock
{
    private Guid _SequenceId;
    private CommandEnum _Command;
    private byte[] _Arg0;
    private byte[] _Arg1;
    private byte[] _Arg2;
    private byte[] _Arg3;

    #region Properties
    public Guid SequenceId { get => _SequenceId; set => SetProperty(ref _SequenceId, value); }
    public CommandEnum Command { get => _Command; set => SetProperty(ref _Command, value); }

    public byte[] Arg0 { get => _Arg0; set => SetProperty(ref _Arg0, value); }
    public byte[] Arg1 { get => _Arg1; set => SetProperty(ref _Arg1, value); }
    public byte[] Arg2 { get => _Arg2; set => SetProperty(ref _Arg2, value); }
    public byte[] Arg3 { get => _Arg3; set => SetProperty(ref _Arg3, value); }
    #endregion

    public override void Read(byte[] data)
    {
        _SequenceId = Helpers.ExtractGuid(data, 0);

        int i = 16, read;
        _Command = (CommandEnum)BitConverter.ToUInt32(data, i);

        i += sizeof(uint);
        _Arg0 = Helpers.ExtractArrayWithLength(data, i, out read);

        i += read;
        _Arg1 = Helpers.ExtractArrayWithLength(data, i, out read);

        i += read;
        _Arg2 = Helpers.ExtractArrayWithLength(data, i, out read);

        i += read;
        _Arg3 = Helpers.ExtractArrayWithLength(data, i, out read);
    }

    public override byte[] Write()
    {
        var Result = new List<byte>();
        Result.AddRange(_SequenceId.ToByteArray());

        Result.AddRange(BitConverter.GetBytes((uint)_Command));

        Result.AddRange(BitConverter.GetBytes(_Arg0?.Length ?? 0));
        if (_Arg0 != null)
            Result.AddRange(_Arg0);

        Result.AddRange(BitConverter.GetBytes(_Arg1?.Length ?? 0));
        if (_Arg1 != null)
            Result.AddRange(_Arg1);

        Result.AddRange(BitConverter.GetBytes(_Arg2?.Length ?? 0));
        if (_Arg2 != null)
            Result.AddRange(_Arg2);

        Result.AddRange(BitConverter.GetBytes(_Arg3?.Length ?? 0));
        if (_Arg3 != null)
            Result.AddRange(_Arg3);

        return Result.ToArray();
    }

    public MessageCmndData(uint Signature)
        : base(Signature) { }
}

public class MessageCmnd
   : RawMessage<MessageCmndData>
{
    public MessageCmnd(uint Signature)
        : base(Signature, (uint)CommonMessageTypeEnum.CMND) { }
}
