using PlayingCards.Durak.Server;

namespace PlayingCards.Durak.Server.Tests;

/// <summary>
/// Пул имён ботов из встроенного <c>names.txt</c> (issue F8). Файл подготовлен заранее —
/// один ник на строку, поэтому парсер лишь читает непустые строки и дедуплицирует.
/// </summary>
[TestFixture]
public class BotNamesTests
{
    [Test]
    public void Pool_LoadsNamesFromFile()
    {
        Assert.That(BotNames.Pool, Is.Not.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(BotNames.Pool, Does.Contain("Афганец"), "первый ник в файле");
            Assert.That(BotNames.Pool, Does.Contain("Зимний"));
            Assert.That(BotNames.Pool, Does.Contain("Височный"), "последний ник в файле");
            Assert.That(BotNames.Pool, Does.Contain("Антон Заводской"));
            Assert.That(BotNames.Pool, Does.Contain("Димон Светлоозерский"));
        });
    }

    [Test]
    public void Pool_HasNoDuplicates()
    {
        Assert.That(BotNames.Pool, Is.Unique);
    }

    [Test]
    public void Pool_NamesAreCleanSingleTokens()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BotNames.Pool, Has.None.Contains(","), "нет неразобранных перечислений");
            Assert.That(BotNames.Pool, Has.None.Contains(":"), "нет «Источник:» и прочей прозы");
            Assert.That(BotNames.Pool, Has.None.Contains("http"), "нет ссылок");
            Assert.That(BotNames.Pool.Any(name => name.Length > 40), Is.False, "имена короткие");
        });
    }

    [Test]
    public void PickName_ReturnsTheOnlyFreeName()
    {
        var taken = BotNames.Pool.Take(BotNames.Pool.Count - 1).ToHashSet(StringComparer.Ordinal);
        var onlyFree = BotNames.Pool[^1];

        Assert.That(BotNames.PickName(taken), Is.EqualTo(onlyFree));
    }

    [Test]
    public void PickName_AllTaken_ReturnsNull()
    {
        var allTaken = BotNames.Pool.ToHashSet(StringComparer.Ordinal);

        Assert.That(BotNames.PickName(allTaken), Is.Null);
    }
}
