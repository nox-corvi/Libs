using Microsoft.Extensions.Logging;
using Nox.Grid.Net.Message;
using Nox.Net.Com.Message;
using Nox.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Grid.Net;

public enum CommonMessageTypeEnum
{
    CMND = 0xFA01,  // send command to target
    UPTM = 0xFA02,  // get current uptime
}

public interface ICommonMessages
{
    public event EventHandler<CmndEventArgs> CmndMessage;
    public event EventHandler<EventArgs> Terminate;

    void OnCmndMessage(object sender, CmndEventArgs e);
    void OnTerminate(object sender, EventArgs e);
}

public class ClientCommon
    : Common, ICommonMessages
{
    public CommonNetSecureClient _CommonNetSecureClient;

    public void Cmnd(Guid SequenceId, CommandEnum command)
    {
        var cmd = new MessageCmnd(Common.Signature1);
        cmd.dataBlock.SequenceId = SequenceId;
        cmd.dataBlock.Command = command;

        _CommonNetSecureClient.CRaw(cmd.Write());
    }

    public void Feat(object sender)
    {

    }

    public ClientCommon(CommonNetSecureClient commonNetSecureClient)
        : base()
        => _CommonNetSecureClient = commonNetSecureClient;
}

public class ServerCommon<T>
    : Common, ICommonMessages
    where T : ClientData, new()
{
    private CommonNetSecureServer<T> _CommonNetSecureServer;

    public void Cmnd(object sender, Guid SequenceId, CommandEnum command)
    {
        var cmd = new MessageCmnd(Common.Signature1);
        cmd.dataBlock.SequenceId = SequenceId;
        cmd.dataBlock.Command = command;

        _CommonNetSecureServer.CRaw(sender, cmd.Write());
    }

    public ServerCommon(CommonNetSecureServer<T> commonNetSecureServer)
        : base()
    {
        _CommonNetSecureServer = commonNetSecureServer;
    }
}

public class Common
    : ICommonMessages
{
    public static readonly uint Signature1 = 0xDFA1;

    public static readonly int KeyLength = 64;
    public static readonly int Iterations = 10000;

    public static readonly byte[] __KEY = { 0x78, 0x6A, 0x42, 0x4B, 0x3D, 0x5C, 0x2F, 0x02,
                                                0x58, 0xD3, 0x22, 0x87, 0xAC, 0xD2, 0xC3, 0x10,
                                                0x13, 0x78, 0xB5, 0x6D, 0x27, 0x9C, 0x3B, 0x47,
                                                0x49, 0x01, 0x00, 0x2F, 0x49, 0x8E, 0x01, 0x15 };
    public static readonly byte[] __IV = { 0x3B, 0x89, 0x77, 0x29, 0x35, 0x45, 0xB3, 0xD8,
                                                0xD2, 0xE4, 0xF6, 0x02, 0x5D, 0x6B, 0x7A, 0x8D,
                                                0x6B, 0x3A, 0x29, 0x58, 0x77, 0x6E, 0x5F, 0xC4,
                                                0x3C, 0x36, 0x72, 0x33, 0x53, 0xAF, 0xFE, 0xA5 };

    #region Events
    public event EventHandler<CmndEventArgs> CmndMessage;
    //public event EventHandler<UptmEventArgs> UptmMessage;
    public event EventHandler<EventArgs> Terminate;
    #endregion

    #region OnRaiseEvent Methods
    
    public void OnCmndMessage(object sender, CmndEventArgs e)
        => CmndMessage?.Invoke(sender, e);
    //public void OnUptmMessage(object sender, UptmEventArgs e)
    //    => UptmMessage?.Invoke(sender, e);

    public void OnTerminate(object sender, EventArgs e)
        => Terminate?.Invoke(sender, e);
    #endregion

    public void CreateNewCertificates(string fn, ILogger logger, out byte[] PrivateKey, out byte[] PublicKey)
    {
        logger?.LogInformation("create new certificate");

        using var rsa = new tinyRSA();
        using var sha = SHA256.Create();

        PrivateKey = rsa.ExportPrivateKey();
        PublicKey = rsa.ExportPublicKey();

        using var laverna1 = new Laverna(__KEY, __IV);
        File.WriteAllBytes($"{fn}.key", laverna1.Encode(PrivateKey));
        File.WriteAllBytes($"{fn}.key.hash", sha.ComputeHash(PrivateKey));

        using var laverna2 = new Laverna(__KEY, __IV);
        File.WriteAllBytes($"{fn}.pub", laverna2.Encode(PublicKey));
        File.WriteAllBytes($"{fn}.pub.hash", sha.ComputeHash(PublicKey));
    }

    public byte[] ReadAndValidateCertificate(string fn1, ILogger logger)
    {
        logger?.LogInformation("read and validate certificate for {fn}", fn1);

        byte[] result;
        if (File.Exists($"{fn1}"))
        {
            // decode
            try
            {
                using (var laverna = new Laverna(__KEY, __IV))
                    result = laverna.Decode(File.ReadAllBytes($"{fn1}"));

                if (File.Exists($"{fn1}.hash"))
                {
                    using (var sha = SHA256.Create())
                    {
                        var computed_hash = sha.ComputeHash(result);
                        var file_hash = File.ReadAllBytes($"{fn1}.hash");

                        // compare with hash of unencrypted data 
                        if (file_hash.SequenceEqual(computed_hash))
                            return result;
                    }
                }

            }
            catch (Exception ex)
            {
                logger?.LogCritical(ex.ToString());
                return null;
            }
        }

        logger?.LogWarning("certificate validation failed for {fn}", fn1);
        return null;
    }

    public string[] GetFeatureSet()
        => new List<string>()
        {
            $"MachineName: {Environment.MachineName}",
            $"UserName: {Environment.UserName}",
            $"OSVersion: {Environment.OSVersion}",
            $"ProcessorCount: {Environment.ProcessorCount}",
            $"Version: {Environment.Version}",
            $"ProcessId: {Environment.ProcessId}",
            $"ProcessPath: {Environment.ProcessPath}",
        }.ToArray();

    public virtual bool ParseMessage(object sender, byte[] Message, Guid SequenceId)
    {
        return BitConverter.ToUInt32(Message, sizeof(uint)) switch // Signature2
        {
            (uint)CommonMessageTypeEnum.CMND => HandleCommandMessage(sender, Message, SequenceId),
            //(uint)CommonMessageTypeEnum.UPTM => HandleUptimeMessage(sender, Message, SequenceId),

            _ => false,// unknown message type
        };
    }

    private bool HandleCommandMessage(object sender, byte[] Message, Guid SequenceId)
    {
        var cmd = new MessageCmnd(Signature1);
        cmd.Read(Message);

        if (cmd.dataBlock.SequenceId == SequenceId)
        {
            var v = new CmndEventArgs(cmd.dataBlock.SequenceId, cmd.dataBlock.Command);
            OnCmndMessage(sender, v);

            return true;
        }
        else
        {
            OnTerminate(sender, new EventArgs());
            return false;
        }
    }

    //private bool HandleUptimeMessage(object sender, byte[] Message, Guid SequenceId)
    //{
    //    var uptime = new MessageUptm(Signature1);
    //    uptime.Read(Message);

    //    if (uptime.dataBlock.SequenceId == SequenceId)
    //    {
    //        var v = new UptmEventArgs(uptime.dataBlock.SequenceId, uptime.dataBlock.Uptime);
    //        OnUptmMessage(sender, v);

    //        return true;
    //    }
    //    else
    //    {
    //        OnTerminate(sender, new EventArgs());

    //        return false;
    //    }
    //}

    public Common()
    {

    }
}
