namespace WorkEnumerableConverter
{
    using System.Collections.Generic;

    using Xunit;

    public class SameMyEnumerableTest
    {
        [Fact]
        public void MyEnumerableToArray()
        {
            var factory = new Factory();
            var converter = factory.Create<MyEnumerable<int>, int[]>();

            var source = new MyEnumerable<int>(new[] { 1, 2, 3 });
            var destination = converter(source);

            Assert.Equal(3, destination.Length);
            Assert.Equal(1, destination[0]);
            Assert.Equal(2, destination[1]);
            Assert.Equal(3, destination[2]);
        }

        [Fact]
        public void MyEnumerableToList()
        {
            var factory = new Factory();
            var converter = factory.Create<MyEnumerable<int>, List<int>>();

            var source = new MyEnumerable<int>(new[] { 1, 2, 3 });
            var destination = converter(source);

            Assert.Equal(3, destination.Count);
            Assert.Equal(1, destination[0]);
            Assert.Equal(2, destination[1]);
            Assert.Equal(3, destination[2]);
        }

        [Fact]
        public void MyEnumerableStructToArray()
        {
            var factory = new Factory();
            var converter = factory.Create<MyEnumerableStruct<int>, int[]>();

            var source = new MyEnumerableStruct<int>(new[] { 1, 2, 3 });
            var destination = converter(source);

            Assert.Equal(3, destination.Length);
            Assert.Equal(1, destination[0]);
            Assert.Equal(2, destination[1]);
            Assert.Equal(3, destination[2]);
        }

        [Fact]
        public void MyEnumerableStructToList()
        {
            var factory = new Factory();
            var converter = factory.Create<MyEnumerableStruct<int>, List<int>>();

            var source = new MyEnumerableStruct<int>(new[] { 1, 2, 3 });
            var destination = converter(source);

            Assert.Equal(3, destination.Count);
            Assert.Equal(1, destination[0]);
            Assert.Equal(2, destination[1]);
            Assert.Equal(3, destination[2]);
        }
    }
}
