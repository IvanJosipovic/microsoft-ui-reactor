using System.ComponentModel;

namespace PerfBench.DirtyTracking.Bound;

public sealed class CounterViewModel : INotifyPropertyChanged
{
    private int _index;
    private int _value;
    private string _displayText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public CounterViewModel(int index)
    {
        _index = index;
        _displayText = $"Counter {index}: 0";
    }

    public string DisplayText
    {
        get => _displayText;
        set
        {
            if (_displayText == value) return;
            _displayText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
        }
    }

    public void Increment()
    {
        _value++;
        DisplayText = $"Counter {_index}: {_value}";
    }
}
