namespace WorkEnumerableConverter
{
    using System.Collections.Generic;

    using Xunit;

    public class SameMyListTest
    {
        [Fact]
        public void MyListToArray()
        {
            var factory = new Factory();
            var converter = factory.Create<MyList<int>, int[]>();

            var source = new MyList<int>(new[] { 1, 2, 3 });
            var destination = converter(source);

            Assert.Equal(3, destination.Length);
            Assert.Equal(1, destination[0]);
            Assert.Equal(2, destination[1]);
            Assert.Equal(3, destination[2]);
        }

        [Fact]
        public void MyListToList()
        {
            var factory = new Factory();
            var converter = factory.Create<MyList<int>, List<int>>();

            var source = new MyList<int>(new[] { 1, 2, 3 });
            var destination = converter(source);

            Assert.Equal(3, destination.Count);
            Assert.Equal(1, destination[0]);
            Assert.Equal(2, destination[1]);
            Assert.Equal(3, destination[2]);
        }

        [Fact]
        public void MyListStructToArray()
        {
            var factory = new Factory();
            var converter = factory.Create<MyListStruct<int>, int[]>();

            var source = new MyListStruct<int>(new[] { 1, 2, 3 });
            var destination = converter(source);

            Assert.Equal(3, destination.Length);
            Assert.Equal(1, destination[0]);
            Assert.Equal(2, destination[1]);
            Assert.Equal(3, destination[2]);
        }

        [Fact]
        public void MyListStructToList()
        {
            var factory = new Factory();
            var converter = factory.Create<MyListStruct<int>, List<int>>();

            var source = new MyListStruct<int>(new[] { 1, 2, 3 });
            var destination = converter(source);

            Assert.Equal(3, destination.Count);
            Assert.Equal(1, destination[0]);
            Assert.Equal(2, destination[1]);
            Assert.Equal(3, destination[2]);
        }
    }
}
