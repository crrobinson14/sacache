namespace SACache
{
    public interface IHashGenerator<T>
    {
        int getHashCode(T obj);
    }
}
