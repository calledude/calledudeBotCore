using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IHttpClientWrapper
{
	Task<(bool, T?)> GetAsJsonAsync<T>(string url, JsonTypeInfo<T> typeInfo);
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

	public async Task<(bool, T?)> GetAsJsonAsync<T>(string url, JsonTypeInfo<T> typeInfo)
	{
		var client = _clientFactory.CreateClient();
		try
		{
			return (true, await client.GetFromJsonAsync(url, typeInfo));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occured when sending a request to {url}.", url);
			return default;
		}
	}
}
