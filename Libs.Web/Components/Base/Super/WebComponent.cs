using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Nox;
using System.Text;
using System.Threading.Tasks;
using Nox.Web.Components;
using Nox.WebApi;
using System.Configuration;

namespace Nox.Web.Components.Base.Super;

public class WebComponent
    : ComponentBase, IDisposable
{
    protected ILogger _Logger;

    protected virtual void OnLoadBegin() { }
    protected virtual async Task OnLoadBeginAsync()
        => await _Logger.LogDebugAsync(nameof(OnLoadBeginAsync));

    protected virtual void OnLoadEnd()
        => _Logger.LogDebug(nameof(OnLoadEnd));
    protected virtual async Task OnLoadEndAsync()
        => await _Logger.LogDebugAsync(nameof(OnLoadEnd));

    protected virtual void OnLoadExecute()
        => _Logger.LogDebug(nameof(OnLoadExecute));
    protected virtual async Task OnLoadExecuteAsync()
        => await _Logger.LogDebugAsync(nameof(OnLoadExecuteAsync));

    protected virtual void OnLoad(CancellationToken cancellationToken)
    {
        try
        {
            OnLoadBegin();

            OnLoadExecute();
        }
        catch (TaskCanceledException tce)
        {
            _Logger.LogException(tce, nameof(OnLoad));
        }
        catch (Exception e)
        {
            _Logger.LogException(e, nameof(OnLoad));
        }
        finally
        {
            OnLoadEnd();
            StateHasChanged();
        }
    }
    protected virtual async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            await OnLoadBeginAsync();

            await OnLoadExecuteAsync();
        }
        catch (TaskCanceledException tce)
        {
            _Logger.LogException(tce, nameof(OnLoad));
        }
        catch (Exception e)
        {
            await _Logger.LogExceptionAsync(e, nameof(OnLoadAsync));
        }
        finally
        {
            await OnLoadEndAsync();
            await InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }
    }

    protected virtual void OnInitializeBegin()
        => _Logger.LogDebug(nameof(OnLoadExecute));
    protected virtual async Task OnInitializeBeginAsync()
        => await _Logger.LogDebugAsync(nameof(OnLoadExecute));

    protected virtual void OnInitializeEnd()
        => _Logger.LogDebug(nameof(OnLoadExecute));
    protected virtual async Task OnInitializeEndAsync()
        => await _Logger.LogDebugAsync(nameof(OnLoadExecuteAsync));

    protected virtual void OnInitializeExecute()
        => _Logger.LogDebug(nameof(OnInitializeExecute));
    protected virtual async Task OnInitializeExecuteAsync()
        => await _Logger.LogDebugAsync(nameof(OnInitializeExecuteAsync));

    private bool _isInternalInitialized = false;
    protected override void OnInitialized()
    {
        try
        {
            OnLoadBegin();
            base.OnInitialized();

            if (!_isInternalInitialized)
            {
                OnInitializeExecute();
            }
        }
        catch (Exception e)
        {
            _Logger.LogException(e, nameof(OnInitialized));
        }
        finally
        {
            OnInitializeEnd();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await OnInitializeBeginAsync();

            await base.OnInitializedAsync();

            if (!_isInternalInitialized)
            {
                await OnInitializeExecuteAsync();
            }
        }
        catch (Exception e)
        {
            await _Logger.LogExceptionAsync(e, nameof(OnInitializedAsync));
        }
        finally
        {
            await OnInitializeEndAsync();
        }
    }

    public virtual void Dispose() { }

    public WebComponent()
        : this(null!)
    {
        
    }

    public WebComponent(ILogger Logger)
        : base()
        => this._Logger = Logger;

    // DI-Constructor
    public WebComponent(ILogger<WebComponent> Logger)
        : this((ILogger)Logger) { }
}