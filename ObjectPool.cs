using System.Collections.Concurrent;

namespace PhiFansConverter;

/// <summary>
/// Simple object pool implementation to reduce GC pressure
/// </summary>
/// <typeparam name="T">Type of objects to pool</typeparam>
public class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentQueue<T> _objects = new();
    private readonly Func<T> _objectGenerator;
    private readonly Action<T>? _objectResetter;
    private readonly int _maxSize;
    private int _currentCount = 0;

    public ObjectPool(int maxSize = 100, Func<T>? objectGenerator = null, Action<T>? objectResetter = null)
    {
        _maxSize = maxSize;
        _objectGenerator = objectGenerator ?? (() => new T());
        _objectResetter = objectResetter;
    }

    public T Rent()
    {
        if (_objects.TryDequeue(out T? item))
        {
            Interlocked.Decrement(ref _currentCount);
            return item;
        }

        return _objectGenerator();
    }

    public void Return(T item)
    {
        if (item == null || _currentCount >= _maxSize)
            return;

        _objectResetter?.Invoke(item);
        
        _objects.Enqueue(item);
        Interlocked.Increment(ref _currentCount);
    }
}

/// <summary>
/// Static object pools for frequently used types
/// </summary>
public static class ObjectPools
{
    // Pool for List<Event> to reduce allocations during event processing
    public static readonly ObjectPool<List<RePhiEditObject.Event>> EventListPool = 
        new(maxSize: 50, objectResetter: list => list.Clear());
    
    // Pool for List<PhiFansObject.EventItem> for conversion operations
    public static readonly ObjectPool<List<PhiFansObject.EventItem>> PhiFansEventItemListPool = 
        new(maxSize: 50, objectResetter: list => list.Clear());
    
    // Pool for int[] arrays used in Beat conversion
    public static readonly ObjectPool<List<int[]>> BeatArrayListPool = 
        new(maxSize: 20, objectResetter: list => list.Clear());
    
    // Pool for coordinate calculation results
    public static readonly ObjectPool<List<(float x, float y)>> CoordinateListPool = 
        new(maxSize: 20, objectResetter: list => list.Clear());

    /// <summary>
    /// Clear all pools (useful for testing or memory cleanup)
    /// </summary>
    public static void ClearAllPools()
    {
        // Pools clear themselves automatically, but we could force clear if needed
        // For now, just let them manage themselves
    }
}