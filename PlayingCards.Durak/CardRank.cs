namespace PlayingCards.Durak
{
    /// <summary>
    /// Старшинство карты.
    /// </summary>
    public class CardRank
    {
        /// <summary>
        /// Старшинство карты
        /// </summary>
        /// <param name="value"><see cref="Value"/></param>
        /// <param name="name"><see cref="Name"/></param>
        public CardRank(int value, string name)
        {
            Value = value;
            Name = name;
            ShortName = name == "10" ? "10" : name.Substring(0, 1);
        }

        /// <summary>
        /// Значение старшенства.
        /// </summary>
        /// <remarks>
        /// Как правило, чем больше, тем мощнее.
        /// </remarks>
        public int Value { get; }

        /// <summary>
        /// Наименование.
        /// </summary>
        /// <remarks>
        /// Например дама/queen, 9/деявятка.
        /// </remarks>
        public string Name { get; }

        /// <summary>
        /// Короткое наименование.
        /// </summary>
        public string ShortName { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
