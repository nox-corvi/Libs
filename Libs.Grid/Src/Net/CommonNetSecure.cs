using Microsoft.Extensions.Logging;
using Nox.Net.Com;
using Nox.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Grid.Net;

public class SecureSocketListener
    : Nox.Net.Com.SecureSocketListener
{

    public SecureSocketListener(uint Signature1, Socket socket, ILogger<SecureSocketListener> logger, int Timeout)
        : base(Signature1, socket, logger, Timeout)
    {
    }
}

internal class NetSecureClient
    : NetSecureClient<SecureSocketListener>
{
    public NetSecureClient()
        : this(null) { }

    public NetSecureClient(ILogger<NetSecureClient> logger)
        : base(Common.Signature1, logger)
    { }
}

internal class NetSecureServer
    : NetSecureServer<SecureSocketListener>
{
    private readonly ILogger _Logger = null!;

    public NetSecureServer()
        : this(null) { }

    public NetSecureServer(ILogger<NetSecureServer> logger)
        : base(Common.Signature1, logger)
        => _Logger = logger;
}
