using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nox.WebApi;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Nox.Web;

[ApiController]
public abstract class WebApi(ILogger Logger)
    : ControllerBase
{
    protected abstract string RootKey { get; }

    protected string Token
    {
        get
        {
            Logger.LogDebug("read request token");
            return HttpContext.Request.Headers["Token"]!;
        }
    }

    protected string Secret
    {
        get
        {
            Logger.LogDebug("read request secret");
            return HttpContext.Request.Headers["Secret"]!;
        }
    }

    protected T TestToken<T>(Func<T> OnSuccess)
        where T : IShell, new()
    {
        Logger.LogDebug("test token");

        if (string.IsNullOrEmpty(Token))
        {
            string msg = "token not found";

            Logger.LogDebug(msg);
            return Shell.Failure<T>(msg);
        }
        else
            return OnSuccess.Invoke();
    }

    protected T RootOnly<T>([NotNull] DoubleDataResponseShell ApiKey, Func<T> OnAccessGranted)
        where T : IShell, new()
    {
        if (ApiKey.AdditionalData2.Equals(RootKey, StringComparison.InvariantCultureIgnoreCase))
        {
            Logger.LogDebug($"{RootKey} only, access granted");
            return Logger.LogShell(OnAccessGranted.Invoke());
        }
        else
        {
            Logger.LogDebug($"{RootKey} only, access denied");
            return Logger.LogShell(Shell.NotAuthorized<T>());
        }
    }

    protected T RouteApiKeyAccess<T>(DoubleDataResponseShell ApiKey, Guid Entity,
        Func<T> OnPassEntity, Func<T> OnXAuth = null!)
        where T : IShell, new()
    {
        try
        {
            if (ApiKey.AdditionalData2.Equals(RootKey, StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.LogDebug("route api key access, access granted");
                if (OnXAuth != null)
                {
                    Logger.LogDebug($"{RootKey} entity, invoke");
                    return Logger.LogShell(OnXAuth.Invoke());
                }
                else
                {
                    // if root, pass always
                    Logger.LogDebug("route api key access, pass");
                    return Logger.LogShell(OnPassEntity.Invoke());
                }
            }
            else
            {
                Logger.LogDebug("route api key access, access revoked");

                if (Guid.Parse(ApiKey.AdditionalData1).Equals(Entity))
                {
                    Logger.LogDebug("route api key access, pass");
                    return Logger.LogShell(OnPassEntity.Invoke());
                }
                else
                {
                    Logger.LogDebug("route api key access, not authorized");
                    return Logger.LogShell(Shell.NotAuthorized<T>());
                }
            }

        }
        catch (Exception e)
        {
            Logger.LogException(e, nameof(RouteApiKeyAccess));
            return Logger.LogShell(Shell.Error<T>(Helpers.SerializeException(e)));
        }
    }
}