using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IHttpClientWrapper
{
    Task<(bool, T?)> GetAsJsonAsync<T>(string url);
}

public class HttpClientWrapper : IHttpClientWrapper
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<HttpClientWrapper> _logger;

    public HttpClientWrapper(IHttpClientFactory clientFactory, ILogger<HttpClientWrapper> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<(bool, T?)> GetAsJsonAsync<T>(string url)
    {
        var client = _clientFactory.CreateClient();

        HttpResponseMessage? response;

        try
        {
            response = await client.GetAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured when sending a request to {url}.", url);
            return default;
        }

        if (!response.IsSuccessStatusCode)
            return default;

        var responseContent = await response.Content.ReadAsStringAsync();
        return (true, JsonConvert.DeserializeObject<T>(responseContent));
    }
}
