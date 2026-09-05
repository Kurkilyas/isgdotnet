namespace InvoiceTrackingSystemBackend.Helpers;

/// <summary>
/// Boşluksuz 1,2,3... sıra. Create'te son+1, update'te kaydırma, delete'te sıkıştırma.
/// Unique index çakışmasın diye önce -id yazılır, kaydedilir, sonra 1..n verilir.
/// </summary>
public static class SequentialOrderHelper
{
    /// <summary>
    /// Sıradaki boş slot max+1'dir. İstemci 3 gönderip sıradaki 2 ise 2 döner.
    /// </summary>
    public static int NextAppendOrder(IEnumerable<int> existingOrders, int requestedOrder)
    {
        var max = 0;
        foreach (var order in existingOrders)
        {
            if (order > max)
            {
                max = order;
            }
        }

        var next = max + 1;
        return requestedOrder == next ? requestedOrder : next;
    }

    public static void AssignSequential<T>(IEnumerable<T> itemsInDesiredOrder, Action<T, int> setOrder)
    {
        var order = 1;
        foreach (var item in itemsInDesiredOrder)
        {
            setOrder(item, order++);
        }
    }

    /// <summary>
    /// Sıra değişmediyse (clamp sonrası aynıysa) false döner, kayıt yazılmaz.
    /// Değiştiyse diğerleri kayar: 1→2 giderse eski 2, 1 olur.
    /// </summary>
    public static async Task<bool> MoveAsync<T>(
        IList<T> items,
        T moving,
        int requestedOrder,
        Func<T, int> getOrder,
        Action<T, int> setOrder,
        Func<T, int> getId,
        Func<Task> saveAsync)
    {
        if (items.Count == 0)
        {
            return false;
        }

        var newOrder = Clamp(requestedOrder, items.Count);
        if (getOrder(moving) == newOrder)
        {
            return false;
        }

        var ordered = items.OrderBy(getOrder).ThenBy(getId).ToList();
        ordered.Remove(moving);
        ordered.Insert(newOrder - 1, moving);

        await WriteSequentialAsync(ordered, setOrder, getId, saveAsync);
        return true;
    }

    /// <summary>Kalan kayıtları 1..n yapar. Zaten sıralıysa dokunmaz.</summary>
    public static async Task CompactAsync<T>(
        IList<T> remainingItems,
        Func<T, int> getOrder,
        Action<T, int> setOrder,
        Func<T, int> getId,
        Func<Task> saveAsync)
    {
        if (remainingItems.Count == 0)
        {
            return;
        }

        var ordered = remainingItems.OrderBy(getOrder).ThenBy(getId).ToList();
        if (IsSequential(ordered, getOrder))
        {
            return;
        }

        await WriteSequentialAsync(ordered, setOrder, getId, saveAsync);
    }

    private static int Clamp(int requestedOrder, int count)
    {
        if (requestedOrder < 1)
        {
            return 1;
        }

        return requestedOrder > count ? count : requestedOrder;
    }

    private static bool IsSequential<T>(IList<T> ordered, Func<T, int> getOrder)
    {
        for (var i = 0; i < ordered.Count; i++)
        {
            if (getOrder(ordered[i]) != i + 1)
            {
                return false;
            }
        }

        return true;
    }

    private static async Task WriteSequentialAsync<T>(
        IList<T> ordered,
        Action<T, int> setOrder,
        Func<T, int> getId,
        Func<Task> saveAsync)
    {
        foreach (var item in ordered)
        {
            setOrder(item, -getId(item));
        }

        await saveAsync();

        for (var i = 0; i < ordered.Count; i++)
        {
            setOrder(ordered[i], i + 1);
        }

        await saveAsync();
    }
}
