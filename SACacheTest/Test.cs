using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SACache;

namespace SACacheTest
{
    [TestClass]
    public class Test
    {
        [TestMethod]
        public void SimpleSetup() {
            SACache<int, string> cache = new SACache<int, string>();

            // We should be able to store an item
            cache.put(0, "Test");
            Assert.AreEqual(cache.get(0), "Test");

            // And we should be able to replace it in-place.
            cache.put(0, "Replacement");
            Assert.AreEqual(cache.get(0), "Replacement");

            Assert.AreEqual(cache.cacheHits, (ulong)2);
            Assert.AreEqual(cache.cacheMisses, (ulong)0);
            Assert.AreEqual(cache.cacheEvictions, (ulong)0);
        }

        [TestMethod]
        public void PartialSetup() {
            SACache<int, string> cache = new SACache<int, string>(4, 2);

            // With 4 lines, 2 per set, we have two sets. That means we should be able
            // to store 2 objects, while the third pushes something out. Yet a higher
            // address does NOT push anything out.
            cache.put(0, "Test");
            cache.put(1, "Test2");
            cache.put(4, "Test3");

            Assert.AreEqual(cache.get(0), null); // Should have been evicted. SACache defaults to LRU.
            Assert.AreEqual(cache.get(4), "Test3");
            Assert.AreEqual(cache.get(1), "Test2");
            Assert.AreEqual(cache.get(2), null);

            Assert.AreEqual(cache.cacheHits, (ulong)2);
            Assert.AreEqual(cache.cacheMisses, (ulong)2);
            Assert.AreEqual(cache.cacheEvictions, (ulong)1);
        }

        [TestMethod]
        public void CompleteSetup() {
            // Test using a complex object for both key and value. Since we're implementing an optional
            // hash generator here, this also is test/proof that we can use an object as its own key AND
            // value. What's neat about that is we could use more sophisticated hashing mechanisms without
            // updating every spot in our code, as we would if we had to pre-extract and provide a single
            // key to the alg. For example, suppose we have an employee{id, name, ssn, dob, ...} object to
            // store. Initially we might key off the user.id field, but maybe that's a lengthy GUID. What
            // if we later determine it's acceptable to key off the SSN? We'd have to update all of our code
            // to implement that change, and risk a bug. Here, we can simply swap out the hashing routine
            // when the cache is initialized.
            SACache<TestType, TestType> cache = new SACache<TestType, TestType>(4, 2, new TestEvictor(), new TestHashGenerator());

            var t1 = new TestType(0, "Test");
            cache.put(t1, t1);

            var t2 = new TestType(1, "Test2");
            cache.put(t2, t2);

            var t3 = new TestType(2, "Test3");

            Assert.AreEqual(cache.get(t1), t1);
            Assert.AreEqual(cache.get(t2), t2);
            Assert.AreNotEqual(cache.get(t2), t1);
            Assert.IsNull(cache.get(t3));

            Assert.AreEqual(cache.cacheHits, (ulong)3);
            Assert.AreEqual(cache.cacheMisses, (ulong)1);
            Assert.AreEqual(cache.cacheEvictions, (ulong)0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidCacheSize() {
            SACache<int, string> cache = new SACache<int, string>(1, 2);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void MisbehavedEvictor() {
            SACache<int, string> cache = new SACache<int, string>(4, 2, new BadEvictor(), null);
            cache.put(0, "Test");
            cache.put(1, "Test");
            cache.put(4, "Test"); // Should evict
        }
    }

    class TestType
    {
        public int id;
        public string value;

        public TestType(int id, string value) {
            this.id = id;
            this.value = value;
        }
    }

    class TestEvictor : IEvictor<TestType, TestType>
    {
        public int callCount = 0;

        public int evict(CacheEntry<TestType, TestType>[] cacheEntries, int startIndex, int endIndex) {
            callCount++;
            return startIndex;
        }
    }

    class TestHashGenerator : IHashGenerator<TestType>
    {
        public int callCount = 0;

        public int getHashCode(TestType obj) {
            callCount++;
            return obj.id;
        }
    }

    class BadEvictor : IEvictor<int, string>
    {
        public int evict(CacheEntry<int, string>[] cacheEntries, int startIndex, int endIndex) {
            return endIndex + 1;
        }
    }
}
