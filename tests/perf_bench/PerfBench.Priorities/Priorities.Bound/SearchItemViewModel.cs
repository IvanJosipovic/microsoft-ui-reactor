using System.ComponentModel;

namespace PerfBench.Priorities.Bound;

public sealed class SearchItemViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public SearchItemViewModel(string text)
    {
        Text = text;
    }

    public string Text { get; }
}
