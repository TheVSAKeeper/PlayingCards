using NLog;
using System.Text;
using SRS = PlayingCards.Durak.Server.StopRoundStatus;

namespace PlayingCards.Durak.Server;

/// <summary>
/// Игровой стол.
/// </summary>
public class Table
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Игра.
    /// </summary>
    public Game Game { get; set; } = null!;

    /// <summary>
    /// Секреты игроков, чтоб понять, кто есть кто.
    /// </summary>
    public List<TablePlayer> Players { get; set; } = null!;

    /// <summary>
    /// Хозяин стола.
    /// </summary>
    public Player Owner { get; set; } = null!;

    /// <summary>
    /// Время начала отсчёта об окончании раунда.
    /// </summary>
    public DateTime? StopRoundBeginDate { get; set; }

    /// <summary>
    /// Причина окончания раунда.
    /// </summary>
    public StopRoundStatus? StopRoundStatus { get; set; }

    /// <summary>
    /// Игрок, покинувший игру, до её окончания.
    /// </summary>
    public Player? LeavePlayer { get; set; }

    /// <summary>
    /// Индекс игрока, покинувшего игру.
    /// </summary>
    public int? LeavePlayerIndex { get; set; }

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
    /// Порядковый номер стола.
    /// </summary>
    public int Number { get; set; }

    public void SetActivePlayerAfkStartTime()
    {
        SetAfk(Game.ActivePlayer, DateTime.UtcNow);
    }

    public void SetDefencePlayerAfkStartTime()
    {
        SetAfk(Game.ActivePlayer, null);
        SetAfk(Game.DefencePlayer, DateTime.UtcNow);
    }

    public void CleanDefencePlayerAfkStartTime()
    {
        SetAfk(Game.DefencePlayer, null);
    }

    /// <summary>
    /// Проставить AFK-засечку игроку по его игровому <see cref="Player" />. Если такого за столом уже
    /// нет (только что вышел/кикнут) или указатель хода пуст — тихо пропускаем: незалоченный рендер
    /// или ход не должен ронять circuit на <c>First</c> («Sequence contains no matching element»).
    /// </summary>
    private void SetAfk(Player? gamePlayer, DateTime? value)
    {
        if (gamePlayer == null)
        {
            return;
        }

        var tablePlayer = Players.FirstOrDefault(x => x.Player == gamePlayer);

        if (tablePlayer != null)
        {
            tablePlayer.AfkStartTime = value;
        }
    }

    public void CleanAllAfkTime()
    {
        foreach (var player in Players)
        {
            player.AfkStartTime = null;
        }
    }

    public void CleanLeaverPlayer()
    {
        LeavePlayer = null;
        LeavePlayerIndex = null;
    }

    /// <summary>
    /// Сбросить отсчёт остановки раунда, чтобы он не «выстрелил» на уже завершённой/новой партии.
    /// </summary>
    public void CleanStopRound()
    {
        StopRoundStatus = null;
        StopRoundBeginDate = null;
        ClearBeatVotes();
    }

    public void StartGame()
    {
        Game.StartGame();
        var log = new StringBuilder();
        log.AppendLine("deck: " + string.Join(' ', Game.Deck.Cards.Select(x => x.ToString())));

        for (var i = 0; i < Game.Players.Count; i++)
        {
            log.AppendLine("pl-" + i + ": " + string.Join(' ', Game.Players[i].Hand.Cards.Select(x => x.ToString())));
        }

        CleanLeaverPlayer();
        CleanStopRound();
        SetActivePlayerAfkStartTime();
        Version++;

        WriteLog("", "start game: \r\n" + log);
    }

    public void PlayCards(string playerSecret, int[] cardIndexes, int? attackCardIndex = null)
    {
        CheckGameInProcess();
        var tablePlayer = Players.Single(player => player.AuthSecret == playerSecret);

        if (attackCardIndex != null)
        {
            Defence(tablePlayer, cardIndexes.First(), attackCardIndex.Value);
        }
        else
        {
            if (Game.IsRoundStarted())
            {
                Attack(tablePlayer, cardIndexes);
            }
            else
            {
                StartAttack(tablePlayer, cardIndexes);
            }
        }

        CheckEndGame();
        Version++;
    }

    public void Take(string playerSecret)
    {
        CheckGameInProcess();

        var tablePlayer = Players.Single(x => x.AuthSecret == playerSecret);

        if (Game.DefencePlayer != tablePlayer.Player)
        {
            throw new BusinessException("Забирать карты может только защищающийся");
        }

        if (Game.Cards.Count == 0)
        {
            throw new BusinessException("На столе нет карт");
        }

        CheckStopRoundBeginDate();
        StopRoundStatus = SRS.Take;
        CleanDefencePlayerAfkStartTime();
        WriteLog(playerSecret, "take");
        Version++;
    }

    /// <summary>Реплики атакующего, объявляющего «Бито». Единый источник — и для людей, и для ботов.</summary>
    public static readonly string[] ReplyPhrases = ["Бито", "Бито!", "Закрываю"];

    /// <summary>
    /// «Бито»: атакующий объявляет, что больше не подкидывает (issue F5). Раунд закрывается досрочно,
    /// только когда «Бито» сказали ВСЕ атакующие; иначе по-прежнему ждём общий таймер удачной защиты.
    /// Пришло на смену авто-таймеру «никто не может ходить», который выдавал отсутствие карт у других.
    /// Над сказавшим всплывает реплика.
    /// </summary>
    public void Beat(string playerSecret)
    {
        CheckGameInProcess();

        var tablePlayer = Players.Single(x => x.AuthSecret == playerSecret);

        if (StopRoundBeginDate == null || StopRoundStatus != SRS.SuccessDefence)
        {
            throw new BusinessException("Сейчас нельзя закрыть раунд");
        }

        if (Game.DefencePlayer == tablePlayer.Player)
        {
            throw new BusinessException("Защищающийся не закрывает раунд");
        }

        tablePlayer.SaidBeat = true;
        tablePlayer.Reply = ReplyPhrases[Random.Shared.Next(ReplyPhrases.Length)];
        tablePlayer.ReplyDate = DateTime.UtcNow;
        WriteLog(playerSecret, "beat");

        if (AllAttackersSaidBeat())
        {
            StopRoundStatus = null;
            StopRoundBeginDate = null;
            Game.StopRound();
            SetActivePlayerAfkStartTime();
        }

        Version++;
    }

    /// <summary>
    /// Все атакующие (не защищающийся, с картами на руках) объявили «Бито». Боты тоже голосуют:
    /// фоновый драйвер говорит «Бито» за бота, которому больше нечего подкинуть (TableHolder.CheckBotBeats).
    /// </summary>
    private bool AllAttackersSaidBeat()
    {
        foreach (var tablePlayer in Players)
        {
            if (tablePlayer.Player == Game.DefencePlayer || tablePlayer.Player.Hand.Cards.Count == 0)
            {
                continue;
            }

            if (tablePlayer.SaidBeat == false)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Сбросить голоса «Бито» — на каждом новом окне остановки раунда и новой партии.</summary>
    private void ClearBeatVotes()
    {
        foreach (var tablePlayer in Players)
        {
            tablePlayer.SaidBeat = false;
        }
    }

    /// <summary>
    /// Сменить режим сортировки руки игрока (по его секрету) и уведомить клиентов (issue F3).
    /// </summary>
    /// <remarks>
    /// Пересортировка меняет порядок (и индексы) карт, поэтому обязателен <see cref="Version" />++ —
    /// иначе очередной клик игрока разрешится против устаревшего порядка и сыграет не ту карту.
    /// </remarks>
    public void SetSortMode(string playerSecret, HandSortMode mode)
    {
        var tablePlayer = Players.Single(player => player.AuthSecret == playerSecret);
        tablePlayer.Player.Hand.SetSortMode(mode);
        Version++;
    }

    private static StringBuilder GetCardsLog(int[] cardIndexes, TablePlayer tablePlayer)
    {
        StringBuilder logCards = new();
        var cards = tablePlayer.Player.Hand.Cards;

        foreach (var cardIndex in cardIndexes)
        {
            if (cardIndex >= 0 && cardIndex < cards.Count)
            {
                var card = cards[cardIndex];
                logCards.Append(' ').Append(card);
            }
            else
            {
                throw new BusinessException($"Ошибка при получении логов карт: индекс {cardIndex} вне диапазона.");
            }
        }

        return logCards;
    }

    private void StartAttack(TablePlayer tablePlayer, int[] cardIndexes)
    {
        var logCards = GetCardsLog(cardIndexes, tablePlayer);

        tablePlayer.Player.Hand.StartAttack(cardIndexes);
        WriteLog(tablePlayer.AuthSecret, $"start attack{logCards}");

        SetDefencePlayerAfkStartTime();
    }

    private void Attack(TablePlayer tablePlayer, int[] cardIndexes)
    {
        var logCards = GetCardsLog(cardIndexes, tablePlayer);

        tablePlayer.Player.Hand.Attack(cardIndexes);
        WriteLog(tablePlayer.AuthSecret, $"attack{logCards}");

        if (StopRoundStatus == null)
        {
            return;
        }

        switch (StopRoundStatus)
        {
            case SRS.SuccessDefence:
                SetDefencePlayerAfkStartTime();
                StopRoundBeginDate = null;
                StopRoundStatus = null;
                ClearBeatVotes();
                break;

            case SRS.Take:
                break;

            default:
                throw new BusinessException($"Неопределённый статус остановки раунда: {StopRoundStatus}");
        }
    }

    private void Defence(TablePlayer tablePlayer, int defenceCardIndex, int attackCardIndex)
    {
        if (StopRoundBeginDate != null)
        {
            throw new BusinessException("Раунд в процессе остановки");
        }

        StringBuilder logCards = new();

        try
        {
            var defenceCard = tablePlayer.Player.Hand.Cards[defenceCardIndex];
            var tableCard = Game.Cards[attackCardIndex].AttackCard;
            logCards.Append($" {defenceCard}->{tableCard}");
        }
        catch (Exception exception)
        {
            throw new BusinessException($"Ошибка при попытке записи логов защиты: {exception.Message}");
        }

        tablePlayer.Player.Hand.Defence(defenceCardIndex, attackCardIndex);
        WriteLog(tablePlayer.AuthSecret, $"defence{logCards}");

        SetDefencePlayerAfkStartTime();

        if (Game.Cards.Any(tableCard => tableCard.DefenceCard == null))
        {
            return;
        }

        CheckStopRoundBeginDate();
        StopRoundStatus = SRS.SuccessDefence;
        CleanDefencePlayerAfkStartTime();
    }

    private void CheckGameInProcess()
    {
        if (Game.Status != GameStatus.InProcess)
        {
            throw new BusinessException("Игра не в процессе");
        }
    }

    private void CheckStopRoundBeginDate()
    {
        if (StopRoundBeginDate != null)
        {
            throw new BusinessException("Идёт остановка раунда");
        }

        StopRoundBeginDate = DateTime.UtcNow;

        ClearBeatVotes();
    }

    private void CheckEndGame()
    {
        if (Game.Status != GameStatus.InProcess)
        {
            CleanAllAfkTime();
            StopRoundStatus = null;
            StopRoundBeginDate = null;
            WriteLog("", "game finish " + Game.Status);

            if (Game.LooserPlayer != null)
            {
                WriteLog("", "looser: " + Game.LooserPlayer.Name);
            }
        }
    }

    private void WriteLog(string? playerSecret, string message)
    {
        var tablePlayer = Players.SingleOrDefault(x => x.AuthSecret == playerSecret);
        var playerIndex = tablePlayer == null ? null : (int?)Game.Players.IndexOf(tablePlayer.Player);

        var logger = LogManager.GetCurrentClassLogger()
            .WithProperty("TableId", Number + " " + Id)
            .WithProperty("PlayerId", playerSecret)
            .WithProperty("PlayerIndex", playerIndex);

        logger.Info(message);
    }
}
