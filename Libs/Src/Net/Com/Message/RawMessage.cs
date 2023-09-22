using System;
using System.IO;

namespace Nox.Net.Com.Message
{
    /// <summary>
    /// structure of message, all data are encrypted
    /// 
    /// field       type    len     usage
    /// signature1  uint    4       key-signature of the message
    /// signature2  uint    4       message signature
    /// hash        uint    4       hashcode of the datablock
    /// len         uint    2       length of the datablock in bytes
    /// data        byte    n       raw data
    /// 
    /// Reserved Signature2 Namespaces: 0xFCA0 - 0xFCFF
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RawMessage<T>
        where T : IDataBlock
    {
        private const uint EOM = 0xFEFE;

        protected uint _signature1;
        protected uint _signature2;

        protected uint _hash;
        protected byte[] _user_data = null;

        private IDataBlock _dataBlock;

        #region Properties
        /// <summary>
        /// signature 1 associates the packet to the application
        /// </summary>
        public uint Signature1 => _signature1;

        /// <summary>
        /// signature 2 identifies the packet type
        /// </summary>
        public uint Signature2
        {
            get => _dataBlock?.Signature2 ?? throw new ArgumentNullException("dataBlock cannot be null to retrieve Signature2");
        }

        public byte[] UserData { get => _user_data; set => _user_data = value; }

        public T dataBlock => (T)_dataBlock;

        #endregion

        private IDataBlock CreateDataBlock(uint Signature2) =>
            (T)Activator.CreateInstance(typeof(T), Signature2);

        public virtual void Read(byte[] raw)
        {
            using (var Stream = new MemoryStream(raw))
            using (var Reader = new BinaryReader(Stream))
            {
                var s1 = Reader.ReadUInt32();
                if (s1 != Signature1)
                    throw new Exception("invalid signature");

                // get signature2
                _signature2 = Reader.ReadUInt32();

                // read hash and length
                var hash = Reader.ReadUInt32();
                var length = Reader.ReadUInt16();

                // data
                _user_data = Reader.ReadBytes(length);
                _dataBlock = CreateDataBlock(Signature2);

                // read byte data in datablock
                _dataBlock.Read(_user_data);

                // and validate
                if (CreateDataHash(_user_data) != hash)
                    throw new Exception("hash invalid");

                var Last = Reader.ReadUInt32();
                if (Last != EOM)
                    throw new Exception("0xfefe not found at end of message");

                _hash = hash;
            }
        }

        public virtual byte[] Write()
        {
            using (var Stream = new MemoryStream())
            using (var Writer = new BinaryWriter(Stream))
            {
                _user_data = _dataBlock.Write();

                var l = (ushort)_user_data.Length;
                var t = CreateDataHash(_user_data);

                Writer.Write(Signature1);
                Writer.Write(Signature2);

                Writer.Write(_hash = CreateDataHash(_user_data));
                Writer.Write((ushort)_user_data.Length);

                Writer.Write(_user_data);

                Writer.Write(EOM);

                // Leeren des Schreib-Puffers erzwingen
                Writer.Flush();
                Stream.Flush();

                return Stream.ToArray();
            }
        }

        #region Helper
        private uint CreateDataHash(byte[] data)
        {
            uint hash = 2166136261U;
            uint prime = 16777619;

            for (int i = 0; i < data.Length; i++)
                hash = (hash ^ data[i]) * prime;

            return hash;
        }
        #endregion

        public RawMessage(uint Signature1) =>
            this._signature1 = Signature1;

        public RawMessage(uint Signature1, uint Signature2)
            : this(Signature1) =>
            _dataBlock = CreateDataBlock(Signature2);
    }
}
