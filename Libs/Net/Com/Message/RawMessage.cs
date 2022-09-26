using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public abstract class RawMessage
    {
        private const uint EOM = 0xFEFE;

        protected uint _signature1;
        protected uint _signature2;

        protected uint _hash;
        protected byte[] _data = null;

        #region Properties
        public uint Signature1 => _signature1;
        
        public uint Signature2
        {
            get => _signature2;
            set => _signature2 = value;
        }

        public uint Hash => _hash;

        public byte[] Data { get  => _data; set => _data = value; } 

        #endregion

        public void Read(byte[] data)
        {
            using (var Stream = new MemoryStream(data))
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
                var raw = Reader.ReadBytes(length);

                // and validate
                if (CreateDataHash(raw) != hash)
                    throw new Exception("hash invalid");

                var Last = Reader.ReadUInt32();
                if (Last != EOM)
                    throw new Exception("0xfefe not found at end of message");

                _hash = hash;
                _data = raw;
            }
        }

        public byte[] Write()
        {
            using (var Stream = new MemoryStream())
            using (var Writer = new BinaryWriter(Stream))
            {
                var l = (ushort)_data.Length;
                var t = CreateDataHash(_data);

                Writer.Write(_signature1);
                Writer.Write(_signature2);

                Writer.Write(_hash = CreateDataHash(_data));
                Writer.Write((ushort)_data.Length);

                Writer.Write(_data);

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
    }

}
