using Nelibur.ObjectMapper;

namespace WorkAutoMapper
{
    public static class TinyMapperTest
    {
        public static void Run()
        {
            //TestNestedArray.Test();
            TestArray.Test();
        }

        public class TestNestedArray
        {
            public static void Test()
            {
                TinyMapper.Bind<Source, Destination>();
                TinyMapper.Bind<InnerSource, InnerDestination>();

                // destination1.Inner.Length = 1 & [0].Value = 1
                var destination1 =  TinyMapper.Map<Destination>(new Source { Inner = new[] { new InnerSource { Value = 1 } } });

                // destination2.Inner is null
                var destination2 = new Destination { Inner = new[] { new InnerDestination { Value = -1 } } };
                TinyMapper.Map(new Source(), destination2);
            }

            public class InnerSource
            {
                public int Value { get; set; }
            }

            public class InnerDestination
            {
                public int Value { get; set; }
            }

            public class Source
            {
                public InnerSource[]? Inner { get; set; }
            }

            public class Destination
            {
                public InnerDestination[]? Inner { get; set; }
            }
        }

        public class TestArray
        {
            public static void Test()
            {
                TinyMapper.Bind<Source, Destination>();

                // destination1.Values = { 1, null, 3 }
                var destination1 =  TinyMapper.Map<Destination>(new Source { Values = new int?[] { 1, null, 3 } });

                // destination2.Values is Empty array (not null!)
                var destination2 = new Destination { Values = new long?[] { 0 } };
                TinyMapper.Map(new Source(), destination2);
            }

            public class Source
            {
                public int?[]? Values { get; set; }
            }

            public class Destination
            {
                public long?[]? Values { get; set; }
            }
        }
    }
}
