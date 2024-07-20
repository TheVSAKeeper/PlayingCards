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
            game.StartGame();

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
            game.StartGame();

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
            game.StartGame();

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
                Assert.Throws<BusinessException>(() => attackTableCard.Defence(defenceCard));
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
            game.StartGame();
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
            var playerCards = new string[]
            {
                "A♠ Q♦ Q♣ Q♥ 10♥ 7♦",
                "Q♠ A♦ A♣ A♥ 10♣ 10♠",
            };
            var trumpValue = "6♦";
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            var player1 = game.Players[0];
            var player2 = game.Players[1];
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue));
            game.StartGame();
            // ходим Q♦
            player1.Hand.StartAttack([1]);
            // подкинули Q♣
            player1.Hand.Attack([1]);
            // отбиваемся A♣ от Q♣
            player2.Hand.Defence(2, 1);
            // решаем не отбиваться, забираем на руки
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
            game.StartGame();
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
            game.StartGame();
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
            game.StartGame();
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
        
        /// <summary>
        /// Игрок кидает одну карту, потом подкидывает одну карту доступного ранга и вторую недоступного ранга.
        /// </summary>
        /// <remarks>
        /// Проверили, что нельзя поддать карту доступного ранга и недоступного ранга.
        /// </remarks>
        [Test]
        public void AttackTwoDifferentCardSequence()
        {
            var playerCards = new string[]
            {
                "J♠ J♦ J♣ J♥ 10♥ 7♦",
                "A♠ Q♦ Q♣ Q♥ 9♣ 9♠",
            };
            var trumpValue = "6♦";
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            var startAttackPlayer = game.Players[0];
            var attackPlayer = game.Players[1];
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue));
            
            game.StartGame();
            
            // ходим J♠
            startAttackPlayer.Hand.StartAttack([0]);
            
            // подкидываем J♦
            startAttackPlayer.Hand.Attack([0]);
            
            // подкидываем 7♦
            Assert.Throws<BusinessException>(() => startAttackPlayer.Hand.Attack([3]));
        }        
        
        /// <summary>
        /// Игрок кидает одну карту, потом подкидывает одновременно одну карту доступного ранга и вторую недоступного ранга.
        /// </summary>
        /// <remarks>
        /// Проверили, что нельзя поддать одновременно карту доступного ранга и недоступного ранга.
        /// </remarks>
        [Test]
        public void AttackTwoDifferentCard()
        {
            var playerCards = new string[]
            {
                "J♠ J♦ J♣ J♥ 10♥ 7♦",
                "A♠ Q♦ Q♣ Q♥ 9♣ 9♠",
            };
            var trumpValue = "6♦";
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            var startAttackPlayer = game.Players[0];
            var attackPlayer = game.Players[1];
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue));
            
            game.StartGame();
            
            // ходим J♠
            startAttackPlayer.Hand.StartAttack([0]);
            
            // подкидываем J♦ и 7♦
            Assert.Throws<BusinessException>(() => startAttackPlayer.Hand.Attack([0, 4]));
        }

        /// <summary>
        /// Подкинем карту, ранг которой, равен рангу защитной карты.
        /// </summary>
        /// <remarks>
        /// Была ошибка, если вальта отбить дамой, то даму нельзя было поддать.
        /// </remarks>
        [Test]
        public void AttackCardsWithDefencedCardRankTest()
        {
            var playerCards = new string[]
            {
                "A♠ Q♦ J♣ J♥ 10♥ 7♦",
                "J♠ J♦ Q♣ Q♥ 9♣ 9♠",
            };
            var trumpValue = "6♦";
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            var attackPlayer = game.Players[0];
            var defencePlayer = game.Players[1];
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue));
            game.StartGame();
            // ходим J♣
            attackPlayer.Hand.StartAttack([2]);
            // отбиваем Q♣->J♣
            defencePlayer.Hand.Defence(2, 0);
            // подкидываем Q♦
            attackPlayer.Hand.Attack([1]);
            game.StopRound();
            Assert.That(attackPlayer.Hand.Cards.Count, Is.EqualTo(6));
            Assert.That(defencePlayer.Hand.Cards.Count, Is.EqualTo(6 + 2));
            Assert.That(game.Deck.CardsCount, Is.EqualTo(36 - 6 * 2 - 2));
        }

        /// <summary>
        /// Проверка, что в игре на двоих, после того, как игрок забрал карты. ход остаётся у атакующего.
        /// </summary>
        [Test]
        public void CorrectChangeActivePlayerTest()
        {
            var playerCards = new string[]
            {
                "A♠ Q♦ Q♣ Q♥ 10♥ 7♦",
                "Q♠ A♦ A♣ A♥ 10♣ 10♠",
            };
            var trumpValue = "6♦";
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            var attackPlayer = game.Players[0];
            var defencePlayer = game.Players[1];
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue));
            game.StartGame();
            // ходим Q♣
            attackPlayer.Hand.StartAttack([2]);
            game.StopRound();
            Assert.That(game.ActivePlayer.Name, Is.EqualTo(attackPlayer.Name));
        }

        /// <summary>
        /// Проверка, что в игре на двоих, после отбивания. ход переходит второму игроку.
        /// </summary>
        /// <remarks>
        /// Если один из игроков, не берёт карту из колоды, то активный игрок неверно определялся.
        /// </remarks>
        [Test]
        public void CorrectChangeActiveAfterDefencePlayerTest()
        {
            var playerCards = new string[]
            {
                "A♠ Q♦ Q♣ Q♥ 10♥ 7♦",
                "Q♠ A♦ A♣ A♥ 10♣ 10♠",
            };
            var trumpValue = "6♦";
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            var attackPlayer = game.Players[0];
            var defencePlayer = game.Players[1];
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue));
            game.StartGame();
            // ходим Q♣
            attackPlayer.Hand.StartAttack([2]);
            game.StopRound();
            // ходим Q♦
            attackPlayer.Hand.StartAttack([1]);
            // отбиаемся A♦
            defencePlayer.Hand.Defence(1, 0);
            game.StopRound();
            Assert.That(game.ActivePlayer.Name, Is.EqualTo(defencePlayer.Name));
        }

        /// <summary>
        /// Проверка, если игрок без карт, то его не учитываем в выборе активного игрока.
        /// </summary>
        [Test]
        public void CorrectChangeActiveZeroCardsPlayerTest()
        {
            var playerCards = new string[]
            {
                "J♠ Q♦ Q♣ Q♥ 10♥ 7♦",
                "Q♠ 9♦ 9♥ 9♠ 9♣ 8♦",
                "A♠ A♦ A♣ A♥ 10♣ 10♠",
            };
            var trumpValue = "6♦";
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            game.AddPlayer("3");
            var attackPlayer = game.Players[0];
            var defencePlayer = game.Players[1];
            var expectedStartAttackPlayer = game.Players[2];
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue));
            game.StartGame();
            // удалим у отбивающегося все карты кроме одной
            defencePlayer.Hand.Cards.RemoveRange(1, 5);
            // очистим колоду
            game.Deck.Cards.Clear();

            // ходим J♠
            attackPlayer.Hand.StartAttack([0]);
            // отбиваем Q♠->J♠
            defencePlayer.Hand.Defence(0, 0);
            game.StopRound();
            Assert.That(game.ActivePlayer.Name, Is.EqualTo(expectedStartAttackPlayer.Name));
        }

        /// <summary>
        /// Проверка, если игрок без карт, то его не учитываем в выборе защищающегося игрока.
        /// </summary>
        [Test]
        public void CorrectChangeDefenceZeroCardsPlayerTest()
        {
            var playerCards = new string[]
            {
                "J♠ Q♦ Q♣ Q♥ 10♥ 7♦",
                "Q♠ 9♦ 9♥ 9♠ 9♣ 8♦",
                "A♠ A♦ A♣ A♥ 10♣ 10♠",
            };
            var trumpValue = "6♦";
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            game.AddPlayer("3");
            var player1 = game.Players[0];
            var player2 = game.Players[1];
            var zeorCardsPlayer = game.Players[2];
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue));
            game.StartGame();
            // удалим все карты у игрока
            zeorCardsPlayer.Hand.Cards.Clear();
            // очистим колоду
            game.Deck.Cards.Clear();

            // ходим J♠
            player1.Hand.StartAttack([0]);
            // отбиваем Q♠->J♠
            player2.Hand.Defence(0, 0);
            game.StopRound();
            Assert.That(game.DefencePlayer.Name, Is.EqualTo(player1.Name));
            Assert.That(game.ActivePlayer.Name, Is.EqualTo(player2.Name));
        }

        /// <summary>
        /// Проверка, что в игре на четверых, когда осталось двое, после забирания, ход остаётся у того кто ходил.
        /// </summary>
        /// <remarks>
        /// Была бага, что игрок после того как забрал карты, начинал ходить.
        /// </remarks>
        [Test]
        public void CorrectChangeActiveAfterFailDefencePlayerTest()
        {
            // arrange
            var playerCards = new string[]
            {
                "Q♠ Q♦ Q♣ Q♥ 10♥ 7♦",
                "A♠ A♦ A♣ A♥ 10♣ J♦",
                "9♠ 9♦ 9♣ 9♥ J♣ J♠",
                "8♠ 8♦ 8♣ 8♥ K♣ K♠",
            };
            var trumpValue = "6♦";
            var game = new Game();
            var player1 = game.AddPlayer("1");
            var player2 = game.AddPlayer("2");
            var player3 = game.AddPlayer("3");
            var player4 = game.AddPlayer("4");
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue));
            game.StartGame();
            player1.Hand.Cards.RemoveRange(0, 5);
            player2.Hand.Cards.RemoveRange(0, 5);
            game.Deck.Cards.Clear();
            // ходим 7♦
            player1.Hand.StartAttack([0]);
            // отбиваем  J♦ -> 7♦
            player2.Hand.Defence(0,0);
            game.StopRound(); // 1 и 2 игрок вышли из игры

            // ходим J♣
            player3.Hand.StartAttack([4]);
            // отбиаемся K♣->J♣
            player4.Hand.Defence(4, 0);
            game.StopRound();

            // act 
            // ходим 8♠
            player4.Hand.StartAttack([0]);
            // player3 забирает 8♠
            game.StopRound();

            // assert
            Assert.That(game.ActivePlayer.Name, Is.EqualTo(player4.Name));
            Assert.That(game.DefencePlayer.Name, Is.EqualTo(player3.Name));
        }

        /// <summary>
        /// Ошибка, если сходить количеством карт, больше, чем у защищающегося.
        /// </summary>
        [Test]
        public void StartAttackOverflowTest()
        {
            var playerCards = new string[]
            {
                "A♠ Q♦ J♣ J♥ 10♥ 9♠",
                "J♠ J♦ Q♣ Q♥ 9♣ 10♠", // 10♠ будет козырем
            };
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            var attackPlayer = game.Players[0];
            var defencePlayer = game.Players[1];
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, null, 24));
            game.StartGame();
            defencePlayer.Hand.Cards.RemoveRange(1, 5); // удалим из руки все карты, кроме одной
            // ходим J♣ J♥
            Assert.Throws<BusinessException>(() => attackPlayer.Hand.StartAttack([2, 3]));
        }

        /// <summary>
        /// Ошибка, если поддаваемое количеством карт плюс карт на столе, больше, чем у защищающегося.
        /// </summary>
        [Test]
        public void AttackOverflowTest()
        {
            var playerCards = new string[]
            {
                "J♠ J♦ J♣ J♥ 10♥ 9♠",
                "A♠ Q♦ Q♣ Q♥ 9♣ 10♠", // 10♠ будет козырем
            };
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            var attackPlayer = game.Players[0];
            var defencePlayer = game.Players[1];
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, null, 24));
            game.StartGame();
            defencePlayer.Hand.Cards.RemoveRange(3, 3); // оставим в руке 3 карты
            // ходим J♠ J♦
            attackPlayer.Hand.StartAttack([0, 1]);
            // поддаём J♣ J♥
            Assert.Throws<BusinessException>(() => attackPlayer.Hand.StartAttack([0, 1]));
        }

        /// <summary>
        /// Проверка, что нельзя до первого отбоя атаковать больше 5 картами.
        /// </summary>
        [Test]
        public void FirstDefenceMaxFiveAttackCardsTest()
        {
            var playerCards = new string[]
            {
                "J♠ J♦ J♣ J♥ Q♣ Q♦",
                "A♠ 10♥ 9♠ Q♥ 9♣ 10♠",
            };
            var trumpValue = "6♦";
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            var attackPlayer = game.Players[0];
            var defencePlayer = game.Players[1];
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue));
            game.StartGame();
            // ходим J♠ J♦ J♣ J♥
            attackPlayer.Hand.StartAttack([0, 1, 2, 3]);
            // отбиваем  Q♥->J♥
            defencePlayer.Hand.Defence(3, 3);
            // поддаём Q♣ Q♦
            Assert.Throws<BusinessException>(() => attackPlayer.Hand.Attack([0, 1]));
        }


        /// <summary>
        /// Проверка, что номинал козырной карты, которую надо всем показать, корректный.
        /// </summary>
        [Test]
        [TestCase("J♠ A♣ J♣ J♥ Q♣ 7♦", 7)]
        [TestCase("J♠ A♣ J♣ J♥ 8♦ 7♦", 7)]
        [TestCase("J♠ A♣ J♣ J♥ 7♦ 8♦", 7)]
        [TestCase("J♠ A♣ J♣ J♥ Q♣ 7♥", null)]
        public void NeedShowCardMinTrumpValueTest(string firstPlayerCards, int? expectedMinTrumpValue)
        {
            var playerCards = new string[]
            {
                firstPlayerCards,
                "A♠ 10♥ 9♠ Q♥ 9♣ 10♠",
            };
            var trumpValue = "6♦";
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue));
            game.StartGame();
            Assert.That(game.NeedShowCardMinTrumpValue, Is.EqualTo(expectedMinTrumpValue));
        }

        /// <summary>
        /// Проверка, что определяется проигравший.
        /// </summary>
        [Test]
        public void CheckLooserTest()
        {
            var game = new Game();
            game.AddPlayer("1");
            game.AddPlayer("2");
            game.AddPlayer("3");
            game.AddPlayer("4");
            game.AddPlayer("5");
            game.AddPlayer("6");
            game.Deck = new Deck(new NotSortedDeckCardGenerator());
            game.StartGame();
            var activePlayerIndex = game.Players.IndexOf(game.ActivePlayer);
            var defencePlayerIndex = game.Players.IndexOf(game.DefencePlayer);
            for (var i = 0; i < 6; i++)
            {
                var player = game.Players[i];
                if (i != defencePlayerIndex)
                {
                    if (i == activePlayerIndex)
                    {
                        player.Hand.Cards = player.Hand.Cards.Take(1).ToList();
                    }
                    else
                    {
                        player.Hand.Cards.Clear();
                    }
                }
            }
            game.Players[activePlayerIndex].Hand.StartAttack([0]);

            Assert.That(game.Status, Is.EqualTo(GameStatus.Finish));
            Assert.That(game.LooserPlayer, Is.Not.Null);
            Assert.That(game.LooserPlayer.Name, Is.EqualTo(game.Players[defencePlayerIndex].Name));
        }


        /// <summary>
        /// Проверка, что игра кончилась, если отбился последней картой.
        /// </summary>
        [Test]
        public void CheckLooserIfDefenceLastCardTest()
        {
            // arrange
            var playerCards = new string[]
            {
                "J♥ J♠ J♦ J♣ Q♣ Q♦",
                "10♥ A♠ 9♠ Q♥ 9♣ 7♦",
            };
            var trumpValue = "6♦";

            var game = new Game();
            var winnerPlayer = game.AddPlayer("1");
            var looserPlayer = game.AddPlayer("2");
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, trumpValue));
            game.StartGame();
            winnerPlayer.Hand.Cards.RemoveRange(1, 5);
            game.Deck.Cards.Clear();

            // ходим 10♥
            looserPlayer.Hand.StartAttack([0]);

            // act
            // отбиваемся последней картой J♥->10♥
            winnerPlayer.Hand.Defence(0, 0);

            // assert
            Assert.That(game.Status, Is.EqualTo(GameStatus.Finish));
            Assert.That(game.LooserPlayer, Is.Not.Null);
            Assert.That(game.LooserPlayer.Name, Is.EqualTo(looserPlayer.Name));
        }
    }
}