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
using System.Net.WebSockets;

namespace Nox.Grid.Net;

public enum ClientAuthStateEnum
{
    None = 0,
    Greet = 1,
    Signed = 2,
    Keyed = 4,
}

public class ClientData
{
    private static int IndexInstanceCounter = 0;

    #region Properties
    public int Index { get; } = ++IndexInstanceCounter;

    public Guid SequenceId { get; set; } = Guid.Empty;

    public byte[] PublicKey { get; set; } = null!;

    public byte[] Key { get; set; } = null!;

    public byte[] Salt { get; set; } = null!;

    public byte[] IV { get; set; } = null!;

    public string Message { get; set; } = null!;

    public ClientAuthStateEnum ClientAuthState { get; set; } = ClientAuthStateEnum.None;

    public Laverna Laverna { get; set; } = null!;
    #endregion

    public ClientData() { }
}

public class ClientDataKeeper<U>
    where U : ClientData
{
    private Dictionary<Guid, U> _Items = new();

    public U this[Guid key]
    {
        get
        {
            U Result;
            if (_Items.TryGetValue(key, out Result))
                return Result;
            else
                return null;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _Items[key] = value;
        }
    }

    public U GetAddValue(Guid key, Func<U> newItem)
    {
        var Item = this[key];

        if (Item == null)
        {
            Item = newItem.Invoke();
            this[key] = Item;
        }

        return Item;
    }

    public void Remove(Guid key)
        => _Items.Remove(key);
}

public abstract class CommonNetSecureServer<T>
    : IDisposable where T : ClientData, new()
{
    protected readonly ILogger _Logger;

    private Common _C;

    private byte[] _privateKey = null!;
    private byte[] _publicKey = null!;

    private byte[] _Pass = null!;
    private byte[] _Salt = null!;

    private string _FingerPrint = "";

    private readonly ClientDataKeeper<T> _ClientDataKeeper = new();
    private NetSecureServer _Server = null!;

    #region Properties
    public Common C { get => _C; }

    public bool StillListen { get => _Server?.Bound ?? false; }

    public string FingerPrint { get => _FingerPrint; }
    #endregion

    protected Guid SenderId(object sender)
        => (sender as INetBase)?.Id ?? throw new NullReferenceException("sender must not be null");

    protected ClientData GetAddClientDataItem(object sender)
        => _ClientDataKeeper.GetAddValue(SenderId(sender), () => new());

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
        _Server.PingMessage += (sender, e)
            => _Logger.LogInformation($"got ping: {e.Id}@{e.Timestamp}");

        _Server.EchoMessage += (sender, e)
            => _Logger.LogInformation($"got echo: {e.PingId}@{e.EchoTimestamp}");

        _Server.ObtainRplyMessage += (sender, e)
            => e.Message = $"RPLY {_Server.Id} {Helpers.RandomString(512)}";

        _Server.ObtainPublicKey += (sender, e)
            => e.publicKey = _publicKey;

        _Server.EhloMessage += (sender, e) =>
        {
            _Logger.LogInformation($"ehlo {e.SequenceId}");

            var cd = GetAddClientDataItem(sender);
            cd.SequenceId = e.SequenceId;
            cd.PublicKey = e.PublicKey;
            cd.Message = e.Message;
        };

        _Server.SigxMessage += (sender, e) =>
        {
            _Logger.LogInformation($"signature exchange {e.SequenceId}");

            try
            {
                using (var rsa = new tinyRSA())
                {
                    var cd = GetAddClientDataItem(sender);

                    rsa.ImportPrivateKey(_privateKey);
                    var result = rsa.Decrypt(e.EncryptedHash);

                    var hash = SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(cd.Message));
                    if (e.Valid = hash.SequenceEqual(result))
                        SigV(sender);
                    else
                        Terminate(sender);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogCritical(ex.ToString());
                Terminate(sender);
            }

        };
        _Server.KeyxMessage += (sender, e) =>
        {
            _Logger.LogInformation($"key exchange {e.SequenceId}");

            try
            {
                using (var rsa = new tinyRSA())
                {
                    var cd = GetAddClientDataItem(sender);

                    rsa.ImportPrivateKey(_privateKey);
                    var result = rsa.Decrypt(e.EncryptedKey);

                    var hash = SHA256.Create().ComputeHash(result);
                    if (hash.SequenceEqual(e.KeyHash))
                    {
                        cd.Key = result;
                        KeyV(sender);
                    }
                    else
                        Terminate(sender);

                    //_Logger.LogTrace($"SERVER KEY: {Helpers.EnHEX(cd.Key)}");
                    //_Logger.LogTrace($"SERVER IV: {Helpers.EnHEX(cd.IV)}");
                }
            }
            catch (Exception ex)
            {
                _Logger.LogCritical(ex.ToString());
                Terminate(sender);
            }
        };

        _Server.ConsMessage += (sender, e) =>
        {
            _Logger.LogInformation("connection secured");

            try
            {
                using (var rsa = new tinyRSA())
                {
                    var cd = GetAddClientDataItem(sender);

                    // set laverna encryption
                    cd.Laverna = new Laverna(cd.Key, cd.IV);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogCritical(ex.ToString());
                Terminate(sender);
            }
        };

        _Server.CRawMessage += (sender, e) =>
        {
            _Logger.LogInformation($"crytpted raw message {e.SequenceId}");

            var cd = GetAddClientDataItem(sender);

            var unencrypted_raw = cd.Laverna.Decode(e.EncryptedData);
            var hash = SHA256.Create().ComputeHash(unencrypted_raw);

            if (e.Valid = hash.SequenceEqual(e.Hash))
                e.UnencryptedData = unencrypted_raw;
        };

        _Server.URawMessage += (sender, e) =>
        {
            _Logger.LogInformation($"unencrypted raw message ...");
            
            ParseMessage(sender, e.Data);
        };

        _Server.Terminate += (sender, e) =>
        {
            _Logger.LogInformation($"terminate");

            //TODO: remove cd items ...
        };

        _Server.Message += (sender, e) =>
            _Logger.LogInformation($"wohooo .. got message {e.Message}");
    }

    public abstract bool ParseMessage(object sender, byte[] Message);

    public void Bind(string IP, int Port)
    {
        _Logger.LogInformation($"bind {IP}:{Port}");

        _C = BindCommon();
        _Server = new NetSecureServer(_Logger);

        DoCreateKeys();
        DoCertificates();
        DoBindEvents();

        _Server.Bind(IP, Port);
    }

    public void Ping()
    {
        _Logger.LogInformation($"send ping");

        _Server?.SendBuffer(new MessagePing(_Server.Signature1).Write());
    }

    public void SigV(object sender)
    {
        var cd = GetAddClientDataItem(sender);

        _Logger.LogInformation($"send signature validate {cd.SequenceId}");

        var sigv = new MessageSigv(_Server.Signature1);
        sigv.dataBlock.SequenceId = cd.SequenceId;

        try
        {
            var hash = SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(cd.Message));
            using (var rsa = new tinyRSA())
            {
                rsa.ImportPublicKey(cd.PublicKey);
                sigv.dataBlock.EncryptedHash = rsa.Encrypt(hash);
            }

            _Server.SendBufferTo(SenderId(sender), sigv.Write());
        }
        catch (Exception ex)
        {
            _Logger.LogCritical(ex.ToString());
            Terminate(sender);
        }
    }

    public void KeyV(object sender)
    {
        var cd = GetAddClientDataItem(sender);

        _Logger.LogInformation($"send key validate {cd.SequenceId}");

        var keyv = new MessageKeyv(_Server.Signature1);
        keyv.dataBlock.SequenceId = cd.SequenceId;

        try
        {
            using (var rsa = new tinyRSA())
            {
                rsa.ImportPublicKey(cd.PublicKey);

                // create new one ...
                using (var _2898 = new Rfc2898DeriveBytes(_Pass, _Salt, Common.Iterations, HashAlgorithmName.SHA256))
                    keyv.dataBlock.EncryptedIV = rsa.Encrypt(cd.IV = _2898.GetBytes(16));

                keyv.dataBlock.IVHash = SHA256.Create().ComputeHash(cd.IV);
            }

            _Server.SendBufferTo(SenderId(sender), keyv.Write());
        }
        catch (Exception ex)
        {
            _Logger.LogCritical(ex.ToString());
            Terminate(sender);
        }
    }

    public void ConS(object sender)
    {
        var cd = GetAddClientDataItem(sender);

        _Logger.LogInformation($"send connection secured {cd.SequenceId}");

        var cons = new MessageKeyv(_Server.Signature1);   
        cons.dataBlock.SequenceId = cd.SequenceId;

        try 
        {
            using (var rsa = new tinyRSA())
            {
                rsa.ImportPublicKey(cd.PublicKey);

                // create new one ...
                using (var _2898 = new Rfc2898DeriveBytes(_Pass, _Salt, Common.Iterations, HashAlgorithmName.SHA256))
                    cons.dataBlock.EncryptedIV = rsa.Encrypt(cd.IV = _2898.GetBytes(16));

                cons.dataBlock.IVHash = SHA256.Create().ComputeHash(cd.IV);
            }

            _Server.SendBufferTo(SenderId(sender), cons.Write());
        }
        catch (Exception ex)
        {
            _Logger.LogCritical(ex.ToString());
            Terminate(sender);
        }
    }

    public void CRaw(object sender, byte[] message)
    {
        var cd = GetAddClientDataItem(sender);

        _Logger.LogInformation($"send crytped raw {cd.SequenceId}");

        var craw = new MessageCRaw(_Server.Signature1);
        craw.dataBlock.SequenceId = cd.SequenceId;
        craw.dataBlock.Hash = SHA256.Create().ComputeHash(message);
        craw.dataBlock.EncryptedData = cd.Laverna.Encode(message);

        try
        {
            _Server?.SendBufferTo(SenderId(sender), craw.Write());
        }
        catch (Exception ex)
        {
            _Logger.LogCritical(ex.ToString());
            Terminate(sender);
        }
    }

    public void Terminate(object sender)
    {
        var cd = GetAddClientDataItem(sender);

        _Logger.LogInformation($"terminate {cd.SequenceId}");

        _Server.SendBufferTo(SenderId(sender), new MessageTerm(_Server.Signature1).Write());
    }

    public CommonNetSecureServer()
    : this(null) { }

    public CommonNetSecureServer(ILogger logger)
        => _Logger = logger;

    public void Dispose()
    {
        _Server?.Dispose();
        GC.SuppressFinalize(this);
    }
}
