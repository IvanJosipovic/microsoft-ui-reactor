using System.Text.Json.Serialization;

namespace Minesweeper.Persistence;

/// <summary>
/// One entry in the high-score table — the player's name and time for a single
/// difficulty preset. Custom games are not tracked (the original game's
/// behavior).
/// </summary>
public sealed record HighScoreEntry(string Name, int Seconds, string AchievedAtIso);

/// <summary>
/// Top score per classic difficulty. A null entry means "no record yet".
/// </summary>
public sealed record HighScores(
    HighScoreEntry? Beginner = null,
    HighScoreEntry? Intermediate = null,
    HighScoreEntry? Expert = null)
{
    public static readonly HighScores Empty = new();
}

[JsonSerializable(typeof(HighScores))]
[JsonSerializable(typeof(HighScoreEntry))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class HighScoresJsonContext : JsonSerializerContext
{
}
