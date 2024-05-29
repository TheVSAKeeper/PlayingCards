namespace PlayingCards.Durak.Tests
{
    public class DeckTests
    {
        /// <summary>
        /// ѕеретусовали колоду и убедились что в ней 9 номиналов карт и 4 масти, и карт 36 штук.
        /// </summary>
        [Test]
        public void DeckCardsCountTest()
        {
            var deck = new Deck();
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
        /// –аздали игрокам по 6 карт в начале игры.
        /// </summary>
        /// <param name="playerCount"> оличество игроков.</param>
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
                game.AddPlayer(new Player());
            }
            game.InitCardDeck();

            foreach (var player in game.Players)
            {
                Assert.That(player.Hand.Cards.Count(), Is.EqualTo(6));
            }
        }

        /// <summary>
        /// ѕроверка, кто первый ходит.
        /// </summary>
        /// <remarks>
        /// ” первого ход€щего должен быть козырь наименьшего номинала на руке, чем у других.
        /// </remarks>
        /// <param name="playerCount"> оличество игроков в игре.</param>
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
                game.AddPlayer(new Player { Name = "Player" + i });
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
        /// ѕроверка, на кого, первым ход€т.
        /// </summary>
        /// <remarks>
        /// —ледующий после активного, защищаетс€.
        /// </remarks>
        /// <param name="playerCount"> оличество игроков в игре.</param>
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
                game.AddPlayer(new Player { Name = "Player" + i });
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
        /// јтакующа€ карта меньше чем та, которой пытаемс€ защититьс€.
        /// </summary>
        [Test]
        public void SuccessDefenceTrumpSuitTest()
        {
            // шестЄрка
            var attackCard = CardsHolder.GetCards()[0];
            // семЄрка той же масти
            var defenceCard = CardsHolder.GetCards()[1];
            // козырный туз той же масти
            var trumpCard = CardsHolder.GetCards()[8];
            var game = new Game();
            game.Deck = new Deck() { TrumpCard = trumpCard };

            var attackTableCard = new TableCard(game, attackCard);
            attackTableCard.Defence(defenceCard);
        }

        /// <summary>
        /// јтакующа€ карта больше чем та, которой пытаемс€ защититьс€.
        /// </summary>
        [Test]
        public void FailDefenceTrumpSuitTest()
        {
            // восьмЄрка
            var attackCard = CardsHolder.GetCards()[2];
            // семЄрка той же масти
            var defenceCard = CardsHolder.GetCards()[1];
            // козырный туз той же масти
            var trumpCard = CardsHolder.GetCards()[8];
            var game = new Game();
            game.Deck = new Deck() { TrumpCard = trumpCard };

            var attackTableCard = new TableCard(game, attackCard);
            Assert.Throws<Exception>(() => attackTableCard.Defence(defenceCard));
        }
    }
}