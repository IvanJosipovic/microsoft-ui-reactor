using System.ComponentModel;

namespace PerfBench.DeferredMount.Bound;

public sealed class TabItemViewModel : INotifyPropertyChanged
{
    private string _displayText;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TabItemViewModel(int tabIndex, int itemIndex)
    {
        _displayText = $"Tab {tabIndex} - Item {itemIndex}";
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
