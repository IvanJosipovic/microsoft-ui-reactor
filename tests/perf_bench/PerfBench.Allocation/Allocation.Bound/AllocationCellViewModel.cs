using System.ComponentModel;

namespace PerfBench.Allocation.Bound;

public sealed class AllocationCellViewModel : INotifyPropertyChanged
{
    private int _value;
    private string _displayText = "0";
    private string _color = "White";

    private static readonly string[] Colors = { "Red", "Green", "Blue", "White" };

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public string Color
    {
        get => _color;
        set
        {
            if (_color == value) return;
            _color = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
        }
    }

    public void Increment()
    {
        _value++;
        DisplayText = (_value % 1000).ToString();
        Color = Colors[_value % Colors.Length];
    }
}
