using System.Collections.Generic;

namespace SACache
{
    public interface IEvictor<TKey, TVal>
    {
        int evict(CacheEntry<TKey, TVal>[] cacheEntries, int startIndex, int endIndex);
    }
}
