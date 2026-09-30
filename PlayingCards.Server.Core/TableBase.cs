namespace PlayingCards.Server.Core;

/// <summary>
/// Игровой стол.
/// </summary>
public abstract class TableBase<TPlayer>
    where TPlayer : TablePlayerBase
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Порядковый номер стола.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Секреты игроков, чтоб понять, кто есть кто.
    /// </summary>
    public List<TPlayer> Players { get; set; } = null!;

    private int _version;

    /// <summary>
    /// Номер версии данных, на любой чих мы его повышаем.
    /// </summary>
    public int Version
    {
        get => _version;
        set
        {
            _version = value;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Событие любого изменения стола (для push в UI).
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Лочит все мутации этого стола: клиентские вызовы (PlayCards/Take/Beat/...) и фоновый тик
    /// конкурируют за один и тот же стол без синхронизации иначе. Порядок захвата всегда
    /// холдер снаружи → SyncRoot внутри.
    /// </summary>
    public readonly object SyncRoot = new();
}
