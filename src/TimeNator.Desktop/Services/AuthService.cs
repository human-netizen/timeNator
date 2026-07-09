using System.Net;
using System.Net.Http.Json;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.Services;

public interface IAuthService
{
    Guid? UserId { get; }
    string? DisplayName { get; }
    Task<bool> TryRestoreAsync(CancellationToken cancellationToken = default);
    Task<string?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<string?> RegisterAsync(string email, string password, string displayName,
        CancellationToken cancellationToken = default);
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Owns the tokens. Uses its own HttpClient without the auth handler, since it is
/// the thing the auth handler calls to get a token.
/// </summary>
public class AuthService(HttpClient http, TokenStore store, TimeProvider clock) : IAuthService
{
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromSeconds(60);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private AuthResponse? _current;

    public Guid? UserId => _current?.UserId;
    public string? DisplayName => _current?.DisplayName;

    public async Task<bool> TryRestoreAsync(CancellationToken cancellationToken = default)
    {
        var stored = store.Load();
        if (stored is null)
            return false;
        return await RefreshAsync(stored.RefreshToken, cancellationToken);
    }

    public Task<string?> LoginAsync(string email, string password, CancellationToken cancellationToken = default) =>
        SignInAsync("api/auth/login", new LoginRequest(email, password), cancellationToken);

    public Task<string?> RegisterAsync(string email, string password, string displayName,
        CancellationToken cancellationToken = default) =>
        SignInAsync("api/auth/register", new RegisterRequest(email, password, displayName), cancellationToken);

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var current = _current;
        if (current is null)
            return null;
        if (current.AccessTokenExpiresAt - clock.GetUtcNow() > RefreshMargin)
            return current.AccessToken;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (_current is not null && _current.AccessTokenExpiresAt - clock.GetUtcNow() > RefreshMargin)
                return _current.AccessToken;
            return await RefreshAsync(current.RefreshToken, cancellationToken) ? _current!.AccessToken : null;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var current = _current;
        _current = null;
        store.Clear();
        if (current is null)
            return;

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
        request.Headers.Authorization = new("Bearer", current.AccessToken);
        request.Content = JsonContent.Create(new RefreshRequest(current.RefreshToken));
        try
        {
            await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // The token is already forgotten locally; the server copy expires on its own.
        }
    }

    private async Task<string?> SignInAsync<T>(string path, T body, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync(path, body, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return "The server could not be reached.";
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return await ProblemReader.ReadMessageAsync(response, cancellationToken);

            Accept((await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken))!);
            return null;
        }
    }

    private async Task<bool> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.PostAsJsonAsync(
                "api/auth/refresh", new RefreshRequest(refreshToken), cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _current = null;
                store.Clear();
                return false;
            }
            response.EnsureSuccessStatusCode();
            Accept((await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken))!);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private void Accept(AuthResponse response)
    {
        _current = response;
        store.Save(new StoredLogin(response.UserId, response.DisplayName, response.RefreshToken));
    }
}
