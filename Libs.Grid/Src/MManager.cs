using Microsoft.Extensions.Logging;
using Nox.Enyo;


public class MManager
    : Nox.Enyo._Framework.EManager
{
    private readonly ILogger _logger ;

    public MManager(ILogger logger)
           : base() 
    { 
        this._logger = logger;
    }

    public MManager()
        : base()
    {

    }
}