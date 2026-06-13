using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace PlayingCards.Durak.Blazor.Services;

/// <summary>
/// Сессия игрока (имя + secret), хранится в браузере. Аналог cookie auth_name/auth_secret.
/// </summary>
public class PlayerSession(ProtectedLocalStorage storage)
{
    private const string NameKey = "auth_name";
    private const string SecretKey = "auth_secret";

    public string? Name { get; private set; }
    public string? Secret { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Secret);

    /// <summary>
    /// Загрузить сессию из хранилища (вызывать в OnAfterRenderAsync, firstRender).
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            var name = await storage.GetAsync<string>(NameKey);
            var secret = await storage.GetAsync<string>(SecretKey);
            Name = name.Success ? name.Value : null;
            Secret = secret.Success ? secret.Value : null;
        }
        catch
        {
            Name = null;
            Secret = null;
        }
    }

    public async Task LoginAsync(string name)
    {
        Name = name;
        Secret = Guid.NewGuid().ToString();
        await storage.SetAsync(NameKey, Name);
        await storage.SetAsync(SecretKey, Secret);
    }

    public async Task LogoutAsync()
    {
        Name = null;
        Secret = null;
        await storage.DeleteAsync(NameKey);
        await storage.DeleteAsync(SecretKey);
    }
}
