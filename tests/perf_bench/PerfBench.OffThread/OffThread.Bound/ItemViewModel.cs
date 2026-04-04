using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PerfBench.OffThread.Bound;

public sealed class ItemViewModel : INotifyPropertyChanged
{
    private string _displayText = string.Empty;

    public string DisplayText
    {
        get => _displayText;
        set { if (_displayText != value) { _displayText = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
