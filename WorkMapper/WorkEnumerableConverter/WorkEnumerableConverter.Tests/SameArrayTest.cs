namespace WorkEnumerableConverter
{
    using System.Collections.Generic;

    using Xunit;

    public class SameArrayTest
    {
        [Fact]
        public void ArrayToArray()
        {
            var factory = new Factory();
            var converter = factory.Create<int[], int[]>();

            var source = new[] { 1, 2, 3 };
            var destination = converter(source);

            Assert.Equal(3, destination.Length);
            Assert.Equal(1, destination[0]);
            Assert.Equal(2, destination[1]);
            Assert.Equal(3, destination[2]);
            Assert.NotSame(source, destination);
        }

        [Fact]
        public void ArrayToList()
        {
            var factory = new Factory();
            var converter = factory.Create<int[], List<int>>();

            var source = new[] { 1, 2, 3 };
            var destination = converter(source);

            Assert.Equal(3, destination.Count);
            Assert.Equal(1, destination[0]);
            Assert.Equal(2, destination[1]);
            Assert.Equal(3, destination[2]);
        }
    }
}
