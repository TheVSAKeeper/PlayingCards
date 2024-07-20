namespace PlayingCards.Durak.Tests.RealGames
{
    public class RealGameTests
    {
        /// <summary>
        /// Тест посвящённый реальной игре бориса и отмороза.
        /// </summary>
        [Test]
        public void BorisVsOtmorozTestsTest()
        {
            var deck = "K♦ 9♥ Q♥ K♥ A♠ 9♠ J♥ J♠ 6♦ A♦ 10♠ 8♣ 10♣ J♦ 7♠ K♣ A♥ 7♥ 8♦ A♣ 8♥ Q♦ Q♠ 10♦";
            var playerCards = new string[]
            {
"9♣ 8♠ 6♠ Q♣ 6♥ 9♦",
"7♦ J♣ 7♣ K♠ 6♣ 10♥",
            };
            var game = new Game();
            var player0 = game.AddPlayer("Борис");
            var player1 = game.AddPlayer("Отмороз");
            game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, deckValues: deck));
            game.StartGame();

            player1.Hand.StartAttack("6♣");
            player0.Hand.Defence("9♣->6♣");
            game.StopRound();

            player0.Hand.StartAttack("6♥");
            player1.Hand.Defence("7♦->6♥");
            player0.Hand.Attack("6♠");
            player1.Hand.Defence("K♠->6♠");
            game.StopRound();

            player1.Hand.StartAttack("7♣");
            player0.Hand.Defence("Q♣->7♣");
            game.StopRound();

            player0.Hand.StartAttack("8♠");
            player1.Hand.Defence("8♦->8♠");
            player0.Hand.Attack("8♥");
            game.StopRound();

            player0.Hand.StartAttack("7♠");
            player1.Hand.Defence("8♠->7♠");
            game.StopRound();

            player1.Hand.StartAttack("8♦");
            game.StopRound();

            player1.Hand.StartAttack("10♦");
            player0.Hand.Defence("J♦->10♦");
            player1.Hand.Attack("J♣");
            player0.Hand.Defence("K♣->J♣");
            game.StopRound();

            player0.Hand.StartAttack("10♠");
            game.StopRound();

            player0.Hand.StartAttack("Q♠");
            game.StopRound();

            player0.Hand.StartAttack("6♦");
            game.StopRound();

            player0.Hand.StartAttack("J♠");
            player1.Hand.Defence("Q♠->J♠");
            player0.Hand.Attack("Q♦");
            game.StopRound();

            player0.Hand.StartAttack("9♠");
            player1.Hand.Defence("Q♠->9♠");
            player0.Hand.Attack("9♦");
            player1.Hand.Defence("Q♦->9♦");
            game.StopRound();

            player1.Hand.StartAttack("10♥");
            player0.Hand.Defence("J♥->10♥");
            player1.Hand.Attack("10♣");
            player0.Hand.Defence("8♦->10♣");
            player1.Hand.Attack("8♥");
            player0.Hand.Defence("K♥->8♥");
            game.StopRound();

            player0.Hand.StartAttack("9♥");
            game.StopRound();

            player0.Hand.StartAttack("Q♥");
            player1.Hand.Defence("6♦->Q♥");
            game.StopRound();

            player1.Hand.StartAttack("7♥");
            player0.Hand.Defence("A♥->7♥");
            player1.Hand.Attack("A♣");
            player0.Hand.Defence("A♦->A♣");
            game.StopRound();

            player0.Hand.StartAttack("K♦");
            game.StopRound();

            player0.Hand.StartAttack("A♠");
            Assert.That(game.Status, Is.EqualTo(GameStatus.Finish));
            Assert.That(game.LooserPlayer.Name, Is.EqualTo(player1.Name));
        }
    }
}
