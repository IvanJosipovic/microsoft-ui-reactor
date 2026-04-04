using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PerfBench.InteractivePool.Bound;

public sealed class InteractiveItemViewModel : INotifyPropertyChanged
{
    private string _buttonLabel = string.Empty;
    private string _textValue = string.Empty;
    private bool _isToggled;

    public string ButtonLabel
    {
        get => _buttonLabel;
        set { if (_buttonLabel != value) { _buttonLabel = value; OnPropertyChanged(); } }
    }

    public string TextValue
    {
        get => _textValue;
        set { if (_textValue != value) { _textValue = value; OnPropertyChanged(); } }
    }

    public bool IsToggled
    {
        get => _isToggled;
        set { if (_isToggled != value) { _isToggled = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
