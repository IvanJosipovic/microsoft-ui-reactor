using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PerfBench.Priorities.Bound;

public sealed class SearchViewModel : INotifyPropertyChanged
{
    private readonly string[] _allItems;
    private string _searchText = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SearchItemViewModel> FilteredItems { get; } = new();

    public SearchViewModel(string[] allItems)
    {
        _allItems = allItems;
        ApplyFilter();
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
            ApplyFilter();
        }
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();
        string filter = _searchText.ToLowerInvariant();
        foreach (var item in _allItems)
        {
            if (string.IsNullOrEmpty(filter) || item.Contains(filter, StringComparison.OrdinalIgnoreCase))
                FilteredItems.Add(new SearchItemViewModel(item));
        }
    }
}
