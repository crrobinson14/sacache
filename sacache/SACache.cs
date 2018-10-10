using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SACache
{
    public class SACache<TKey, TVal>
    {
        CacheEntry<TKey, TVal>[] entries;
        IEvictor<TKey, TVal> evictor;
        IHashGenerator<TKey> hashGenerator;

        private uint setCount;
        private uint cacheSize;
        private uint linesPerSet;

        public ulong cacheHits { get; private set; }
        public ulong cacheMisses { get; private set; }
        public ulong cacheEvictions { get; private set; }

        // Convenience initializers for simplified/default behavior
        public SACache() : this(0, 0, null, null) { }
        public SACache(uint cacheSize, uint linesPerSet) : this(cacheSize, linesPerSet, null, null) { }

        /// <summary>
        /// Create and initialize an SACache.
        /// </summary>
        /// <param name="cacheSize">The number of entries the cache should track. Defaults to 32M entries.</param>
        /// <param name="linesPerSet">The number of lines per set (set-associative cardinality). Defaults to 4.</param>
        /// <param name="evictor">Cache-entry eviction routine. Defaults to LRU. A custom routine may be supplied via a
        /// a class that implements sacache.IEvictor.</param>
        /// <param name="hashGenerator">Hash generation mechanism for cache keys. Defaults to Object.GetHashCode().</param>
        public SACache(uint cacheSize, uint linesPerSet, IEvictor<TKey, TVal> evictor, IHashGenerator<TKey> hashGenerator) {
            // This is the default, but I like to be explicit in constructors. It helps to know they weren't forgotten...
            clearStats();

            this.cacheSize = cacheSize;
            if (this.cacheSize < 1) {
                Debug.WriteLine("[SACache] No cache size specified, using 32M.");
                this.cacheSize = 32 * 1024 * 1024;
            }

            this.linesPerSet = linesPerSet;
            if (this.linesPerSet < 1) {
                Debug.WriteLine("[SACache] No set-cardinality specified, configuring for N=4.");
                this.linesPerSet = 4;
            }

            if (this.cacheSize % this.linesPerSet != 0) {
                throw new System.ArgumentException("[SACache] Cache size must be an even multiple of set size.");
            }

            this.evictor = evictor;
            if (this.evictor == null) {
                Debug.WriteLine("[SACache] No cache evictor specified, using \"LRU\".");
                this.evictor = new LRUEvictor<TKey, TVal>();
            }

            this.hashGenerator = hashGenerator;
            if (this.hashGenerator == null) {
                Debug.WriteLine("[SACache] WARNING: No hash generator specified, using \"Object.GetHashCode()\". " +
                    "Mutable key types (complex objects) may not cache properly using this!");
                this.hashGenerator = new GenericHashGenerator<TKey>();
            }

            // We're a cache, not a database - pre-allocate everything we're going to need.
            setCount = this.cacheSize / this.linesPerSet;
            entries = new CacheEntry<TKey, TVal>[this.cacheSize];

            for (int i = 0; i < this.cacheSize; i++) {
                entries[i] = new CacheEntry<TKey, TVal>();
            }
        }

        /// <summary>
        /// Get a cached value.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>The cached value, or null if the value was not found (CACHE MISS).</returns>
        public TVal get(TKey key) {
            int tag = hashGenerator.getHashCode(key);

            int start, end;
            calculateBlockRange(tag, out start, out end);

            // NOTE: Did a crude test on "Parallel.For(start, end, i => {" to see if it was worth using. With its overhead,
            // on my machine, "N" needed to be >= 32 before it was actually faster. A set cardinality that high is almost
            // never the right balance.
            for (int i = start; i <= end; i++) {
                CacheEntry<TKey, TVal> entry = entries[i];

                if (entry.valid && entry.key.Equals(key)) {
                    cacheHits++;
                    entry.timestamp = getCurrentTime();
                    return entry.value;
                }
            };

            // Cache miss. We were told not to provide a backing store, so we just return default and let the caller handle it.
            // TODO: Talk to the team and see if we want to provide an (optional?) Interface to streamline this.
            cacheMisses++;

            return default(TVal);
        }

        /// <summary>
        /// Store a value in the cache.
        /// </summary>
        /// <param name="key">The key to use for later retrieval.</param>
        /// <param name="value">The value to store.</param>
        public void put(TKey key, TVal value) {
            int tag = hashGenerator.getHashCode(key);

            int start, end;
            calculateBlockRange(tag, out start, out end);

            int targetIndex = -1;
            for (int i = start; i <= end; i++) {
                CacheEntry<TKey, TVal> check = entries[i];
                if (check.valid) {
                    if (check.key.Equals(key)) {
                        // Key is already cached - update our value
                        check.value = value;
                        check.timestamp = getCurrentTime();
                        return;
                    }
                } else if (targetIndex == -1) {
                    targetIndex = i;
                }
            }

            // Key was not found - see if we need to evict
            if (targetIndex < 0) {
                cacheEvictions++;
                targetIndex = evictor.evict(entries, start, end);

                // Sanity check...
                if (targetIndex < start || targetIndex > end) {
                    throw new System.ArgumentException("[SACache] Evictor must return a value >= start && <= end");
                }
            }

            CacheEntry<TKey, TVal> entry = entries[targetIndex];
            entry.update(key, value, getCurrentTime());
        }

        /// <summary>
        /// Clear the usage statistics for the cache.
        /// </summary>
        public void clearStats() {
            cacheHits = cacheMisses = cacheEvictions = 0;
        }

        // Can't wait for named tuples!
        private void calculateBlockRange(int key, out int start, out int end) {
            // This is actually not strictly the "real" way to do it. If we were making a
            // CPU N-way, each set would map to a physical memory range and be strongly tied
            // to it. But that requires knowing the upper-bound (physical memory size) on the
            // set of possible keys. Our own keys here are arbitrary. So here we're modding to
            // just keep / realign them to the range of cache sets. So we won't have things like
            // "block offsets" but we can still be effective with the algorithm itself... 
            start = key % (int) cacheSize - key % (int) linesPerSet;
            end = start + (int) linesPerSet - 1;
        }

        private long getCurrentTime() {
            return DateTime.Now.Ticks;
        }
    }
}
