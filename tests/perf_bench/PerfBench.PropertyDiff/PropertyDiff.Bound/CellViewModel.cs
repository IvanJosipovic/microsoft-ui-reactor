using System.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace PerfBench.PropertyDiff.Bound;

public sealed class CellViewModel : INotifyPropertyChanged
{
    private static readonly SolidColorBrush GreenBrush = new(Microsoft.UI.Colors.Green);
    private static readonly SolidColorBrush RedBrush = new(Microsoft.UI.Colors.Red);

    private int _index;
    private string _displayText = string.Empty;
    private SolidColorBrush _cellBrush = GreenBrush;

    public event PropertyChangedEventHandler? PropertyChanged;

    public CellViewModel(int index)
    {
        _index = index;
        _displayText = $"Cell {index}: 0.00";
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

    public SolidColorBrush CellBrush
    {
        get => _cellBrush;
        set
        {
            if (ReferenceEquals(_cellBrush, value)) return;
            _cellBrush = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CellBrush)));
        }
    }

    public void Update(double newValue)
    {
        DisplayText = $"Cell {_index}: {newValue:F2}";
        CellBrush = newValue >= 50.0 ? GreenBrush : RedBrush;
    }
}
