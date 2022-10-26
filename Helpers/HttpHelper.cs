using System.Net;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace ProductAPI.Helpers;

public class HttpHelper
{
    private IHttpClientFactory _clientFactory = default!;

    public static HttpHelper WithFactory(IHttpClientFactory factory)
    {
        return new HttpHelper()
        {
            _clientFactory = factory
        };
    }

    public async Task<RequestResponse<T>> Get<T>(string path, int timeout = 4, Dictionary<string, string?> queryParams = default!)
    {
        var fullPath = queryParams != null ? QueryHelpers.AddQueryString(path, queryParams) : path;
        var request = new HttpRequestMessage(HttpMethod.Get, fullPath);

        var client = _clientFactory.CreateClient("CrawlerClient");
        client.Timeout = TimeSpan.FromSeconds(timeout);

        HttpResponseMessage response;

        try
        {
            response = await client.SendAsync(request);
        }
        catch (Exception x)
        {
            Console.Error.WriteLine($"Error: {x.Message}");
            return new RequestResponse<T>
            {
                Failed = true,
                Code = 0,
                Message = "Failed to connect with backend",
                Data = default!
            };
        }

        RequestResponse<T> apiResponse = new RequestResponse<T>()
        {
            Code = (int)response.StatusCode
        };

        var stringResponse = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode && stringResponse != null)
        {
            try
            {
                var conversionType = typeof(T);
                if (conversionType.IsPrimitive || conversionType == typeof(Decimal) || conversionType == typeof(string))
                {
                    apiResponse.Data = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(stringResponse);
                }
                else
                {
                    apiResponse.Data = JsonSerializer.Deserialize<T>(stringResponse,
                    new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                }
            }
            catch
            {
                apiResponse.Failed = true;
                apiResponse.Message = "Failed type conversion";
            }
        }
        else
        {
            apiResponse.Failed = true;
            apiResponse.Message = stringResponse ?? "Unkown fail reason within ApiServiceBase";
        }

        return apiResponse;
    }

    public async Task<RequestResponse<T>> Post<T, C>(string path, C data, int timeout = 8)
    {
        var client = _clientFactory.CreateClient("CrawlerClient");
        client.Timeout = TimeSpan.FromSeconds(timeout);

        HttpResponseMessage response;

        try
        {
            response = await client.PostAsJsonAsync<C>(path, data);
        }
        catch (Exception x)
        {
            return new RequestResponse<T>
            {
                Failed = true,
                Code = 0,
                Message = "Failed to connect with backend",
                Data = default!
            };
        }

        RequestResponse<T> apiResponse = new RequestResponse<T>();
        var stringResponse = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode && stringResponse != null)
        {
            try
            {
                var conversionType = typeof(T);
                if (conversionType.IsPrimitive || conversionType == typeof(Decimal) || conversionType == typeof(string))
                {
                    apiResponse.Data = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(stringResponse);
                }
                else
                {
                    apiResponse.Data = JsonSerializer.Deserialize<T>(stringResponse,
                        new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                }
            }
            catch
            {
                apiResponse.Failed = true;
                apiResponse.Message = "Failed type conversion";
            }
        }
        else
        {
            apiResponse.Failed = true;
            apiResponse.Message = string.IsNullOrEmpty(stringResponse) ?
                "Unkown fail reason within ApiServiceBase" :
                stringResponse.Replace("\"", "");

            apiResponse.Code = (int)response.StatusCode;
        }

        return apiResponse;
    }
}

public class RequestResponse<T>
{
    public T? Data { get; set; }
    public bool Failed { get; set; } = false;
    public int Code { get; set; } = 5;
    public string Message { get; set; } = string.Empty;
}