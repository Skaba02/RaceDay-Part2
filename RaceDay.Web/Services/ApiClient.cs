using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Helpers;

namespace RaceDay.Web.Services;

/// <summary>The outcome of one call to the API.</summary>
public class ApiResult
{
    public bool Success { get; set; }
    public HttpStatusCode Status { get; set; }
    public string? Message { get; set; }

    /// <summary>True when the API says the user is not logged in (for example after the API restarted).</summary>
    public bool IsUnauthorized => Status == HttpStatusCode.Unauthorized;
}

/// <summary>The outcome of one call to the API, with the data it returned.</summary>
public class ApiResult<T> : ApiResult
{
    public T? Data { get; set; }
}

/// <summary>A file (for example an image) returned by the API.</summary>
public record ApiFile(byte[] Bytes, string ContentType);

/// <summary>
/// The only class that talks to the RaceDay API. Every page gets its data through here.
/// It keeps each user's API session cookie in that user's website session,
/// so the API knows who is logged in and what role they have.
/// </summary>
public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    // ---------- Public methods ----------

    public async Task<ApiResult<T>> GetAsync<T>(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync<T>(request);
    }

    public async Task<ApiResult<T>> PostAsync<T>(string url, object? body = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (body != null) request.Content = JsonContent.Create(body, options: JsonOptions);
        return await SendAsync<T>(request);
    }

    public async Task<ApiResult> PostAsync(string url, object? body = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (body != null) request.Content = JsonContent.Create(body, options: JsonOptions);
        return await SendAsync(request);
    }

    public async Task<ApiResult> PutAsync(string url, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        return await SendAsync(request);
    }

    public async Task<ApiResult> DeleteAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        return await SendAsync(request);
    }

    /// <summary>Sends an uploaded file to the API (the API stores it in Blob Storage).</summary>
    public async Task<ApiResult> PostFileAsync(string url, IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        return await SendAsync(request);
    }

    /// <summary>Downloads a file from the API. Returns null when there is no file.</summary>
    public async Task<ApiFile?> GetFileAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        AttachCookie(request);

        try
        {
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            return new ApiFile(bytes, contentType);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    // ---------- Private helpers ----------

    private async Task<ApiResult> SendAsync(HttpRequestMessage request)
    {
        AttachCookie(request);

        try
        {
            using var response = await _http.SendAsync(request);
            CaptureCookie(response);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResult { Success = true, Status = response.StatusCode };
            }

            return new ApiResult
            {
                Success = false,
                Status = response.StatusCode,
                Message = await ReadErrorAsync(response)
            };
        }
        catch (HttpRequestException)
        {
            return Unreachable<ApiResult>();
        }
        catch (TaskCanceledException)
        {
            return Unreachable<ApiResult>();
        }
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request)
    {
        AttachCookie(request);

        try
        {
            using var response = await _http.SendAsync(request);
            CaptureCookie(response);

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResult<T>
                {
                    Success = false,
                    Status = response.StatusCode,
                    Message = await ReadErrorAsync(response)
                };
            }

            try
            {
                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                return new ApiResult<T> { Success = true, Status = response.StatusCode, Data = data };
            }
            catch (JsonException)
            {
                return new ApiResult<T>
                {
                    Success = false,
                    Status = HttpStatusCode.BadGateway,
                    Message = "The API sent an answer the website could not read."
                };
            }
        }
        catch (HttpRequestException)
        {
            return Unreachable<ApiResult<T>>();
        }
        catch (TaskCanceledException)
        {
            return Unreachable<ApiResult<T>>();
        }
    }

    private static TResult Unreachable<TResult>() where TResult : ApiResult, new() => new()
    {
        Success = false,
        Status = HttpStatusCode.ServiceUnavailable,
        Message = "The RaceDay API cannot be reached right now. Please check that it is running and try again."
    };

    /// <summary>Adds this user's API session cookie to the request.</summary>
    private void AttachCookie(HttpRequestMessage request)
    {
        var cookie = Session.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }
    }

    /// <summary>When the API starts a session (at login), remember its cookie for this user.</summary>
    private void CaptureCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return;

        foreach (var value in values)
        {
            var pair = value.Split(';')[0];
            if (pair.StartsWith(".AspNetCore.Session=", StringComparison.OrdinalIgnoreCase))
            {
                Session.SetString(SessionKeys.ApiCookie, pair);
            }
        }
    }

    /// <summary>Turns an API error answer into one short sentence for the user.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(text))
            {
                using var document = JsonDocument.Parse(text);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    {
                        return message.GetString()!;
                    }

                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var field in errors.EnumerateObject())
                        {
                            if (field.Value.ValueKind == JsonValueKind.Array && field.Value.GetArrayLength() > 0)
                            {
                                return field.Value[0].GetString() ?? "Please check the information you entered.";
                            }
                        }

                        return "Please check the information you entered.";
                    }
                }
            }
        }
        catch (JsonException)
        {
            // The answer was not JSON. Fall through to the standard sentences below.
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Please log in again.",
            HttpStatusCode.Forbidden => "You are not allowed to do that.",
            HttpStatusCode.NotFound => "That could not be found.",
            HttpStatusCode.Conflict => "That cannot be done because of the current data.",
            HttpStatusCode.BadRequest => "Please check the information you entered.",
            _ => "Something went wrong on the server. Please try again."
        };
    }
}
