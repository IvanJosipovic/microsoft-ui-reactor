using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Minesweeper.Game;
using Minesweeper.Persistence;
using static Microsoft.UI.Reactor.Factories;

namespace Minesweeper.Components.Dialogs;

/// <summary>
/// Pure renderers for the three dialogs. Each returns the Element that goes
/// inside <see cref="ModalOverlay"/>'s body slot. The overlay itself owns
/// the title bar, button row, and backdrop.
/// </summary>
internal static class DialogContent
{
    /// <summary>High-scores list — three rows, one per classic difficulty.</summary>
    public static Element HighScores(HighScores scores)
    {
        Element Row(string label, HighScoreEntry? entry) =>
            HStack(12,
                TextBlock(label).Width(140).SemiBold(),
                entry is null
                    ? TextBlock("(no record yet)").Foreground(Theme.SecondaryText)
                    : TextBlock($"{entry.Seconds}s — {entry.Name}").Foreground(Theme.PrimaryText)
            );

        return VStack(8,
            Row("Beginner", scores.Beginner),
            Row("Intermediate", scores.Intermediate),
            Row("Expert", scores.Expert)
        );
    }

    /// <summary>New-best capture form. Shows the result and a name input.</summary>
    public static Element NewBest(DifficultyKind kind, int seconds, string name, Action<string> onNameChanged)
    {
        return VStack(12,
            TextBlock($"You set a new best time on {kind}: {seconds} seconds!")
                .FontSize(16).SemiBold(),
            TextBlock("Enter your name for the high-score table:")
                .Foreground(Theme.SecondaryText),
            TextField(name, onNameChanged, placeholder: "Your name").Width(280)
        );
    }

    /// <summary>
    /// Custom-board form. Reports validation errors inline so the player
    /// sees why the Start button is disabled.
    /// </summary>
    public static Element CustomBoard(
        string rows, Action<string> onRowsChanged,
        string columns, Action<string> onColumnsChanged,
        string mines, Action<string> onMinesChanged,
        string? error)
    {
        return VStack(10,
            HStack(8, TextBlock("Rows (4–24):").Width(140), TextField(rows, onRowsChanged).Width(80)),
            HStack(8, TextBlock("Columns (4–30):").Width(140), TextField(columns, onColumnsChanged).Width(80)),
            HStack(8, TextBlock("Mines:").Width(140), TextField(mines, onMinesChanged).Width(80)),
            error is null
                ? TextBlock("").Height(0)
                : TextBlock(error).Foreground(Theme.SystemCritical)
        );
    }
}
