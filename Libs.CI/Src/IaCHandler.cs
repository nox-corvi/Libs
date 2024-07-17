using Microsoft.Extensions.Logging;
using Nox;
using Nox.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.CI
{
    public class IaCHandler(CI CI, ILogger Logger)
       : CIBase(CI, Logger)
    {
        const int MAX_BLOCK_SIZE = 1024;

        // get the crc of a stream
        public uint CalculateCRC(Stream stream, bool Rewind)
        {
            Logger?.LogDebug(Message(ResEnum._calc_crc));

            try
            {
                int read = 0;
                byte[] data = new byte[MAX_BLOCK_SIZE];

                var t = new tinyCRC();

                if (Rewind)
                    stream.Position = 0;

                while ((read = stream.Read(data, 0, MAX_BLOCK_SIZE)) > 0)
                    t.Push(data, 0, read);

                Logger?.LogDebug(Message(ResEnum._calc_crc_done, t.CRC32.ToString()));
                
                return t.CRC32;
            }
            catch (Exception e)
            {
                string ErrMsg = Message(MsgTypeEnum._error, ResEnum._calc_crc_fail);

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public bool ValidateFileCRC(string Filename, uint crc32value)
        {
            Logger?.LogDebug(Message(ResEnum._validate_file_crc, crc32value.ToString()));

            try
            {
                using (var file = File.OpenRead(Filename))
                {
                    uint crc32 = CalculateCRC((Stream)file, false);

                    Logger?.LogDebug(Message(ResEnum._calc_crc_done, crc32.ToString()));

                    return (crc32 == crc32value);
                }
            }
            catch (Exception e)
            {
                string ErrMsg = Message(MsgTypeEnum._error, ResEnum._calc_crc_fail);

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        private bool ValidateFileCRC(string Filename, string crc32string)
        {
            Logger?.LogDebug(Message(ResEnum._validate_file_crc, NullStr(crc32string)));

            try
            {
                // parse 
                if (!uint.TryParse(crc32string, out uint crc32value))
                {
                    string ErrMsg = Message(ResEnum._invalid_argument_A, crc32string);

                    _CI.CancelWithMessage(ErrMsg);
                    throw new ArgumentException(ErrMsg);
                }

                return ValidateFileCRC(Filename, crc32value); ;
            }
            catch (Exception e)
            {
                string ErrMsg = Message(MsgTypeEnum._error, ResEnum._calc_crc_fail);

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public bool Encode(Stream inputStream, Stream outputStream, string Key, string Salt)
        {
            Logger?.LogDebug(Message(ResEnum._encode));

            try
            {
                using (var l = new Laverna(Key, Salt))
                    return (l.Encode(inputStream, outputStream) != 0);
            }
            catch (Exception e)
            {
                string ErrMsg = Message(MsgTypeEnum._error, ResEnum._encode_fail);

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public bool Decode(Stream inputStream, Stream outputStream, string Key, string Salt)
        {
            Logger?.LogDebug(Message(ResEnum._decode));

            try
            {
                using (var l = new Laverna(Key, Salt))
                    return (l.Decode(inputStream, outputStream) != 0);
            }
            catch (Exception e)
            {
                string ErrMsg = Message(MsgTypeEnum._error, ResEnum._decode_fail);

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public string EncodeString(string Key, string Salt, string Value)
        {
            Logger?.LogDebug(Message(ResEnum._encode_laverna, NullStr(Value)));

            try
            {
                using (var l = new Laverna(Key, Salt))
                    return l.EncryptString(Value);
            }
            catch (Exception e)
            {
                string ErrMsg = Message(MsgTypeEnum._error, ResEnum._encode_fail);

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public string DecodeString(string Key, string Salt, string Value)
        {
            Logger?.LogDebug(Message(ResEnum._decode_laverna, [Value]));

            try
            {
                using (var l = new Laverna(Key, Salt))
                    return l.DecryptString(Value);
            }
            catch (Exception e)
            {
                string ErrMsg = Message(MsgTypeEnum._error, ResEnum._decode_fail);

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        // DI-Constructor
        public IaCHandler(CI CI, ILogger<IaCHandler> Logger)
            : this(CI, (ILogger)Logger) { }
    }
}
