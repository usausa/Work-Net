using System;

using WorkMapper;
using WorkMapper.Functions;

namespace WorkIL
{
    public sealed class NullableMapper
    {
        public NullableDestinationInner? Map(NullableSourceInner? source)
        {
            if (source is null)
            {
                return null;
            }

            var destination = new NullableDestinationInner();
            destination.Value = source.Value.Value;
            return destination;
        }

        public void Map(NullableSourceInner? source, NullableDestinationInner? destination)
        {
            if (source is null)
            {
                return;
            }

            // ?
            var nullableDestinationInner = destination!.Value;
            nullableDestinationInner.Value = source.Value.Value;
        }

        public struct NullableSourceInner
        {
            public int Value { get; set; }
        }

        public struct NullableDestinationInner
        {
            public int Value { get; set; }
        }
    }
}
