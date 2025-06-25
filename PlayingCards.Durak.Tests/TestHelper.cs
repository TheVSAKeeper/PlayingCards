﻿namespace PlayingCards.Durak.Tests;

public static class TestHelper
{
    /// <summary>
    /// Превратить строку в индексы карт из руки и начать раунд.
    /// </summary>
    public static void StartAttack(this PlayerHand hand, string cardsString)
    {
        var result = GetHandCardIndexes(hand.Cards.ToList(), cardsString);
        hand.StartAttack(result);
    }

    /// <summary>
    /// Превратить строку в индексы карт из руки и подкинуть.
    /// </summary>
    public static void Attack(this PlayerHand hand, string cardsString)
    {
        var result = GetHandCardIndexes(hand.Cards.ToList(), cardsString);
        hand.Attack(result);
    }

    /// <summary>
    /// Превратить строку в индексы карт из руки и отбиться.
    /// </summary>
    public static void Defence(this PlayerHand hand, string cardsString)
    {
        var index = cardsString.IndexOf("->", StringComparison.InvariantCultureIgnoreCase);
        var defenceCardStr = cardsString[..index];
        var attackCardStr = cardsString[(index + 2)..];

        var defenceCardIndex = GetHandCardIndexes(hand.Cards.ToList(), defenceCardStr)[0];
        var attackCardIndex = GetHandCardIndexes(GetTableAttackCards(hand.Game), attackCardStr)[0];
        hand.Defence(defenceCardIndex, attackCardIndex);
    }

    /// <summary>
    /// Превратить строку в индексы карт из руки и сыграть карту.
    /// </summary>
    public static void PlayCards(this PlayerHand hand, string cardsString, string? attackCardString = null)
    {
        var result = GetHandCardIndexes(hand.Cards.ToList(), cardsString);

        if (attackCardString != null)
        {
            var attackCardIndex = GetHandCardIndexes(GetTableAttackCards(hand.Game), attackCardString)[0];
            hand.PlayCards(result, attackCardIndex);
        }
        else
        {
            hand.PlayCards(result);
        }
    }

    private static int[] GetHandCardIndexes(List<Card> handCards, string cardsString)
    {
        var cardsSplit = cardsString.Split(' ');
        var result = new int[cardsSplit.Length];

        for (var i = 0; i < cardsSplit.Length; i++)
        {
            var cardString = cardsSplit[i];
            var card = handCards.Single(handCard => handCard.ToString() == cardString);
            var index = handCards.IndexOf(card);
            result[i] = index;
        }

        return result;
    }

    private static List<Card> GetTableAttackCards(Game game)
    {
        return game.Cards.Select(tableCard => tableCard.AttackCard).ToList();
    }
}
