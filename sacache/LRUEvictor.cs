using System.Collections.Generic;

namespace SACache
{
    class LRUEvictor<TKey, TVal> : IEvictor<TKey, TVal>
    {
        public int evict(CacheEntry<TKey, TVal>[] cacheEntries, int startIndex, int endIndex) {
            int lruIndex = startIndex;
            long lruTimestamp = cacheEntries[startIndex].timestamp;
            for (int i = startIndex; i <= endIndex; i++) {
                long currentTimestamp = cacheEntries[i].timestamp;
                if (lruTimestamp > currentTimestamp) {
                    // NOTE: Our test harness never tests this branch. As soon as we got over 80% I stopped
                    // adding tests cases - I figured the point was proven and it was enough to have a chat
                    // about the pros and cons of TDD.
                    lruIndex = i;
                    lruTimestamp = currentTimestamp;
                }
            }
            return lruIndex;
        }
    }
}
