namespace SACache
{
    public class CacheEntry<TKey, TVal>
    {
        public TKey key = default(TKey);
        public TVal value = default(TVal);
        public bool valid = false;
        public long timestamp = 0;

        // It might seem weird that we allow updates to the key but it's because we use this
        // routine to overwrite values, not just update existing ones.
        public void update(TKey key, TVal value, long timestamp) {
            this.key = key;
            this.value = value;
            this.timestamp = timestamp;

            this.valid = true;
        }
    }
}
