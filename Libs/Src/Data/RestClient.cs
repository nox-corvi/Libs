using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace Nox.Data;

public class RestClient
{
    private HttpClient _httpClient;

    public string BaseURL { get; } = null!;

    public T RestGet<T>(string Path, string Arguments = null!)
        where T : class
    {
        string URL = Path;
        if (Arguments != null)
            URL += "?" + Arguments;

        var response = _httpClient.GetAsync(URL);
        response.Wait();
        response.Result.EnsureSuccessStatusCode();

        var data = _httpClient.GetStringAsync(URL);
        data.Wait();

        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(data.Result)!;
    }

    public async Task<T> RestGetAsync<T>(string Path, string Arguments = null!)
        where T : class
    {
        string URL = Path;
        if (Arguments != null)
        {
            if (!URL.EndsWith("?"))
            {
                URL += "?";
            }
            URL += Arguments;
        }

        var response = await _httpClient.GetAsync(URL);
        response.EnsureSuccessStatusCode();

        var data = await _httpClient.GetStringAsync(URL);

        return await Task.Run(() => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(data)!);
    }

    public RestClient(string baseURL)
    {
        this.BaseURL = baseURL;
        _httpClient = new() { BaseAddress = new Uri(baseURL) };
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }
}
