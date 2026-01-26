using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace LawOfWriter.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private const string ApiBaseUrl = "https://die.sinnnlosen.de/api/";

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(ApiBaseUrl);
        
        _logger.LogInformation("ApiService initialized with base URL: {BaseUrl}", ApiBaseUrl);
    }

    /// <summary>
    /// GET Request zu einem API Endpoint
    /// </summary>
    /// <typeparam name="T">Der Typ der erwarteten Response</typeparam>
    /// <param name="endpoint">Der Endpoint (z.B. "/games" oder "/users/123")</param>
    /// <returns>Das deserialisierte Objekt vom Typ T</returns>
    public async Task<T?> GetAsync<T>(string endpoint)
    {
        _logger.LogInformation("GET Request: {Endpoint} (Type: {ResponseType})", endpoint, typeof(T).Name);
        
        try
        {
            var result = await _httpClient.GetFromJsonAsync<T>(endpoint);
            _logger.LogInformation("GET Request successful: {Endpoint}", endpoint);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP Error during GET request to {Endpoint}. Status: {StatusCode}", 
                endpoint, ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during GET request to {Endpoint}", endpoint);
            throw;
        }
    }

    /// <summary>
    /// POST Request zu einem API Endpoint
    /// </summary>
    /// <typeparam name="TRequest">Der Typ des Request Body</typeparam>
    /// <typeparam name="TResponse">Der Typ der erwarteten Response</typeparam>
    /// <param name="endpoint">Der Endpoint (z.B. "/games")</param>
    /// <param name="data">Die zu sendenden Daten</param>
    /// <returns>Das deserialisierte Response Objekt vom Typ TResponse</returns>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        _logger.LogInformation("POST Request: {Endpoint} (RequestType: {RequestType}, ResponseType: {ResponseType})", 
            endpoint, typeof(TRequest).Name, typeof(TResponse).Name);
        
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);
            
            _logger.LogDebug("POST Response Status: {StatusCode} for {Endpoint}", 
                response.StatusCode, endpoint);
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<TResponse>();
            _logger.LogInformation("POST Request successful: {Endpoint}", endpoint);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP Error during POST request to {Endpoint}. Status: {StatusCode}", 
                endpoint, ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during POST request to {Endpoint}", endpoint);
            throw;
        }
    }

    /// <summary>
    /// POST Request zu einem API Endpoint ohne Response Body
    /// </summary>
    /// <typeparam name="TRequest">Der Typ des Request Body</typeparam>
    /// <param name="endpoint">Der Endpoint</param>
    /// <param name="data">Die zu sendenden Daten</param>
    /// <returns>True wenn erfolgreich, sonst False</returns>
    public async Task<bool> PostAsync<TRequest>(string endpoint, TRequest data)
    {
        _logger.LogInformation("POST Request (no response): {Endpoint} (RequestType: {RequestType})", 
            endpoint, typeof(TRequest).Name);
        
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);
            
            _logger.LogDebug("POST Response Status: {StatusCode} for {Endpoint}", 
                response.StatusCode, endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("POST Request successful: {Endpoint}", endpoint);
                return true;
            }
            
            _logger.LogWarning("POST Request failed with status {StatusCode}: {Endpoint}", 
                response.StatusCode, endpoint);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during POST request to {Endpoint}", endpoint);
            return false;
        }
    }

    /// <summary>
    /// PUT Request zu einem API Endpoint
    /// </summary>
    /// <typeparam name="TRequest">Der Typ des Request Body</typeparam>
    /// <typeparam name="TResponse">Der Typ der erwarteten Response</typeparam>
    /// <param name="endpoint">Der Endpoint</param>
    /// <param name="data">Die zu sendenden Daten</param>
    /// <returns>Das deserialisierte Response Objekt vom Typ TResponse</returns>
    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        _logger.LogInformation("PUT Request: {Endpoint} (RequestType: {RequestType}, ResponseType: {ResponseType})", 
            endpoint, typeof(TRequest).Name, typeof(TResponse).Name);
        
        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, data);
            
            _logger.LogDebug("PUT Response Status: {StatusCode} for {Endpoint}", 
                response.StatusCode, endpoint);
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<TResponse>();
            _logger.LogInformation("PUT Request successful: {Endpoint}", endpoint);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP Error during PUT request to {Endpoint}. Status: {StatusCode}", 
                endpoint, ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during PUT request to {Endpoint}", endpoint);
            throw;
        }
    }

    /// <summary>
    /// DELETE Request zu einem API Endpoint
    /// </summary>
    /// <param name="endpoint">Der Endpoint</param>
    /// <returns>True wenn erfolgreich, sonst False</returns>
    public async Task<bool> DeleteAsync(string endpoint)
    {
        _logger.LogInformation("DELETE Request: {Endpoint}", endpoint);
        
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            
            _logger.LogDebug("DELETE Response Status: {StatusCode} for {Endpoint}", 
                response.StatusCode, endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("DELETE Request successful: {Endpoint}", endpoint);
                return true;
            }
            
            _logger.LogWarning("DELETE Request failed with status {StatusCode}: {Endpoint}", 
                response.StatusCode, endpoint);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during DELETE request to {Endpoint}", endpoint);
            return false;
        }
    }

    /// <summary>
    /// Gibt die rohe HttpResponseMessage zurück für spezielle Fälle
    /// </summary>
    /// <param name="endpoint">Der Endpoint</param>
    /// <returns>Die HttpResponseMessage</returns>
    public async Task<HttpResponseMessage> GetRawAsync(string endpoint)
    {
        _logger.LogInformation("Raw GET Request: {Endpoint}", endpoint);
        
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            _logger.LogDebug("Raw GET Response Status: {StatusCode} for {Endpoint}", 
                response.StatusCode, endpoint);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during raw GET request to {Endpoint}", endpoint);
            throw;
        }
    }
}
