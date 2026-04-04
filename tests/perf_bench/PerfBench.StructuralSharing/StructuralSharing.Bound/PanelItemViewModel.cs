using System.ComponentModel;

namespace PerfBench.StructuralSharing.Bound;

public sealed class PanelItemViewModel : INotifyPropertyChanged
{
    private int _index;
    private string _displayText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public PanelItemViewModel(int index)
    {
        _index = index;
        _displayText = $"Item {index}: 0.00";
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

    public void Update(double newValue)
    {
        DisplayText = $"Item {_index}: {newValue:F2}";
    }
}
