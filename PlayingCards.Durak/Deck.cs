namespace PlayingCards.Durak
{
    /// <summary>
    /// Колода карт.
    /// </summary>
    public class Deck
    {
        /// <summary>
        /// Колода карт.
        /// </summary>
        public Deck()
        {
            Cards = new List<Card>();
        }

        /// <summary>
        /// КАрты.
        /// </summary>
        public List<Card> Cards { get; set; }
    }
}
