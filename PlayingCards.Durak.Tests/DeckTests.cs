namespace PlayingCards.Durak.Tests
{
    public class DeckTests
    {
        /// <summary>
        /// Перетусовали колоду и убедились что в ней 9 номиналов карт и 4 масти, и карт 36 штук.
        /// </summary>
        [Test]
        public void DeckCardsCountTest()
        {
            var deck = new Deck(new RandomDeckCardGenerator());
            deck.Shuffle();
            var cards = new List<Card>();
            while (deck.CardsCount > 0)
            {
                var card = deck.PullCard();
                cards.Add(card);
            }
            Assert.That(cards.Count, Is.EqualTo(36));
            Assert.That(cards.GroupBy(x => x.Rank.Value).Count(), Is.EqualTo(9));
            Assert.That(cards.GroupBy(x => x.Suit.Value).Count(), Is.EqualTo(4));
        }

        /// <summary>
        /// Раздали игрокам по 6 карт в начале игры.
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
            var game = new Game();
            for (var i = 0; i < playerCount; i++)
            {
                game.AddPlayer("player" + i);
            }
            game.InitCardDeck();

            foreach (var player in game.Players)
            {
                Assert.That(player.Hand.Cards.Count(), Is.EqualTo(6));
            }
        }

        /// <summary>
        /// Проверка, кто первый ходит.
        /// </summary>
        /// <remarks>
        /// У первого ходящего должен быть козырь наименьшего номинала на руке, чем у других.
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
            var game = new Game();
            for (var i = 0; i < playerCount; i++)
            {
                game.AddPlayer("Player" + i);
            }
            game.InitCardDeck();

            Assert.IsNotNull(game.ActivePlayer);
            var activePlayerMinSuitCard = game.ActivePlayer.Hand.GetMinSuitCard(game.Deck.TrumpCard.Suit);
            Assert.IsNotNull(activePlayerMinSuitCard);

            foreach (var player in game.Players)
            {
                if (player.Name != game.ActivePlayer.Name)
                {
                    var card = player.Hand.GetMinSuitCard(game.Deck.TrumpCard.Suit);
                    var cardRank = card?.Rank.Value ?? Int32.MaxValue;
                    Assert.GreaterOrEqual(cardRank, activePlayerMinSuitCard.Rank.Value);
                }
            }
        }

        /// <summary>
        /// Проверка, на кого, первым ходят.
        /// </summary>
        /// <remarks>
        /// Следующий после активного, защищается.
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
            var game = new Game();
            for (var i = 0; i < playerCount; i++)
            {
                game.AddPlayer("Player" + i);
            }
            game.InitCardDeck();

            var activePlayerNumber = game.ActivePlayer.Name.Substring("Player".Length);
            var defencePlayerNumber = Convert.ToInt32(activePlayerNumber) + 1;
            if (defencePlayerNumber >= playerCount)
            {
                defencePlayerNumber = 0;
            }
            var defencePlayerName = "Player" + defencePlayerNumber;
            Assert.IsNotNull(game.DefencePlayer);
            Assert.That(game.DefencePlayer.Name, Is.EqualTo(defencePlayerName));
        }

        /// <summary>
        /// Отбиваемся от карты.
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
            int trumCardIndex,
            bool isSuccess)
        {
            var attackCard = CardsHolder.GetCards()[attackCardIndex];
            var defenceCard = CardsHolder.GetCards()[defenceCardIndex];
            var trumpCard = CardsHolder.GetCards()[trumCardIndex];
            var game = new Game();
            game.Deck = new Deck(new EmptyDeckCardGenerator()) { TrumpCard = trumpCard };
            var attackTableCard = new TableCard(game, attackCard);
            if (isSuccess)
            {
                attackTableCard.Defence(defenceCard);
            }
            else
            {
                Assert.Throws<Exception>(() => attackTableCard.Defence(defenceCard));
            }
        }

        /// <summary>
        /// Сходили одну карту и отбили её.
        /// </summary>
        /// <remarks>
        /// Проверили, что после первого раунда, все добрали карты на руки до 6.
        /// Проверили, что в колоде стало на 2 карты меньше.
        /// </remarks>
        [Test]
        public void PlayOneRoundOneCardDefenceTest()
        {
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            var player1 = game.Players[0];
            var player2 = game.Players[1];
            game.Deck = new Deck(new NotSortedDeckCardGenerator());
            game.InitCardDeck();
            // ходим пиковой дамой
            player1.Hand.StartAttack([1]);
            // отбиваемся пиковым королём
            player2.Hand.Defence(0, 0);
            game.StopRound();
            Assert.That(player1.Hand.Cards.Count, Is.EqualTo(6));
            Assert.That(player2.Hand.Cards.Count, Is.EqualTo(6));
            Assert.That(game.Deck.CardsCount, Is.EqualTo(36 - 6 - 6 - 2));
        }

        /// <summary>
        /// Сходили две карты, одну отбили, а вторую не отбили, и забираем всё на руки.
        /// </summary>
        /// <remarks>
        /// Проверили, что атакующий добрал карты до 6.
        /// Проверили, что защищающийся забрал себе обе карты атакующиего, и у него теперь их 8.
        /// Проверили, что в колоде стало на 2 карты меньше.
        /// </remarks>
        [Test]
        public void PlayOneRoundTwoCardAndNotDefenceTest()
        {
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            var player1 = game.Players[0];
            var player2 = game.Players[1];
            game.Deck = new Deck(new NotSortedDeckCardGenerator());
            game.InitCardDeck();
            // ходим пиковым тузом
            player1.Hand.StartAttack([0]);
            // ходим пиковой дамой
            player1.Hand.StartAttack([0]);
            // отбиваемся пиковым королём от дамы
            player2.Hand.Defence(0, 1);
            // а туза мы отбить не можем, забираем на руки
            game.StopRound();
            Assert.That(player1.Hand.Cards.Count, Is.EqualTo(6));
            Assert.That(player2.Hand.Cards.Count, Is.EqualTo(8));
            Assert.That(game.Deck.CardsCount, Is.EqualTo(36 - 6 - 6 - 2));
        }

        /// <summary>
        /// Игрок 1 начинает раунд, игрок 3 подкидывает, игрок 2 отбивает все карты.
        /// </summary>
        /// <remarks>
        /// Проверили, что можно подкинуть карту.
        /// </remarks>
        [Test]
        public void StartAttackAndAttackCardTest()
        {
            var game = new Game();
            game.AddPlayer("1"); //A♠ 10♠ 6♠ J♥ 7♥ Q♦
            game.AddPlayer("2"); //K♠ 9♠ A♥ 10♥ 6♥ J♦
            game.AddPlayer("3"); //Q♠ 8♠ K♥ 9♥ A♦ 10♦
            game.AddPlayer("4"); //J♠ 7♠ Q♥ 8♥ K♦ 9♦
            var startAttackPlayer = game.Players[0];
            var defencePlayer = game.Players[1];
            var attackPlayer = game.Players[2];
            game.Deck = new Deck(new NotSortedDeckCardGenerator());
            game.InitCardDeck();
            // ходим 10♠
            startAttackPlayer.Hand.StartAttack([1]);
            // подкидываем 10♦
            attackPlayer.Hand.Attack([5]);
            // отбиваемся K♠ от 10♠
            defencePlayer.Hand.Defence(0, 0);
            // отбиваемся J♦ от 10♦
            defencePlayer.Hand.Defence(4, 1);
            game.StopRound();
            Assert.That(startAttackPlayer.Hand.Cards.Count, Is.EqualTo(6));
            Assert.That(defencePlayer.Hand.Cards.Count, Is.EqualTo(6));
            Assert.That(attackPlayer.Hand.Cards.Count, Is.EqualTo(6));
            Assert.That(game.Deck.CardsCount, Is.EqualTo(36 - 6 * 4 - 2 * 2));
        }


        /// <summary>
        /// Игрок кидает 4 одинаковые карты, а защитник забирает их себе.
        /// </summary>
        /// <remarks>
        /// Проверили, что можно начать раунд с нескольких карт.
        /// </remarks>
        [Test]
        public void StartAttackManyCardsAndNotDefenceTest()
        {
            var playerCards = new string[]
            {
                "A♠ A♦ A♣ A♥ 10♥ 7♦",
                "Q♠ Q♦ Q♣ Q♥ 10♣ 10♠",
            };
            var trumpValue = "6♦";
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            var startAttackPlayer = game.Players[0];
            var defencePlayer = game.Players[1];
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue));
            game.InitCardDeck();
            // ходим A♠ A♦ A♣ A♥
            startAttackPlayer.Hand.StartAttack([0, 1, 2, 3]);
            game.StopRound();
            Assert.That(startAttackPlayer.Hand.Cards.Count, Is.EqualTo(6));
            Assert.That(defencePlayer.Hand.Cards.Count, Is.EqualTo(6 + 4));
            Assert.That(game.Deck.CardsCount, Is.EqualTo(36 - 6 * 2 - 4));
        }

        /// <summary>
        /// Игрок кидает одну карту, второй подкидывает ещё 3, а защитник забирает их себе.
        /// </summary>
        /// <remarks>
        /// Проверили, что можно поддать несколько карт.
        /// </remarks>
        [Test]
        public void AttackManyCardsAndNotDefenceTest()
        {
            var playerCards = new string[]
            {
                "A♠ Q♦ Q♣ Q♥ 10♥ 7♦",
                "J♠ J♦ J♣ J♥ 9♣ 9♠",
                "Q♠ A♦ A♣ A♥ 10♣ 10♠",
            };
            var trumpValue = "6♦";
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            game.AddPlayer("3");
            var startAttackPlayer = game.Players[0];
            var defencePlayer = game.Players[1];
            var attackPlayer = game.Players[2];
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue));
            game.InitCardDeck();
            // ходим A♠
            startAttackPlayer.Hand.StartAttack([0]);
            // подкидываем A♦ A♣ A♥
            attackPlayer.Hand.Attack([1, 2, 3]);
            game.StopRound();
            Assert.That(startAttackPlayer.Hand.Cards.Count, Is.EqualTo(6));
            Assert.That(attackPlayer.Hand.Cards.Count, Is.EqualTo(6));
            Assert.That(defencePlayer.Hand.Cards.Count, Is.EqualTo(6 + 4));
            Assert.That(game.Deck.CardsCount, Is.EqualTo(36 - 6 * 3 - 4));
        }
    }
}