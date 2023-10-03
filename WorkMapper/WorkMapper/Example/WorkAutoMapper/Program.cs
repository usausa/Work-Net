using System;

namespace WorkAutoMapper
{
    using System.Collections.Generic;

    using AutoMapper;

    class Program
    {
        static void Main()
        {
            var a = Nullable.GetUnderlyingType(typeof(int));

            NullableTest.Run();
            //TestNestedSame.Test();
        }
    }

    public class TestNestedSame
    {
        public static void Test()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<Source, Destination>();
            });
            var mapper = config.CreateMapper();

            var source = new Source { Inner = new Inner { Value = 1 } };
            var destination =  mapper.Map<Destination>(source);
            var same = destination.Inner == source.Inner;

            var d2 = mapper.Map<Destination>((Source)null);
            var d3 = new Destination();
            mapper.Map<Source, Destination>(null, d3);
            mapper.Map<Source, Destination>(null, null);
        }

        public class Inner
        {
            public int Value { get; set; }
        }


        public class Source
        {
            public Inner? Inner { get; set; }
        }

        public class Destination
        {
            public Inner? Inner { get; set; }
        }
    }


    public class TestNestedEnumerable
    {
        public static void Test()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<InnerSource, InnerDestination>();

                c.CreateMap<ArraySource, ArrayDestination>();
                c.CreateMap<ArraySource, ListDestination>();
                c.CreateMap<ArraySource, IListDestination>();
                c.CreateMap<ArraySource, IEnumerableDestination>();
                c.CreateMap<ArraySource, ISetDestination>();
                c.CreateMap<ListSource, ArrayDestination>();
                c.CreateMap<ListSource, ListDestination>();
                c.CreateMap<ListSource, IListDestination>();
                c.CreateMap<ListSource, IEnumerableDestination>();
                c.CreateMap<InterfaceListSource, ArrayDestination>();
                c.CreateMap<InterfaceListSource, ListDestination>();
                c.CreateMap<InterfaceListSource, IListDestination>();
                c.CreateMap<InterfaceListSource, IEnumerableDestination>();
                c.CreateMap<EnumerableSource, ArrayDestination>();
                c.CreateMap<EnumerableSource, ListDestination>();
                c.CreateMap<EnumerableSource, IListDestination>();
                c.CreateMap<EnumerableSource, IEnumerableDestination>();
            });
            var mapper = config.CreateMapper();

            var innerSource = new[] {new InnerSource {Value = 1}};

            // Array to
            // ok
            var da1 =  mapper.Map<ArrayDestination>(new ArraySource { Inner = innerSource });
            // ok
            var dl1 =  mapper.Map<ListDestination>(new ArraySource { Inner = innerSource });
            // ok List
            var dil1 =  mapper.Map<IListDestination>(new ArraySource { Inner = innerSource });
            // ok List
            var die1 =  mapper.Map<IEnumerableDestination>(new ArraySource { Inner = innerSource });
            // ok HashSet
            var dis =  mapper.Map<ISetDestination>(new ArraySource { Inner = innerSource });

            // List to
            // ok
            var da2 =  mapper.Map<ArrayDestination>(new ListSource { Inner = new List<InnerSource>(innerSource) });
            // ok
            var dl2 =  mapper.Map<ListDestination>(new ListSource { Inner = new List<InnerSource>(innerSource) });
            // ok List
            var dil2 =  mapper.Map<IListDestination>(new ListSource { Inner = new List<InnerSource>(innerSource) });
            // ok List
            var die2 =  mapper.Map<IEnumerableDestination>(new ListSource { Inner = new List<InnerSource>(innerSource) });
            // NG
            //var dis2 =  mapper.Map<ISetDestination>(new ListSource { Inner = new List<InnerSource>(innerSource) });

            var da3 =  mapper.Map<ArrayDestination>(new InterfaceListSource { Inner = new List<InnerSource>(innerSource) });
            var dl3 =  mapper.Map<ListDestination>(new InterfaceListSource { Inner = new List<InnerSource>(innerSource) });
            var dil3 =  mapper.Map<IListDestination>(new InterfaceListSource { Inner = new List<InnerSource>(innerSource) });
            var die3 =  mapper.Map<IEnumerableDestination>(new InterfaceListSource { Inner = new List<InnerSource>(innerSource) });
            //var dis3 =  mapper.Map<ISetDestination>(new InterfaceListSource { Inner = new List<InnerSource>(innerSource) });

            var da4 =  mapper.Map<ArrayDestination>(new EnumerableSource { Inner = new List<InnerSource>(innerSource) });
            var dl4 =  mapper.Map<ListDestination>(new EnumerableSource { Inner = new List<InnerSource>(innerSource) });
            var dil4 =  mapper.Map<IListDestination>(new EnumerableSource { Inner = new List<InnerSource>(innerSource) });
            var die4 =  mapper.Map<IEnumerableDestination>(new EnumerableSource { Inner = new List<InnerSource>(innerSource) });
            //var dis4 =  mapper.Map<ISetDestination>(new EnumerableSource { Inner = new List<InnerSource>(innerSource) });
        }

        public class InnerSource
        {
            public int Value { get; set; }
        }

        public class InnerDestination
        {
            public int Value { get; set; }
        }

        public class ArraySource
        {
            public InnerSource[]? Inner { get; set; }
        }

        public class ListSource
        {
            public List<InnerSource>? Inner { get; set; }
        }

        public class InterfaceListSource
        {
            public IList<InnerSource>? Inner { get; set; }
        }

        public class EnumerableSource
        {
            public IEnumerable<InnerSource>? Inner { get; set; }
        }

        public class ArrayDestination
        {
            public InnerDestination[]? Inner { get; set; }
        }

        public class ListDestination
        {
            public List<InnerDestination>? Inner { get; set; }
        }

        public class IListDestination
        {
            public IList<InnerDestination>? Inner { get; set; }
        }

        public class IEnumerableDestination
        {
            public IEnumerable<InnerDestination>? Inner { get; set; }
        }

        public class ISetDestination
        {
            public ISet<InnerDestination>? Inner { get; set; }
        }
    }

    public class TestNestedArray
    {
        public static void Test()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<InnerSource, InnerDestination>();
                c.CreateMap<Source, Destination>();
            });
            var mapper = config.CreateMapper();

            // destination1.Inner.Length = 1 & [0].Value = 1
            var destination1 =  mapper.Map<Destination>(new Source { Inner = new[] { new InnerSource { Value = 1 } } });

            // destination2.Inner is Empty array!
            var destination2 = new Destination { Inner = new[] { new InnerDestination { Value = -1 } } };
            mapper.Map(new Source(), destination2);
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

    public class TestSpecialSequence
    {
        public static void Test()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<Source, Destination>();
                c.CreateMap<Source, Destination2>();
            });
            var mapper = config.CreateMapper();

            // '1', '2', '3'
            var destination1 =  mapper.Map<Destination>(new Source { Value = "123" });

            // AutoMapperMappingException: 'Error mapping types.'
            //var destination2 =  mapper.Map<Destination2>(new Source { Value = "123" });
        }

        public class Source
        {
            public string? Value { get; set; }
        }

        public class Destination
        {
            public char[]? Value { get; set; }
        }

        public class Destination2
        {
            public int[]? Value { get; set; }
        }
    }

    public class TestArray
    {
        public static void Test()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<Source, Destination>();
            });
            var mapper = config.CreateMapper();

            // destination1.Values = { 1, null, 3 }
            var destination1 =  mapper.Map<Destination>(new Source { Values = new int?[] { 1, null, 3 } });

            // destination2.Values is Empty array (not null!)
            var destination2 = new Destination { Values = new long?[] { 0 } };
            mapper.Map(new Source(), destination2);
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

    public class TestNestedToStruct
    {
        public static void Test()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<InnerSource, InnerDestination>();
                c.CreateMap<Source, Destination>();
            });
            var mapper = config.CreateMapper();

            // destination1.Inner.Value = 1
            var destination1 =  mapper.Map<Destination>(new Source { Inner = new InnerSource { Value = 1 } });

            // destination2.Inner.Value = 0
            var destination2 = new Destination { Inner = new InnerDestination { Value = -1 } };
            mapper.Map(new Source(), destination2);
        }

        public class InnerSource
        {
            public int Value { get; set; }
        }

        public struct InnerDestination
        {
            public int Value { get; set; }
        }

        public class Source
        {
            public InnerSource? Inner { get; set; }
        }

        public class Destination
        {
            public InnerDestination Inner { get; set; }
        }
    }

    public class TestNested
    {
        public static void Test()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<InnerSource, InnerDestination>();
                c.CreateMap<Source, Destination>();
            });
            var mapper = config.CreateMapper();

            // destination1.Inner.Value = 1
            var destination1 =  mapper.Map<Destination>(new Source { Inner = new InnerSource { Value = 1 } });

            // destination2.Inner is null
            var destination2 = new Destination { Inner = new InnerDestination { Value = -1 } };
            mapper.Map(new Source(), destination2);
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
            public InnerSource? Inner { get; set; }
        }

        public class Destination
        {
            public InnerDestination? Inner { get; set; }
        }
    }

    public class TestNullIfWithConversion
    {
        public static void Test()
        {
            // System.InvalidOperationException: 'No coercion operator is defined
            // same type only ?
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<int, string?>().ConvertUsing(x => null);
                c.CreateMap<Source, Destination>().ForMember(x => x.Value, opt => opt.NullSubstitute("x"));
            });
            var mapper = config.CreateMapper();

            var d = mapper.Map<Destination>(new Source());
            d = mapper.Map<Destination>(new Source { Value = 1 });
        }

        public class Source
        {
            public int Value { get; set; }
        }

        public class Destination
        {
            public string? Value { get; set; }
        }

        public class SourceToNullStringConverter : IValueConverter<Source, string?>
        {
            public string? Convert(Source sourceMember, ResolutionContext context)
            {
                return null;
            }
        }
    }

    public class TestNullIf
    {
        public static void Test()
        {
            var config = new MapperConfiguration(c =>
            {
                // InvalidOperationException id Source int Value
                c.CreateMap<Source, Destination>()
                    .ForMember(x => x.Value, opt => opt.NullSubstitute(2));
            });
            var mapper = config.CreateMapper();

            var d = mapper.Map<Destination>(new Source());
            d = mapper.Map<Destination>(new Source { Value = 1 });
        }

        public class Source
        {
            public int? Value { get; set; }
        }

        public class Destination
        {
            public long? Value { get; set; }
        }
    }

    public class TestCanNotConvert
    {
        public static void Test()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<Source, Destination>();
                c.CreateMap<Source, Destination2>();
            });
            var mapper = config.CreateMapper();

            // AutoMapperMappingException: 'Error mapping types.'
            //var d = mapper.Map<Destination>(new Source { Value = "x" });
            //var d2 = mapper.Map<Destination2>(new Source { Value = "x" });
        }

        public class Source
        {
            public string? Value { get; set; }
        }

        public class Destination
        {
            public int Value { get; set; }
        }

        public class Destination2
        {
            public int? Value { get; set; }
        }
    }

    public class TestSourceNullToNonNull
    {
        public static void Test()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<Source, Destination>();
            });
            var mapper = config.CreateMapper();

            // Null to default(0)
            var d = new Destination { Value = 1 };
            mapper.Map(new Source(), d);
        }

        public class Source
        {
            public int? Value { get; set; }
        }

        public class Destination
        {
            public int Value { get; set; }
        }
    }

    public class TestConversion
    {
        public static void Test()
        {
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<long, int>().ConstructUsing(x => (int)x + 1);
                c.CreateMap<long?, int>().ConstructUsing(x => x.HasValue ? (int)x + 2 : -1);
                c.CreateMap<long?, int?>().ConstructUsing(x => x.HasValue ? (int)x + 3 : -2);
                c.CreateMap<long, int?>().ConstructUsing(x => (int)x + 4);
                c.CreateMap<Source, Destination>();
            });
            var mapper = config.CreateMapper();

            var d = mapper.Map<Source, Destination>(new Source { Value = 1 });
        }

        public class Source
        {
            public long? Value { get; set; }
        }

        public class Destination
        {
            public int Value { get; set; }
        }
    }
}
