using System;

namespace WorkIL
{
    public class NestedMapper
    {
        public Func<SourceInner, DestinationInner> nestedClasMapper;

        public Func<StructSourceInner?, StructDestinationInner?> nestedNullableMapper;

        public Func<StructSourceInner, StructDestinationInner> nestedStructMapper;


        public void MapClass(Source s, Destination d)
        {
            var temp = s.Inner;
            if (temp is not null)
            {
                d.Inner = nestedClasMapper(temp);
            }
            else
            {
                d.Inner = null;
            }
        }

        public void MapClass2(Source s, Destination d)
        {
            var temp = s.Inner;
            d.Inner = temp is not null ? nestedClasMapper(temp) : null;
        }

        public void MapClass0(Source s, Destination d)
        {
            d.Inner = nestedClasMapper(s.Inner!);
        }

        public void MapNullable(Source s, Destination d)
        {
            var temp = s.NullableInner;
            if (s.NullableInner is not null)
            {
                d.NullableInner = nestedNullableMapper(temp);
            }
            else
            {
                d.NullableInner = null;
            }
        }

        public void MapNullable2(Source s, Destination d)
        {
            var temp = s.NullableInner;
            d.NullableInner = temp is not null ? nestedNullableMapper(temp) : null;
        }

        public void MapStruct(Source s, Destination d)
        {
            d.StructInner = nestedStructMapper(s.StructInner);
        }

        public struct StructSourceInner
        {
            public int Value { get; set; }
        }

        public struct StructDestinationInner
        {
            public int Value { get; set; }
        }

        public class SourceInner
        {
            public int Value { get; set; }
        }

        public class DestinationInner
        {
            public int Value { get; set; }
        }

        public class Source
        {
            public SourceInner? Inner { get; set; }

            public StructSourceInner? NullableInner { get; set; }

            public StructSourceInner StructInner { get; set; }
        }

        public class Destination
        {
            public DestinationInner? Inner { get; set; }

            public StructDestinationInner? NullableInner { get; set; }

            public StructDestinationInner StructInner { get; set; }
        }
    }
}
