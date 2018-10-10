using System.Collections.Generic;

// NOTE: Our test harness never tests this class. As soon as we got over 80% I stopped
// adding tests cases - I figured the point was proven and it was enough to have a chat
// about the pros and cons of TDD.

namespace SACache
{
    class MRUEvictor<TKey, TVal> : IEvictor<TKey, TVal>
    {
        public int evict(CacheEntry<TKey, TVal>[] cacheEntries, int startIndex, int endIndex) {
            int mruIndex = startIndex;
            long mruTimestamp = cacheEntries[startIndex].timestamp;
            for (int i = startIndex; i <= endIndex; i++) {
                long currentTimestamp = cacheEntries[i].timestamp;
                if (mruTimestamp < currentTimestamp) {
                    mruIndex = i;
                    mruTimestamp = currentTimestamp;
                }
            }
            return mruIndex;
        }
    }
}
