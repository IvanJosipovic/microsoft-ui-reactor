using System.ComponentModel;

namespace PerfBench.TimeSlice.Bound;

public sealed class TimeSliceItemViewModel : INotifyPropertyChanged
{
    private string _displayText;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TimeSliceItemViewModel(int index)
    {
        _displayText = $"Item {index}: mounted";
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
}
