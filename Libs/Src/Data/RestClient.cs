using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Nox.WebApi;
using System.Net.Http.Json;
using Newtonsoft.Json;
using System.Text;

namespace Nox.Data;

public class RestClient
{
    private readonly HttpClient _httpClient;

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

            return await Task.Run(() => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(data)!);
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

    public RestClient(string baseURL)
    {
        this.BaseURL = baseURL;
        _httpClient = new()
        {
            BaseAddress = new Uri(baseURL),
            Timeout = TimeSpan.FromSeconds(30)
        };

//        _httpClient.DefaultRequestHeaders.Accept.Clear();

        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        //_httpClient.DefaultRequestHeaders.Accept.Add(
        //    new MediaTypeWithQualityHeaderValue("text/plain"));
    }
}
