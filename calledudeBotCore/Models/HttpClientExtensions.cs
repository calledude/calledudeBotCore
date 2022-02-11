using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace calledudeBot.Models;

public static class HttpClientExtensions
{
    public static async Task<(bool, T?)> GetAsJsonAsync<T>(this HttpClient client, string url)
    {
        HttpResponseMessage? response;

        try
        {
            response = await client.GetAsync(url);
        }
        catch (Exception)
        {
            return default;
        }

        if (!response.IsSuccessStatusCode)
            return default;

        var responseContent = await response.Content.ReadAsStringAsync();
        return (true, JsonConvert.DeserializeObject<T>(responseContent));
    }
}
