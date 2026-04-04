using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;

namespace PerfBench.Journal.Bound;

public sealed class JournalCellViewModel : INotifyPropertyChanged
{
    private string _text = string.Empty;
    private Brush _foreground = new SolidColorBrush(Microsoft.UI.Colors.White);

    public string Text
    {
        get => _text;
        set { if (_text != value) { _text = value; OnPropertyChanged(); } }
    }

    public Brush Foreground
    {
        get => _foreground;
        set { if (_foreground != value) { _foreground = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
