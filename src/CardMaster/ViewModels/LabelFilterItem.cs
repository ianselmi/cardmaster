namespace CardMaster.ViewModels;

/// <summary>
/// Un chip del filtro per label nella lista carte: il nome della label e se è attiva.
/// La selezione è multipla e in OR (una carta basta che abbia una delle label attive).
/// </summary>
public sealed class LabelFilterItem : ObservableObject
{
    private bool _isSelected;

    public LabelFilterItem(string name, bool isSelected = false)
    {
        Name = name;
        _isSelected = isSelected;
    }

    public string Name { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
