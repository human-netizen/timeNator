using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TimeNator.Desktop.Services;

public record StoredLogin(Guid UserId, string DisplayName, string RefreshToken);

/// <summary>
/// Keeps the refresh token between launches, encrypted with DPAPI for the current
/// Windows user. The access token is short-lived and never written to disk.
/// </summary>
public class TokenStore
{
    private readonly string _path = Path.Combine(AppPaths.DataDirectory, "login.bin");

    public StoredLogin? Load()
    {
        try
        {
            if (!File.Exists(_path))
                return null;
            var json = Unprotect(File.ReadAllBytes(_path));
            return JsonSerializer.Deserialize<StoredLogin>(json);
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or IOException)
        {
            return null;
        }
    }

    public void Save(StoredLogin login)
    {
        Directory.CreateDirectory(AppPaths.DataDirectory);
        File.WriteAllBytes(_path, Protect(JsonSerializer.Serialize(login)));
    }

    public void Clear() => File.Delete(_path);

    private static byte[] Protect(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        return OperatingSystem.IsWindows()
            ? ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser)
            : bytes;
    }

    private static string Unprotect(byte[] data) =>
        Encoding.UTF8.GetString(OperatingSystem.IsWindows()
            ? ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser)
            : data);
}
