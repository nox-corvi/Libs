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
    public class IaCHandler
        : CIBase
    {
        const int MAX_BLOCK_SIZE = 1024;

       // get the crc of a stream
        public uint CalculateCRC(Stream stream, bool Rewind)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, stream, Rewind);
            _logger?.LogMessage(Message(ResEnum._calc_crc), Log4.Log4LevelEnum.Debug);

            try
            {
                int read = 0;
                byte[] data = new byte[MAX_BLOCK_SIZE];

                var t = new tinyCRC();

                if (Rewind)
                    stream.Position = 0;

                while ((read = stream.Read(data, 0, MAX_BLOCK_SIZE)) > 0)
                    t.Push(data, 0, read);

                _logger?.LogMessage(Message(ResEnum._calc_crc_done,t.CRC32.ToString()), Log4.Log4LevelEnum.Debug);
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
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Filename, crc32value);
            _logger?.LogMessage(Message(ResEnum._validate_file_crc, crc32value.ToString()), Log4.Log4LevelEnum.Debug);

            try
            {
                using (var file = File.OpenRead(Filename))
                {
                    uint crc32 = CalculateCRC((Stream)file, false);

                    _logger?.LogMessage(Message(ResEnum._calc_crc_done, crc32.ToString()), Log4.Log4LevelEnum.Debug);
                    
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

        public bool ValidateFileCRC(string Filename, string crc32string)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Filename, crc32string);
            _logger?.LogMessage(Message(ResEnum._validate_file_crc, NullStr(crc32string)), Log4.Log4LevelEnum.Debug);

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
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, inputStream, outputStream, Key, Salt);
            _logger?.LogMessage(Message(ResEnum._encode), Log4.Log4LevelEnum.Debug);

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
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, inputStream, outputStream, Key, Salt);
            _logger?.LogMessage(Message(ResEnum._decode), Log4.Log4LevelEnum.Debug);

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
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Key, Salt, Value);
            _logger?.LogMessage(Message(ResEnum._encode_laverna, NullStr(Value)), Log4.Log4LevelEnum.Debug);
 
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
            _logger?.LogMethod(Log4.Log4LevelEnum.Debug, Key, Salt, Value);
            _logger?.LogMessage(Message(ResEnum._decode_laverna, NullStr(Value)), Log4.Log4LevelEnum.Debug);

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

        public IaCHandler(CI CI)
            : base(CI) { }

        public IaCHandler(CI CI, Log4 logger)
            : base(CI, logger) { }
    }
}
