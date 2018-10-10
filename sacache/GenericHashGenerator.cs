namespace SACache
{
    class GenericHashGenerator<T> : IHashGenerator<T>
    {
        public int getHashCode(T obj) {
            return obj.GetHashCode();
        }
    }
}
