using Microsoft.Extensions.Logging;
using Nox.Net.Com.Message;
using Nox.Net.Com;
using Nox.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Nox;

namespace Nox.Grid.Net;

public abstract class CommonNetSecureClient
    : IDisposable
{
    protected readonly ILogger _Logger = null!;

    private Common _C;

    private byte[] _privateKey = null!;
    private byte[] _publicKey = null!;

    private byte[] _Pass = null!;
    private byte[] _Salt = null!;

    private string _FingerPrint = "";

    private string _EhloMessage = "";
    private byte[] _foreignKey = null!;

    private byte[] _Key = null!;
    private byte[] _IV = null!;

    //private Guid _PingId;
    protected Guid _SequenceId;

    private NetSecureClient _Client = null!;
    private Laverna _Laverna = null!;

    #region Properties
    public Common C { get => _C; }

    public Guid SequenceId { get => _SequenceId; }
    public bool IsConnected { get => _Client?.IsConnected ?? false; }

    public string FingerPrint { get => _FingerPrint; }
    #endregion

    private void DoCertificates()
    {
        var fn = $"{AppDomain.CurrentDomain.FriendlyName}";

        // create new certicate and store to file ...
        _privateKey = C.ReadAndValidateCertificate($"{fn}.key", _Logger);
        _publicKey = C.ReadAndValidateCertificate($"{fn}.pub", _Logger);

        if (_privateKey == null || _publicKey == null)
            C.CreateNewCertificates(fn, _Logger, out _privateKey, out _publicKey);

        using (var sha = SHA256.Create())
            _FingerPrint = Helpers.EnHEX(sha.ComputeHash(_publicKey)).ToUpper();
    }

    public void DoCreateKeys()
    {
        _Pass = Encoding.ASCII.GetBytes(Helpers.RandomString(Common.KeyLength));
        _Salt = Encoding.ASCII.GetBytes(Helpers.RandomString(Common.KeyLength));
    }

    public abstract Common BindCommon();

    protected virtual void DoBindEvents()
    {
        _Client.PingMessage += (sender, e) =>
            _Logger.LogInformation($"got ping: {e.Id}@{e.Timestamp}");

        _Client.EchoMessage += (sender, e) =>
            _Logger.LogInformation($"got echo: {e.PingId}@{e.EchoTimestamp}");

        _Client.RplyMessage += (sender, e) =>
        {
            _Logger.LogInformation($"rply {e.SequenceId}");

            _SequenceId = e.SequenceId;
            _foreignKey = e.PublicKey;

            SigX();
        };

        _Client.ObtainRplyMessage += (sender, e) =>
            _Logger.LogInformation($"use rply message: {e.Message = $"{Helpers.RandomString(512)}"}");

        _Client.ObtainPublicKey += (sender, e) =>
            _Logger.LogInformation($"obtain public key: {e.publicKey = _publicKey}");

        _Client.SigvMessage += (sender, e) =>
        {
            _Logger.LogInformation($"signature validate {e.SequenceId}");

            using (var rsa = new tinyRSA())
            {
                rsa.ImportPrivateKey(_privateKey);
                var result = rsa.Decrypt(e.EncryptedHash);

                var hash = SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(_EhloMessage));
                if (e.Valid = hash.SequenceEqual(result))
                    KeyX();
                else
                    Terminate();
            }
        };

        _Client.KeyvMessage += (sender, e) =>
        {
            _Logger.LogInformation($"key validate {e.SequenceId}");

            try
            {
                using (var rsa = new tinyRSA())
                {
                    rsa.ImportPrivateKey(_privateKey);

                    _IV = rsa.Decrypt(e.EncryptedIV);

                    var hash = SHA256.Create().ComputeHash(_IV);
                    if (hash.SequenceEqual(e.IVHash))
                    {
                        // create laverna object ..
                        _Laverna = new Laverna(_Key, _IV);

                        // send connection secured 
                        ConS();
                    }
                    else
                        Terminate();
                }
            }
            catch (Exception ex)
            {
                _Logger.LogCritical(ex.ToString());
                _Client?.Dispose();
            }

        };

        _Client.CRawMessage += (sender, e) =>
        {
            _Logger.LogInformation($"crytpted raw message {e.SequenceId}");

            var unencrypted_raw = _Laverna.Decode(e.EncryptedData);
            var hash = SHA256.Create().ComputeHash(unencrypted_raw);

            if (e.Valid = hash.SequenceEqual(e.Hash))
                e.UnencryptedData = unencrypted_raw;
        };

        _Client.URawMessage += (sender, e) =>
        {
            _Logger.LogInformation($"unencrypted raw message ...");

            ParseMessage(sender, e.Data);
        };

        _Client.Terminate += (sender, e) =>
        {
            _Logger.LogInformation("terminate");

            _Client?.Dispose();
        };

        _Client.Message += (sender, e) =>
            _Logger.LogInformation($"wohooo .. got message {e.Message}");
    }

    public abstract bool ParseMessage(object sender, byte[] Message);

    public void Connect(string IP, int Port)
    {
        _Logger.LogInformation($"connect {IP}:{Port}");

        StopClient();

        _C = BindCommon();
        _Client = new(_Logger);

        DoCreateKeys();
        DoCertificates();
        DoBindEvents();

        _Client.Connect(IP, Port);
        Ehlo();
    }

    public void StopClient()
    {
        _Logger.LogInformation($"stop client");

        _Client?.StopClient();
        _Client = null!;
    }

    public void Ping()
    {
        _Logger.LogInformation($"send ping");
        _Client?.SendBuffer(
            new MessagePing(_Client.Signature1)
            .Write());
    }

    public void Terminate()
    {
        _Logger.LogInformation($"send terminate");
        _Client?.SendBuffer(
            new MessageTerm(_Client.Signature1)
            .Write());

        _Client?.Dispose();
    }

    public void Ehlo()
    {
        _Logger.LogInformation($"send ehlo {_SequenceId = Guid.NewGuid()}");
        
        var ehlo = new MessageEhlo(_Client.Signature1);
        ehlo.dataBlock.SequenceId = SequenceId;
        ehlo.dataBlock.PublicKey = _publicKey;
        ehlo.dataBlock.Message = _EhloMessage = $"EHLO {_Client.Id} {Helpers.RandomString(32)}";

        _Client?.SendBuffer(ehlo.Write());
    }

    public void SigX()
    {
        _Logger.LogInformation($"send signature exchange {SequenceId}");
        
        var sigx = new MessageSigx(_Client.Signature1);
        sigx.dataBlock.SequenceId = SequenceId;

        try
        {
            var hash = SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(_EhloMessage));
            using (var rsa = new tinyRSA())
            {
                rsa.ImportPublicKey(_foreignKey);
                sigx.dataBlock.EncryptedHash = rsa.Encrypt(hash);
            }

            _Client?.SendBuffer(sigx.Write());
        }
        catch (Exception ex)
        {
            _Logger.LogCritical(ex.ToString());
            _Client?.Dispose();
        }
    }

    public void KeyX()
    {
        _Logger.LogInformation($"send key exchange {SequenceId}");
        
        var keyx = new MessageKeyx(_Client.Signature1);
        keyx.dataBlock.SequenceId = SequenceId;

        try
        {
            using (var rsa = new tinyRSA())
            {
                rsa.ImportPublicKey(_foreignKey);

                // create new one ...
                using (var _2898 = new Rfc2898DeriveBytes(_Pass, _Salt, Common.Iterations, HashAlgorithmName.SHA256))
                    keyx.dataBlock.EncryptedKey = rsa.Encrypt(_Key = _2898.GetBytes(16));

                keyx.dataBlock.KeyHash = SHA256.Create().ComputeHash(_Key);
            }

            _Client?.SendBuffer(keyx.Write());
        }
        catch (Exception ex)
        {
            _Logger.LogCritical(ex.ToString());
            _Client?.Dispose();
        }
    }

    public void ConS()
    {
        _Logger.LogInformation($"send connection secured {SequenceId}");

        var cons = new MessageConS(_Client.Signature1);
        cons.dataBlock.SequenceId = SequenceId;

        try
        {
            _Client?.SendBuffer(cons.Write());
        }
        catch (Exception ex)
        {
            _Logger.LogCritical(ex.ToString());
            _Client?.Dispose();
        }
    }

    public void CRaw(byte[] message)
    {
        _Logger.LogInformation($"send crytped raw {SequenceId}");

        var craw = new MessageCRaw(_Client.Signature1);
        craw.dataBlock.SequenceId = SequenceId;
        craw.dataBlock.Hash = SHA256.Create().ComputeHash(message);
        craw.dataBlock.EncryptedData = _Laverna.Encode(message);

        try
        {
            _Client?.SendBuffer(craw.Write());
        }
        catch (Exception ex)
        {
            _Logger.LogCritical(ex.ToString());
            _Client?.Dispose();
        }
    }

    public void Dispose()
    {
        _Client?.Dispose();
        _Client = null;

        GC.SuppressFinalize(this);
    }

    public CommonNetSecureClient(ILogger logger)
        => _Logger = logger;

    public CommonNetSecureClient()
        : this(null) { }

}
