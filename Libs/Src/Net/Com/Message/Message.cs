using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public enum MessageTypeEnum
    {
        TERM = 0xFCA0,  // Done
        PING = 0xFCA1,  // Ping
        ECHO = 0xFCA2,  // Reply to Ping
        FEAT = 0xFCA3
    }
    public enum SecureMessageTypeEnum
    {
        EHLO = 0xFCB0,  // Hello, Fingerprint and Message
        RPLY = 0xFCB1,  // Reply to EHLO
        SIGX = 0xFCB2,  // Signature Exchange
        SIGV = 0xFCB3,  // Signature Validate in Reply to SIGX
        KEYX = 0xFCB4,
        KEYV = 0xFCB5,
        CONS = 0xFCB6,
        CRAW = 0xFCBD,  // Crypted RAW Message
        RESP = 0xFCBF,  // generic response 
    }

    public class MessagePlainData
        : DataBlock
    {
        public override void Read(byte[] data) { }

        public override byte[] Write() => new byte[] { };

        public MessagePlainData(uint Signature2)
            : base(Signature2) { }
    }
}
 