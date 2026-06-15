using System.Reflection;
using System.Text;

namespace PlayingCards.Durak.Server;

/// <summary>
/// Пул тематических имён для ИИ-болванчиков, разобранный из встроенного ресурса <c>names.txt</c>
/// (свита боссов Escape from Tarkov). Загружается один раз и потокобезопасно.
/// </summary>
/// <remarks>
/// Файл уже подготовлен: один ник на строку. Парсер тривиален — читаем непустые строки и
/// дедуплицируем (страховка от случайных повторов в файле, чтобы двум ботам не выпало одно имя).
/// Если ресурс недоступен/пуст — пул пуст, и вызывающая сторона откатывается на «Бот N».
/// </remarks>
public static class BotNames
{
    private static readonly Lazy<IReadOnlyList<string>> LazyPool =
        new(Parse, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Полный плоский список уникальных имён (может быть пустым).
    /// </summary>
    public static IReadOnlyList<string> Pool => LazyPool.Value;

    /// <summary>
    /// Выбрать случайное имя, не занятое за столом. Возвращает <c>null</c>, если свободных нет.
    /// </summary>
    /// <param name="taken">Имена, уже занятые за столом (люди и другие боты).</param>
    public static string? PickName(IReadOnlyCollection<string> taken)
    {
        var pool = Pool;

        if (pool.Count == 0)
        {
            return null;
        }

        var start = Random.Shared.Next(pool.Count);

        for (var offset = 0; offset < pool.Count; offset++)
        {
            var name = pool[(start + offset) % pool.Count];

            if (taken.Contains(name) == false)
            {
                return name;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> Parse()
    {
        var text = ReadResource();

        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rawLine in text.Split('\n'))
        {
            var name = rawLine.Trim('\r', ' ', '\t');

            if (name.Length > 0 && seen.Add(name))
            {
                names.Add(name);
            }
        }

        return names;
    }

    private static string? ReadResource()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("names.txt", StringComparison.Ordinal));

        if (resourceName == null)
        {
            return null;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            return null;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
