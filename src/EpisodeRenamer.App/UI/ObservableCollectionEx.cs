using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace EpisodeRenamer.App.UI;

/// <summary>
/// ObservableCollection مع دعم إضافة دفعات.
/// </summary>
public class ObservableCollectionEx<T> : ObservableCollection<T>
{
    /// <summary>
    /// يضيف مجموعة عناصر إلى المجموعة دفعة واحدة.
    /// </summary>
    /// <remarks>
    /// يضيف جميع العناصر داخلياً إلى <see cref="Collection{T}.Items"/> ثم يطلق
    /// حدث <see cref="NotifyCollectionChangedAction.Reset"/> مرة واحدة فقط.
    /// هذا يقلل عدد إشعارات <see cref="INotifyCollectionChanged"/> من N إلى 1،
    /// مما يسمح لـ <see cref="System.Windows.Data.CollectionView"/> في WPF
    /// بإجراء الفرز والتجميع مرة واحدة بدلاً من إعادة المعالجة بعد كل إضافة.
    /// </remarks>
    /// <param name="items">العناصر المراد إضافتها. يتم تجاهلها إذا كانت null أو فارغة.</param>
    public void AddRange(IEnumerable<T> items)
    {
        if (items is null) return;

        // جمع العناصر أولاً لتجنب التكرار أثناء الإضافة
        List<T> list = items as List<T> ?? new List<T>(items);
        if (list.Count == 0) return;

        CheckReentrancy();

        foreach (T item in list)
            Items.Add(item);

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
