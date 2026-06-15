using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using PlayingCards.Durak;

namespace PlayingCards.Durak.Blazor.Services;

/// <summary>
/// Сессия игрока (имя + secret) и его персональные настройки, хранятся в браузере.
/// Имя/secret — аналог cookie auth_name/auth_secret; настройки — подсказки и режим сортировки руки.
/// </summary>
public class PlayerSession(ProtectedLocalStorage storage)
{
    private const string NameKey = "auth_name";
    private const string SecretKey = "auth_secret";
    private const string NoHintsKey = "pref_nohints";
    private const string HandSortKey = "pref_handsort";

    public string? Name { get; private set; }
    public string? Secret { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Secret);

    /// <summary>Отключить подсказки по ходам (личная настройка). По умолчанию подсказки включены.</summary>
    public bool NoHints { get; private set; }

    /// <summary>Режим сортировки карт в руке (личная настройка). По умолчанию — как было до выбора режима.</summary>
    public HandSortMode HandSort { get; private set; } = HandSortMode.ByRankTrumpInline;

    /// <summary>Срабатывает при любом изменении <see cref="NoHints"/> или <see cref="HandSort"/>.</summary>
    public event Action? SettingsChanged;

    /// <summary>
    /// Загрузить сессию и настройки из хранилища (вызывать в OnAfterRenderAsync, firstRender —
    /// ProtectedLocalStorage доступен только после первого рендера).
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

        try
        {
            var noHints = await storage.GetAsync<bool>(NoHintsKey);
            NoHints = noHints.Success && noHints.Value;

            var handSort = await storage.GetAsync<int>(HandSortKey);
            HandSort = handSort.Success && Enum.IsDefined(typeof(HandSortMode), handSort.Value)
                ? (HandSortMode)handSort.Value
                : HandSortMode.ByRankTrumpInline;
        }
        catch
        {
            NoHints = false;
            HandSort = HandSortMode.ByRankTrumpInline;
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

    /// <summary>Переключить подсказки по ходам, сохранить и оповестить подписчиков.</summary>
    public async Task SetNoHintsAsync(bool value)
    {
        if (NoHints == value)
        {
            return;
        }

        NoHints = value;
        await storage.SetAsync(NoHintsKey, value);
        SettingsChanged?.Invoke();
    }

    /// <summary>Сменить режим сортировки руки, сохранить и оповестить подписчиков.</summary>
    public async Task SetHandSortAsync(HandSortMode mode)
    {
        if (HandSort == mode)
        {
            return;
        }

        HandSort = mode;
        await storage.SetAsync(HandSortKey, (int)mode);
        SettingsChanged?.Invoke();
    }
}
