using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using AdminP.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public interface IAuthService
{
    Task<bool> LoginAsync(string name, string password);
    Task LogoutAsync();
    bool IsAuthenticated { get; }
    Task<string> GetTokenAsync();
    Task<HttpResponseMessage> SendWithAuthAsync(Func<HttpRequestMessage> requestFactory);
}

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly NavigationManager _navigation;
    private readonly IJSRuntime _jsRuntime;

    public bool IsAuthenticated { get; private set; }

    public AuthService(HttpClient http, NavigationManager navigation, IJSRuntime jsRuntime)
    {
        _http = http;
        _navigation = navigation;
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> LoginAsync(string name, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("auth/login/admin", new { name, password });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResults>();
                if (result is not null)
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "accessToken",
                        result.AccessToken ?? string.Empty);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken",
                        result.RefreshToken ?? string.Empty);
                }

                IsAuthenticated = true;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "accessToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
        IsAuthenticated = false;
        _navigation.NavigateTo("/admin-login");
    }

    public async Task<string> GetTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "accessToken");
    }

    public async Task<HttpResponseMessage> SendWithAuthAsync(Func<HttpRequestMessage> requestFactory)
    {
        // 1) Ставим accessToken и отправляем
        var accessToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "accessToken");
        var request = requestFactory();
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        var response = await _http.SendAsync(request);

        // 2) Если 419 (требуется обновление) — пробуем рефрешнуть и повторить
        if ((int)response.StatusCode == 419)
        {
            var refreshToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "refreshToken");
            if (string.IsNullOrEmpty(refreshToken))
            {
                await ClearTokensAsync();
                return response;
            }

            try
            {
                var refreshResponse = await _http.PostAsJsonAsync("auth/refresh", new { refreshToken });
                if (refreshResponse.IsSuccessStatusCode)
                {
                    var authResult = await refreshResponse.Content.ReadFromJsonAsync<LoginRefresh>();
                    if (authResult != null)
                    {
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "accessToken", authResult.newAccessToken);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", authResult.newRefreshToken);

                        // Повторная отправка исходного запроса с новым accessToken
                        var retry = requestFactory();
                        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.newAccessToken);
                        response = await _http.SendAsync(retry);
                    }
                }
            }
            catch
            {
                await ClearTokensAsync();
            }
        }

        return response;
    }

    private async Task ClearTokensAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "accessToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
    }
    
}