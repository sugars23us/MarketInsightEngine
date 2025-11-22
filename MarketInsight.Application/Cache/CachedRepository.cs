using System.Collections.Concurrent;

public sealed class CachedRepository<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    private readonly ConcurrentDictionary<TKey, Task<TValue>> _cache = new();

    public Task<TValue?> GetOrLoadAsync(
        TKey key,
        Func<TKey, CancellationToken, Task<TValue?>> loader,
        CancellationToken ct = default)
    {
        return _cache.GetOrAdd(key, k => loader(k, ct));
    }
}