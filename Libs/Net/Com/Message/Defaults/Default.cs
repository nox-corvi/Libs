namespace Nox.Net.Com.Message.Defaults
{
    public enum DefaultMessageTypeEnum
    {
        PING = 0xFCA0,  // Ping
        ECHO = 0xFCA1,  // Reply to Ping
        EHLO = 0xFCA2,  // Hello, Fingerprint and Message
        RPLY = 0xFCA3,  // Reply to EHLO

        RESP = 0xFCAF,  // generic response 
        
        KEXC = 0xFCB1,
        KEYV = 0xFCB2,
    }
}
