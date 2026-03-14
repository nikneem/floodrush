using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace HexMaster.FloodRush.Game.ViewModels;

/// <summary>
/// An <see cref="ObservableCollection{T}"/> that supports replacing the entire collection
/// in one operation, raising a single <see cref="NotifyCollectionChangedAction.Reset"/>
/// notification instead of one per item.
/// </summary>
/// <remarks>
/// The default <see cref="ObservableCollection{T}"/> fires one
/// <see cref="NotifyCollectionChangedAction.Add"/> event per item, which causes any
/// subscriber that rebuilds on every notification (e.g. <c>PlayfieldBoardView</c>)
/// to run an O(N²) number of rebuilds while loading a level. Using
/// <see cref="ResetTo"/> ensures exactly one rebuild regardless of board size.
/// </remarks>
public sealed class BatchObservableCollection<T> : ObservableCollection<T>
{
    /// <summary>
    /// Clears the collection, bulk-populates it from <paramref name="items"/>, and
    /// raises exactly one <see cref="NotifyCollectionChangedAction.Reset"/> notification.
    /// </summary>
    public void ResetTo(IEnumerable<T> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
