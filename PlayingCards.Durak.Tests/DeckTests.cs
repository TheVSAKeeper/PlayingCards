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
                game.AddPlayer(new Player());
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
    }
}