using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayingCards.Durak.Tests
{
    public class LogParser
    {
        [Test]
        [Ignore("для генерации тестов из логов")]
        public void CheckLooserIfDefenceLastCardTest()
        {
            var log = GetLog();
            var testSb = new StringBuilder();
            var playerSb = new StringBuilder();
            var lines = log.Split('\r').Select(x => x.Trim('\n')).ToArray();
            var isInitGame = false;
            string? deck = null;
            var playersCards = new List<string>();
            var isFirstStartAttack = true;
            for (int i = 0; i < lines.Length; i++)
            {
                string? line = lines[i];
                if (line.Contains("start game"))
                {
                    isInitGame = true;
                    continue;
                }
                if (isInitGame)
                {
                    if (deck == null)
                    {
                        deck = line.Substring("deck:".Length+1);
                        continue;
                    }
                    if (line.StartsWith("pl-"))
                    {
                        playersCards.Add(line.Split(':')[1].Trim(' '));
                        continue;
                    }
                    else
                    {
                        isInitGame = false;
                        continue;
                    }
                }
                else
                {
                    const string startAttackStr = "start attack";
                    const string attackStr = "attack";
                    const string defenceStr = "defence";
                    if (line.Contains(startAttackStr))
                    {
                        var indx = line.IndexOf(startAttackStr);
                        var playerIndex = int.Parse(line.Substring(0, indx - 1).Split('|').Last());
                        var cardsStr = line.Substring(indx + startAttackStr.Length).Trim(' ');
                        if (isFirstStartAttack == false)
                        {
                            playerSb.AppendLine($"game.StopRound();");
                        }
                        isFirstStartAttack = false;
                        playerSb.AppendLine();
                        playerSb.AppendLine($"player{playerIndex}.Hand.StartAttack(\"{cardsStr}\");");
                    }
                    else if (line.Contains(attackStr))
                    {
                        var indx = line.IndexOf(attackStr);
                        var playerIndex = int.Parse(line.Substring(0, indx - 1).Split('|').Last());
                        var cardsStr = line.Substring(indx + attackStr.Length).Trim(' ');
                        playerSb.AppendLine($"player{playerIndex}.Hand.Attack(\"{cardsStr}\");");
                    }
                    else if (line.Contains(defenceStr))
                    {
                        var indx = line.IndexOf(defenceStr);
                        var playerIndex = int.Parse(line.Substring(0, indx - 1).Split('|').Last());
                        var cardsStr = line.Substring(indx + defenceStr.Length).Trim(' ');
                        playerSb.AppendLine($"player{playerIndex}.Hand.Defence(\"{cardsStr}\");");
                    }
                }
            }

            testSb.AppendLine("var deck = \"" + deck + "\";");
            testSb.AppendLine("var playerCards = new string[]");
            testSb.AppendLine("{");
            foreach (var playerCards in playersCards)
            {
                testSb.AppendLine("\"" + playerCards + "\",");
            }
            testSb.AppendLine("};");
            testSb.AppendLine("var game = new Game();");
            for (int i = 0; i < playersCards.Count; i++)
            {
                testSb.AppendLine($"var player{i} = game.AddPlayer(\"{i}\");");
            }
            testSb.AppendLine("game.Deck = new Deck(new SortedDeckCardGenerator(playerCards, deckValues: deck));");
            testSb.AppendLine("game.StartGame();");

            var test = testSb.ToString() + playerSb.ToString();
            Console.WriteLine(test);
        }

        private string GetLog()
        {
            return @"";
        }
    }
}
