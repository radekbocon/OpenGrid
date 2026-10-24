using System.Collections.ObjectModel;

namespace OpenGrid;

public class ObservableSet<T> : ObservableCollection<T>
{
    protected override void InsertItem(int index, T item)
    {
        if (Items.Contains(item))
        {
            return;
        }
        
        base.InsertItem(index, item);
    }
}