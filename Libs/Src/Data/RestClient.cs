using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Nox.WebApi;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Nox;

namespace Nox.Data;

public class RestClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _Logger;

    public string BaseURL { get; } = null!;

    public async Task<T> RestGetAsync<T>(string Path, params KeyValue[] CustomHeaders)
        where T : class
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Path);

            foreach (var Item in CustomHeaders)
                request.Headers.Add(Item.Key, Item.Value);

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsStringAsync();

            return await Task.Run(() => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(data)!);
        }
        catch (Exception e)
        {
            _Logger.LogException(e, nameof(RestGetAsync));
            throw;
        }
        finally
        {
            foreach (var Item in CustomHeaders)
                _httpClient.DefaultRequestHeaders.Remove(Item.Key);
        }
    }

    public T RestGet<T>(string Path, params KeyValue[] CustomHeaders)
        where T : class
        => AsyncHelper.RunSync<T>(async () => await RestGetAsync<T>(Path, CustomHeaders));

    public async Task<T> RestPostAsync<T>(string Path, IPostShell Content, params KeyValue[] CustomHeaders)
        where T : class
    {
        _Logger?.LogTrace(Path, nameof(RestPostAsync));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Path)
            {
                Content = new StringContent(JsonConvert.SerializeObject(Content), Encoding.UTF8, "application/json")
            };
            foreach (var Item in CustomHeaders)
                request.Headers.Add(Item.Key, Item.Value);

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsStringAsync();

            return await Task.Run(() => JsonConvert.DeserializeObject<T>(data)!);
        }
        catch (Exception e)
        {
            _Logger.LogException(e, nameof(RestPostAsync));
            _Logger.LogDebug($"{nameof(RestPostAsync)}: {Path}");
            throw;
        }
        finally
        {
            foreach (var Item in CustomHeaders)
                _httpClient.DefaultRequestHeaders.Remove(Item.Key);
        }
    }

    public T RestPost<T>(string Path, IPostShell content, params KeyValue[] CustomHeaders)
        where T : class
        => AsyncHelper.RunSync<T>(async () => await RestPostAsync<T>(Path, content, CustomHeaders));

    public RestClient(string BaseURL, ILogger Logger)
    {
        this.BaseURL = BaseURL;
        this._Logger = Logger;

        _httpClient = new()
        {
            BaseAddress = new Uri(BaseURL),
            Timeout = TimeSpan.FromSeconds(30)
        };

        //        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        //_httpClient.DefaultRequestHeaders.Accept.Add(
        //    new MediaTypeWithQualityHeaderValue("text/plain"));
    }

    // DI-Constructor
    public RestClient(string BaseURL, ILogger<RestClient> Logger)
        : this(BaseURL, (ILogger)Logger) { }
}
