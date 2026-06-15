namespace PlayingCards.Durak.Blazor.Services;

/// <summary>
/// Идентификатор сборки фронта — чтобы после деплоя глазами убедиться, что задеплоилась новая версия.
/// Значения прокидываются в Docker-образ build-аргументами (см. <c>Dockerfile</c> и <c>deploy-durak.ps1</c>):
/// <c>BUILD_VERSION</c> — git short-SHA (точка отката), <c>BUILD_DATE</c> — время сборки образа.
/// При локальном запуске без Docker переменных нет — тогда версия «dev», дата отсутствует.
/// </summary>
public sealed class BuildInfo
{
    public BuildInfo(IConfiguration configuration)
    {
        Version = configuration["BUILD_VERSION"] is { Length: > 0 } version ? version : "dev";
        Date = configuration["BUILD_DATE"] is { Length: > 0 } date ? date : null;
    }

    /// <summary>Короткий идентификатор сборки: git short-SHA (+ <c>-dirty</c>) либо «dev» при локальном запуске.</summary>
    public string Version { get; }

    /// <summary>Время сборки образа (UTC) либо <c>null</c>, если фронт запущен не из Docker-образа.</summary>
    public string? Date { get; }
}
