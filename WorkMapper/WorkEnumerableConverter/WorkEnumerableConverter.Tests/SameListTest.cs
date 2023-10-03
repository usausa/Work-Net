namespace WorkEnumerableConverter
{
    using System.Collections.Generic;

    using Xunit;

    public class SameListTest
    {
        [Fact]
        public void ListToArray()
        {
            var factory = new Factory();
            var converter = factory.Create<List<int>, int[]>();

            var source = new List<int>(new[] { 1, 2, 3 });
            var destination = converter(source);

            Assert.Equal(3, destination.Length);
            Assert.Equal(1, destination[0]);
            Assert.Equal(2, destination[1]);
            Assert.Equal(3, destination[2]);
        }

        [Fact]
        public void ListToList()
        {
            var factory = new Factory();
            var converter = factory.Create<List<int>, List<int>>();

            var source = new List<int>(new[] { 1, 2, 3 });
            var destination = converter(source);

            Assert.Equal(3, destination.Count);
            Assert.Equal(1, destination[0]);
            Assert.Equal(2, destination[1]);
            Assert.Equal(3, destination[2]);
            Assert.NotSame(source, destination);
        }
    }
}
