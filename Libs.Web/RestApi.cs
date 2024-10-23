using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nox.Data;
using Nox.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Web;

public abstract class RestApi
    : IRestClient
{
    protected const string DTF = "yyyy-MM-dd";

    public abstract string ConfigKey { get; }

    private IConfiguration Configuration;
    private ILogger Logger;

    private string _ApiBaseUrl = "/api";
    public string _ApiVersion = "v1";

    private RestClient _restClient = null!;

    public string ApiBaseUrl
    {
        get => _ApiBaseUrl = Configuration[$"{ConfigKey}:BaseUrl"] ?? _ApiBaseUrl;
    }

    public string ApiVersion
    {
        get => _ApiVersion = Configuration[$"{ConfigKey}:Version"] ?? _ApiVersion;
    }

    protected KeyValue Token
    {
        get => new KeyValue("Token",
            Configuration["XAuth:Token"])
                ?? throw new ArgumentNullException($"{ConfigKey}:Token");
    }

    protected string BuildApiPath(string URL, params string[] Args)
    {
        string ReturnUrl = $"{ApiBaseUrl}/{ApiVersion}/{URL}";

        if (Args.Length > 0)
            ReturnUrl += "?" + Args[0];

        if (Args.Length > 1)
            for (int i = 1; i < Args.Length; i++)
                ReturnUrl += "&" + Args[i];

        return ReturnUrl;
    }

    protected string PA<T>(T value, [CallerArgumentExpression("value")] string parameterName = null!)
        => $"{parameterName}={value}";


    #region IRestClient Implementation
    public async Task<T> RestGetAsync<T>(string Path, params KeyValue[] CustomHeaders) where T : class
        => await _restClient.RestGetAsync<T>(Path, CustomHeaders);

    public T RestGet<T>(string Path, params KeyValue[] CustomHeaders) where T : class
        => _restClient.RestGet<T>(Path, CustomHeaders);

    public async Task<T> RestPostAsync<T>(string Path, IPostShell Content, params KeyValue[] CustomHeaders) where T : class
        => await _restClient.RestPostAsync<T>(Path, Content, CustomHeaders);

    public T RestPost<T>(string Path, IPostShell content, params KeyValue[] CustomHeaders) where T : class
        => _restClient.RestPost<T>(Path, content, CustomHeaders);
    #endregion

    public RestApi(IConfiguration Configuration, ILogger Logger)
    {
        this.Configuration = Configuration;
        this.Logger = Logger;

        _restClient = new RestClient(Configuration[$"{ConfigKey}:URL"] ??
            throw new ArgumentNullException($"{ConfigKey}::URL"), Logger);
    }
}
