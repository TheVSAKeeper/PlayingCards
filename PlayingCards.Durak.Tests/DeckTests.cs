namespace PlayingCards.Durak.Tests;

public class DeckTests
{
    /// <summary>
    ///     Перетасовали колоду и убедились что в ней 9 номиналов карт и 4 масти, и карт 36 штук.
    /// </summary>
    [Test]
    public void DeckCardsCountTest()
    {
        Deck deck = new(new RandomDeckCardGenerator());
        deck.Shuffle();
        List<Card> cards = [];

        while (deck.CardsCount > 0)
        {
            Card card = deck.PullCard();
            cards.Add(card);
        }

        Assert.Multiple(() =>
        {
            Assert.That(cards, Has.Count.EqualTo(36));
            Assert.That(cards.GroupBy(card => card.Rank).Count(), Is.EqualTo(9));
            Assert.That(cards.GroupBy(card => card.Suit).Count(), Is.EqualTo(4));
        });
    }

    /// <summary>
    ///     Раздали игрокам по 6 карт в начале игры.
    /// </summary>
    /// <param name="playerCount">Количество игроков.</param>
    [Test]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    public void TwoPlayersStartGameTest(int playerCount)
    {
        Game game = new();

        for (int i = 0; i < playerCount; i++)
        {
            game.AddPlayer($"player{i}");
        }

        game.StartGame();

        Assert.That(game.Players, Has.Count.EqualTo(playerCount));

        foreach (Player player in game.Players)
        {
            Assert.That(player.Hand.Cards, Has.Count.EqualTo(6));
        }
    }

    /// <summary>
    ///     Проверка, кто первый ходит.
    /// </summary>
    /// <remarks>
    ///     У первого ходящего должен быть козырь наименьшего номинала на руке, чем у других.
    /// </remarks>
    /// <param name="playerCount">Количество игроков в игре.</param>
    [Test]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    public void ActivePlayerTest(int playerCount)
    {
        Game game = new();

        for (int i = 0; i < playerCount; i++)
        {
            game.AddPlayer($"Player{i}");
        }

        game.StartGame();
        Assert.That(game.ActivePlayer, Is.Not.Null);

        Card? activePlayerMinSuitCard = game.ActivePlayer.Hand.GetMinSuitCard(game.Deck.TrumpCard.Suit);
        Assert.That(activePlayerMinSuitCard, Is.Not.Null);

        foreach (int cardRank in game.Players
                     .Where(player => player.Name != game.ActivePlayer.Name)
                     .Select(player => player.Hand.GetMinSuitCard(game.Deck.TrumpCard.Suit))
                     .Select(card => card?.Rank ?? int.MaxValue))
        {
            Assert.That(cardRank, Is.GreaterThanOrEqualTo(activePlayerMinSuitCard.Rank.Value));
        }
    }

    /// <summary>
    ///     Проверка, на кого, первым ходят.
    /// </summary>
    /// <remarks>
    ///     Следующий после активного, защищается.
    /// </remarks>
    /// <param name="playerCount">Количество игроков в игре.</param>
    [Test]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    public void DefencePlayerTest(int playerCount)
    {
        const string PlayerPrefix = "Player";

        Game game = new();

        for (int i = 0; i < playerCount; i++)
        {
            game.AddPlayer(PlayerPrefix + i);
        }

        game.StartGame();

        Assert.Multiple(() =>
        {
            Assert.That(game.ActivePlayer, Is.Not.Null);
            Assert.That(game.DefencePlayer, Is.Not.Null);
        });

        string activePlayerNumber = game.ActivePlayer.Name[PlayerPrefix.Length..];
        int defencePlayerNumber = int.Parse(activePlayerNumber) + 1;

        if (defencePlayerNumber >= playerCount)
        {
            defencePlayerNumber = 0;
        }

        string defencePlayerName = $"{PlayerPrefix}{defencePlayerNumber}";

        Assert.That(game.DefencePlayer.Name, Is.EqualTo(defencePlayerName));
    }

    /// <summary>
    ///     Отбиваемся от карты.
    /// </summary>
    [Test]
    [TestCase(0, 1, 8, true, TestName = "Бьём козырную шестёрку, козырной семёркой")]
    [TestCase(2, 1, 8, false, TestName = "Бьём козырную восьмёрку, козырной семёркой")]
    [TestCase(0, 10, 8, false, TestName = "Бьём козырную шестёрку, некозырной семёркой")]
    [TestCase(0, 1, 9, true, TestName = "Бьём некозырную шестёрку, некозырной семёркой той же масти")]
    [TestCase(2, 1, 9, false, TestName = "Бьём некозырную восьмёрку, некозырной семёркой той же масти")]
    [TestCase(9, 1, 8, true, TestName = "Бьём некозырную шестёрку, козырной семёркой")]
    [TestCase(9, 1, 34, false, TestName = "Бьём некозырную шестёрку, некозырной семёркой другой масти")]
    public void SuccessDefenceTrumpSuitTest(
        int attackCardIndex,
        int defenceCardIndex,
        int trumpCardIndex,
        bool isSuccess)
    {
        Card[] cards = CardsHolder.Cards.ToArray();
        Card attackCard = cards[attackCardIndex];
        Card defenceCard = cards[defenceCardIndex];
        Card trumpCard = cards[trumpCardIndex];

        Game game = new()
        {
            Deck = new Deck(new EmptyDeckCardGenerator())
            {
                TrumpCard = trumpCard
            }
        };

        TableCard attackTableCard = new(game, attackCard);

        if (isSuccess)
        {
            attackTableCard.Defence(defenceCard);
        }
        else
        {
            Assert.Throws<BusinessException>(() => attackTableCard.Defence(defenceCard));
        }
    }

    /// <summary>
    ///     Сходили одну карту и отбили её.
    /// </summary>
    /// <remarks>
    ///     Проверили, что после первого раунда, все добрали карты на руки до 6.
    ///     Проверили, что в колоде стало на 2 карты меньше.
    /// </remarks>
    [Test]
    public void PlayOneRoundOneCardDefenceTest()
    {
        string[] playerCards =
        [
            "A♠ Q♦ Q♣ Q♥ 10♥ 7♦",
            "Q♠ A♦ A♣ A♥ 10♣ 10♠"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");

        Player player1 = game.Players[0];
        Player player2 = game.Players[1];

        game.StartGame();

        // ходим
        player1.Hand.StartAttack("Q♥");
        // отбиваемся
        player2.Hand.Defence("A♥->Q♥");

        game.StopRound();

        Assert.Multiple(() =>
        {
            Assert.That(player1.Hand.Cards, Has.Count.EqualTo(6));
            Assert.That(player2.Hand.Cards, Has.Count.EqualTo(6));
            Assert.That(game.Deck.CardsCount, Is.EqualTo(36 - 6 - 6 - 2));
        });
    }

    /// <summary>
    ///     Сходили две карты, одну отбили, а вторую не отбили, и забираем всё на руки.
    /// </summary>
    /// <remarks>
    ///     Проверили, что атакующий добрал карты до 6.
    ///     Проверили, что защищающийся забрал себе обе карты атакующего, и у него теперь их 8.
    ///     Проверили, что в колоде стало на 2 карты меньше.
    /// </remarks>
    [Test]
    public void PlayOneRoundTwoCardAndNotDefenceTest()
    {
        string[] playerCards =
        [
            "A♠ Q♦ Q♣ Q♥ 10♥ 7♦",
            "Q♠ A♦ A♣ A♥ 10♣ 10♠"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");

        Player player1 = game.Players[0];
        Player player2 = game.Players[1];

        game.StartGame();

        // ходим
        player1.Hand.StartAttack("Q♦");
        // подкинули
        player1.Hand.Attack("Q♣");
        // отбиваемся
        player2.Hand.Defence("A♣->Q♣");

        // решаем не отбиваться, забираем на руки
        game.StopRound();

        Assert.Multiple(() =>
        {
            Assert.That(player1.Hand.Cards, Has.Count.EqualTo(6));
            Assert.That(player2.Hand.Cards, Has.Count.EqualTo(8));
            Assert.That(game.Deck.CardsCount, Is.EqualTo(36 - 6 - 6 - 2));
        });
    }

    /// <summary>
    ///     Игрок 1 начинает раунд, игрок 3 подкидывает, игрок 2 отбивает все карты.
    /// </summary>
    /// <remarks>
    ///     Проверили, что можно подкинуть карту.
    /// </remarks>
    [Test]
    public void StartAttackAndAttackCardTest()
    {
        string[] playerCards =
        [
            "A♠ 10♠ 6♠ J♥ 7♥ Q♦",
            "K♠ 9♠ A♥ 10♥ 6♥ J♦",
            "Q♠ 8♠ K♥ 9♥ A♦ 10♦",
            "J♠ 7♠ Q♥ 8♥ K♦ 9♦"
        ];

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");
        game.AddPlayer("3");
        game.AddPlayer("4");

        Player startAttackPlayer = game.Players[0];
        Player defencePlayer = game.Players[1];
        Player attackPlayer = game.Players[2];

        game.StartGame();

        // ходим
        startAttackPlayer.Hand.StartAttack("10♠");

        // подкидываем
        attackPlayer.Hand.Attack("10♦");

        // отбиваемся
        defencePlayer.Hand.Defence("K♠->10♠");

        // отбиваемся
        defencePlayer.Hand.Defence("J♦->10♦");

        game.StopRound();

        Assert.Multiple(() =>
        {
            Assert.That(startAttackPlayer.Hand.Cards, Has.Count.EqualTo(6));
            Assert.That(defencePlayer.Hand.Cards, Has.Count.EqualTo(6));
            Assert.That(attackPlayer.Hand.Cards, Has.Count.EqualTo(6));
            Assert.That(game.Deck.CardsCount, Is.EqualTo(36 - 6 * 4 - 2 * 2));
        });
    }

    /// <summary>
    ///     Игрок кидает 4 одинаковые карты, а защитник забирает их себе.
    /// </summary>
    /// <remarks>
    ///     Проверили, что можно начать раунд с нескольких карт.
    /// </remarks>
    [Test]
    public void StartAttackManyCardsAndNotDefenceTest()
    {
        string[] playerCards =
        [
            "A♠ A♦ A♣ A♥ 10♥ 7♦",
            "Q♠ Q♦ Q♣ Q♥ 10♣ 10♠"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");

        Player startAttackPlayer = game.Players[0];
        Player defencePlayer = game.Players[1];

        game.StartGame();

        // ходим
        startAttackPlayer.Hand.StartAttack("A♠ A♦ A♣ A♥");

        game.StopRound();

        Assert.Multiple(() =>
        {
            Assert.That(startAttackPlayer.Hand.Cards, Has.Count.EqualTo(6));
            Assert.That(defencePlayer.Hand.Cards, Has.Count.EqualTo(6 + 4));
            Assert.That(game.Deck.CardsCount, Is.EqualTo(36 - 6 * 2 - 4));
        });
    }

    /// <summary>
    ///     Игрок кидает одну карту, второй подкидывает ещё 3, а защитник забирает их себе.
    /// </summary>
    /// <remarks>
    ///     Проверили, что можно поддать несколько карт.
    /// </remarks>
    [Test]
    public void AttackManyCardsAndNotDefenceTest()
    {
        string[] playerCards =
        [
            "A♠ Q♦ Q♣ Q♥ 10♥ 7♦",
            "J♠ J♦ J♣ J♥ 9♣ 9♠",
            "Q♠ A♦ A♣ A♥ 10♣ 10♠"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");
        game.AddPlayer("3");

        Player startAttackPlayer = game.Players[0];
        Player defencePlayer = game.Players[1];
        Player attackPlayer = game.Players[2];

        game.StartGame();

        // ходим
        startAttackPlayer.Hand.StartAttack("A♠");
        // подкидываем
        attackPlayer.Hand.Attack("A♦ A♣ A♥");

        game.StopRound();

        Assert.Multiple(() =>
        {
            Assert.That(startAttackPlayer.Hand.Cards, Has.Count.EqualTo(6));
            Assert.That(attackPlayer.Hand.Cards, Has.Count.EqualTo(6));
            Assert.That(defencePlayer.Hand.Cards, Has.Count.EqualTo(6 + 4));
            Assert.That(game.Deck.CardsCount, Is.EqualTo(36 - 6 * 3 - 4));
        });
    }

    /// <summary>
    ///     Игрок кидает одну карту, потом подкидывает одну карту доступного ранга и вторую недоступного ранга.
    /// </summary>
    /// <remarks>
    ///     Проверили, что нельзя поддать карту доступного ранга и недоступного ранга.
    /// </remarks>
    [Test]
    public void AttackTwoDifferentCardSequence()
    {
        string[] playerCards =
        [
            "J♠ J♦ J♣ J♥ 10♥ 7♦",
            "A♠ Q♦ Q♣ Q♥ 9♣ 9♠"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");
        Player startAttackPlayer = game.Players[0];

        game.StartGame();

        // ходим
        startAttackPlayer.Hand.StartAttack("J♠");
        // подкидываем
        startAttackPlayer.Hand.Attack("J♦");

        // подкидываем
        Assert.Throws<BusinessException>(() => startAttackPlayer.Hand.Attack("7♦"));
    }

    /// <summary>
    ///     Игрок кидает одну карту, потом подкидывает одновременно одну карту доступного ранга и вторую недоступного ранга.
    /// </summary>
    /// <remarks>
    ///     Проверили, что нельзя поддать одновременно карту доступного ранга и недоступного ранга.
    /// </remarks>
    [Test]
    public void AttackTwoDifferentCard()
    {
        string[] playerCards =
        [
            "J♠ J♦ J♣ J♥ 10♥ 7♦",
            "A♠ Q♦ Q♣ Q♥ 9♣ 9♠"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");

        Player startAttackPlayer = game.Players[0];

        game.StartGame();

        // ходим
        startAttackPlayer.Hand.StartAttack("J♠");

        // подкидываем
        Assert.Throws<BusinessException>(() => startAttackPlayer.Hand.Attack("J♦ 7♦"));
    }

    /// <summary>
    ///     Подкинем карту, ранг которой, равен рангу защитной карты.
    /// </summary>
    /// <remarks>
    ///     Была ошибка, если вольта отбить дамой, то даму нельзя было поддать.
    /// </remarks>
    [Test]
    public void AttackCardsWithDefencedCardRankTest()
    {
        string[] playerCards =
        [
            "A♠ Q♦ J♣ J♥ 10♥ 7♦",
            "J♠ J♦ Q♣ Q♥ 9♣ 9♠"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");

        Player attackPlayer = game.Players[0];
        Player defencePlayer = game.Players[1];

        game.StartGame();

        // ходим
        attackPlayer.Hand.StartAttack("J♣");
        // отбиваем
        defencePlayer.Hand.Defence("Q♣->J♣");
        // подкидываем
        attackPlayer.Hand.Attack("Q♦");

        game.StopRound();

        Assert.Multiple(() =>
        {
            Assert.That(attackPlayer.Hand.Cards, Has.Count.EqualTo(6));
            Assert.That(defencePlayer.Hand.Cards, Has.Count.EqualTo(6 + 2));
            Assert.That(game.Deck.CardsCount, Is.EqualTo(36 - 6 * 2 - 2));
        });
    }

    /// <summary>
    ///     Проверка, что в игре на двоих, после того, как игрок забрал карты ход остаётся у атакующего.
    /// </summary>
    [Test]
    public void CorrectChangeActivePlayerTest()
    {
        string[] playerCards =
        [
            "A♠ Q♦ Q♣ Q♥ 10♥ 7♦",
            "Q♠ A♦ A♣ A♥ 10♣ 10♠"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");

        Player attackPlayer = game.Players[0];

        game.StartGame();

        // ходим
        attackPlayer.Hand.StartAttack("Q♣");
        game.StopRound();

        Assert.That(game.ActivePlayer!.Name, Is.EqualTo(attackPlayer.Name));
    }

    /// <summary>
    ///     Проверка, что в игре на двоих, после отбивания ход переходит второму игроку.
    /// </summary>
    /// <remarks>
    ///     Если один из игроков, не берёт карту из колоды, то активный игрок неверно определялся.
    /// </remarks>
    [Test]
    public void CorrectChangeActiveAfterDefencePlayerTest()
    {
        string[] playerCards =
        [
            "A♠ Q♦ Q♣ Q♥ 10♥ 7♦",
            "Q♠ A♦ A♣ A♥ 10♣ 10♠"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");

        Player attackPlayer = game.Players[0];
        Player defencePlayer = game.Players[1];

        game.StartGame();

        // ходим
        attackPlayer.Hand.StartAttack("Q♣");

        game.StopRound();

        // ходим
        attackPlayer.Hand.StartAttack("Q♦");
        // отбиваемся
        defencePlayer.Hand.Defence("A♦->Q♦");

        game.StopRound();

        Assert.That(game.ActivePlayer!.Name, Is.EqualTo(defencePlayer.Name));
    }

    /// <summary>
    ///     Проверка, если игрок без карт, то его не учитываем в выборе активного игрока.
    /// </summary>
    [Test]
    public void CorrectChangeActiveZeroCardsPlayerTest()
    {
        string[] playerCards =
        [
            "J♠ Q♦ Q♣ Q♥ 10♥ 7♦",
            "Q♠ 9♦ 9♥ 9♠ 9♣ 8♦",
            "A♠ A♦ A♣ A♥ 10♣ 10♠"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");
        game.AddPlayer("3");

        Player attackPlayer = game.Players[0];
        Player defencePlayer = game.Players[1];
        Player expectedStartAttackPlayer = game.Players[2];

        game.StartGame();

        // удалим у отбивающегося все карты кроме одной
        defencePlayer.Hand.RemoveRange(0, 5);
        // очистим колоду
        game.Deck.Cards.Clear();

        // ходим
        attackPlayer.Hand.StartAttack("J♠");
        // отбиваем
        defencePlayer.Hand.Defence("Q♠->J♠");

        game.StopRound();

        Assert.That(game.ActivePlayer!.Name, Is.EqualTo(expectedStartAttackPlayer.Name));
    }

    /// <summary>
    ///     Проверка, если игрок без карт, то его не учитываем в выборе защищающегося игрока.
    /// </summary>
    [Test]
    public void CorrectChangeDefenceZeroCardsPlayerTest()
    {
        string[] playerCards =
        [
            "J♠ Q♦ Q♣ Q♥ 10♥ 7♦",
            "Q♠ 9♦ 9♥ 9♠ 9♣ 8♦",
            "A♠ A♦ A♣ A♥ 10♣ 10♠"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");
        game.AddPlayer("3");

        Player player1 = game.Players[0];
        Player player2 = game.Players[1];
        Player zeroCardsPlayer = game.Players[2];

        game.StartGame();

        // удалим все карты у игрока
        zeroCardsPlayer.Hand.Clear();
        // очистим колоду
        game.Deck.Cards.Clear();

        // ходим
        player1.Hand.StartAttack("J♠");
        // отбиваем
        player2.Hand.Defence("Q♠->J♠");

        game.StopRound();

        Assert.Multiple(() =>
        {
            Assert.That(game.DefencePlayer!.Name, Is.EqualTo(player1.Name));
            Assert.That(game.ActivePlayer!.Name, Is.EqualTo(player2.Name));
        });
    }

    /// <summary>
    ///     Проверка, что в игре на четверых, когда осталось двое, после забирания, ход остаётся у того кто ходил.
    /// </summary>
    /// <remarks>
    ///     Была бага, что игрок после того как забрал карты, начинал ходить.
    /// </remarks>
    [Test]
    public void CorrectChangeActiveAfterFailDefencePlayerTest()
    {
        string[] playerCards =
        [
            "Q♠ Q♦ Q♣ Q♥ 10♥ 7♦",
            "A♠ A♦ A♣ A♥ 10♣ J♦",
            "9♠ 9♦ 9♣ 9♥ J♣ J♠",
            "8♠ 8♦ 8♣ 8♥ K♣ K♠"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        Player player1 = game.AddPlayer("1");
        Player player2 = game.AddPlayer("2");
        Player player3 = game.AddPlayer("3");
        Player player4 = game.AddPlayer("4");

        game.StartGame();

        player1.Hand.RemoveRange(1, 5);
        player2.Hand.RemoveRange(0, 1);
        player2.Hand.RemoveRange(1, 4);
        game.Deck.Cards.Clear();

        // ходим
        player1.Hand.StartAttack("7♦");
        // отбиваем
        player2.Hand.Defence("J♦->7♦");

        // 1 и 2 игрок вышли из игры
        game.StopRound();

        // ходим
        player3.Hand.StartAttack("J♣");
        // отбиваемся
        player4.Hand.Defence("K♣->J♣");
        game.StopRound();

        // ходим
        player4.Hand.StartAttack("8♠");

        // player3 забирает
        game.StopRound();

        Assert.Multiple(() =>
        {
            Assert.That(game.ActivePlayer!.Name, Is.EqualTo(player4.Name));
            Assert.That(game.DefencePlayer!.Name, Is.EqualTo(player3.Name));
        });
    }

    /// <summary>
    ///     Ошибка, если сходить количеством карт, больше, чем у защищающегося.
    /// </summary>
    [Test]
    public void StartAttackOverflowTest()
    {
        string[] playerCards =
        [
            "A♠ Q♦ J♣ J♥ 10♥ 9♠",
            "J♠ J♦ Q♣ Q♥ 9♣ 10♠" // 10♠ будет козырем
        ];

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, null, 24))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");

        Player attackPlayer = game.Players[0];
        Player defencePlayer = game.Players[1];

        game.StartGame();

        // удалим из руки все карты, кроме одной
        defencePlayer.Hand.RemoveRange(1, 5);

        // ходим
        Assert.Throws<BusinessException>(() => attackPlayer.Hand.StartAttack("J♣ J♥"));
    }

    /// <summary>
    ///     Ошибка, если поддаваемое количеством карт плюс карт на столе, больше, чем у защищающегося.
    /// </summary>
    [Test]
    public void AttackOverflowTest()
    {
        string[] playerCards =
        [
            "J♠ J♦ J♣ J♥ 10♥ 9♠",
            "A♠ Q♦ Q♣ Q♥ 9♣ 10♠" // 10♠ будет козырем
        ];

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, null, 24))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");

        Player attackPlayer = game.Players[0];
        Player defencePlayer = game.Players[1];

        game.StartGame();

        // оставим в руке 3 карты
        defencePlayer.Hand.RemoveRange(3, 3);

        // ходим
        attackPlayer.Hand.StartAttack("J♠ J♦");
        // поддаём
        Assert.Throws<BusinessException>(() => attackPlayer.Hand.StartAttack("J♣ J♥"));
    }

    /// <summary>
    ///     Проверка, что нельзя до первого отбоя атаковать больше 5 картами.
    /// </summary>
    [Test]
    public void FirstDefenceMaxFiveAttackCardsTest()
    {
        string[] playerCards =
        [
            "J♠ J♦ J♣ J♥ Q♣ Q♦",
            "A♠ 10♥ 9♠ Q♥ 9♣ 10♠"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");

        Player attackPlayer = game.Players[0];
        Player defencePlayer = game.Players[1];

        game.StartGame();

        attackPlayer.Hand.StartAttack("J♠ J♦ J♣ J♥");
        defencePlayer.Hand.Defence("Q♥->J♥");

        Assert.Throws<BusinessException>(() => attackPlayer.Hand.Attack("Q♣ Q♦"));
    }

    /// <summary>
    ///     Проверка, что номинал козырной карты, которую надо всем показать, корректный.
    /// </summary>
    [Test]
    [TestCase("J♠ A♣ J♣ J♥ Q♣ 7♦", 7)]
    [TestCase("J♠ A♣ J♣ J♥ 8♦ 7♦", 7)]
    [TestCase("J♠ A♣ J♣ J♥ 7♦ 8♦", 7)]
    [TestCase("J♠ A♣ J♣ J♥ Q♣ 7♥", null)]
    public void NeedShowCardMinTrumpValueTest(string firstPlayerCards, int? expectedMinTrumpValue)
    {
        string[] playerCards =
        [
            firstPlayerCards,
            "A♠ 10♥ 9♠ Q♥ 9♣ 10♠"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        game.AddPlayer("1");
        game.AddPlayer("2");

        game.StartGame();

        Assert.That(game.NeedShowCardMinTrumpValue, Is.EqualTo(expectedMinTrumpValue));
    }

    /// <summary>
    ///     Проверка, что определяется проигравший.
    /// </summary>
    [Test]
    public void CheckLooserTest()
    {
        Game game = new()
        {
            Deck = new Deck(new NotSortedDeckCardGenerator())
        };

        game.AddPlayer("1");
        game.AddPlayer("2");
        game.AddPlayer("3");
        game.AddPlayer("4");
        game.AddPlayer("5");
        game.AddPlayer("6");

        game.StartGame();

        int activePlayerIndex = game.Players.IndexOf(game.ActivePlayer!);
        int defencePlayerIndex = game.Players.IndexOf(game.DefencePlayer!);

        for (int i = 0; i < 6; i++)
        {
            Player player = game.Players[i];

            if (i == defencePlayerIndex)
            {
                continue;
            }

            if (i == activePlayerIndex)
            {
                player.Hand.RemoveRange(1, player.Hand.Cards.Count - 1);
            }
            else
            {
                player.Hand.Clear();
            }
        }

        game.Players[activePlayerIndex].Hand.StartAttack([0]);

        Assert.Multiple(() =>
        {
            Assert.That(game.Status, Is.EqualTo(GameStatus.Finish));
            Assert.That(game.LooserPlayer, Is.Not.Null);
        });

        Assert.That(game.LooserPlayer.Name, Is.EqualTo(game.Players[defencePlayerIndex].Name));
    }

    /// <summary>
    ///     Проверка, что игра кончилась, если отбился последней картой.
    /// </summary>
    [Test]
    public void CheckLooserIfDefenceLastCardTest()
    {
        string[] playerCards =
        [
            "J♥ J♠ J♦ J♣ Q♣ Q♦",
            "10♥ A♠ 9♠ Q♥ 9♣ 7♦"
        ];

        string trumpValue = "6♦";

        Game game = new()
        {
            Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue))
        };

        Player winnerPlayer = game.AddPlayer("1");
        Player looserPlayer = game.AddPlayer("2");

        game.StartGame();

        winnerPlayer.Hand.RemoveRange(0, 1);
        winnerPlayer.Hand.RemoveRange(1, 4);
        game.Deck.Cards.Clear();

        looserPlayer.Hand.StartAttack("10♥");
        winnerPlayer.Hand.Defence("J♥->10♥");

        Assert.Multiple(() =>
        {
            Assert.That(game.Status, Is.EqualTo(GameStatus.Finish));
            Assert.That(game.LooserPlayer, Is.Not.Null);
        });

        Assert.That(game.LooserPlayer.Name, Is.EqualTo(looserPlayer.Name));
    }
}