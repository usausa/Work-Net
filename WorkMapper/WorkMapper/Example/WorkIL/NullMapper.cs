using System;

using WorkMapper;
using WorkMapper.Functions;

namespace WorkIL
{
    public sealed class NullIgnoreMapper
    {
        public void Map(NullableSource source, NullableDestination destination)
        {
            int? localValue = source.Value;
            if (localValue is not null)
            {
                destination.Value = localValue;
            }

            string? classValue = source.ClassValue;
            if (classValue is not null)
            {
                destination.ClassValue = classValue;
            }
        }
    }

    public sealed class NullIfMapper
    {
        public int? nullValue1;

        public string? nullValue2;

        public void Map1(NullableSource source, NullableDestination destination)
        {
            destination.Value = source.Value ?? nullValue1;
        }

        public void Map2(NullableSource source, NullableDestination destination)
        {
            destination.ClassValue = source.ClassValue ?? nullValue2;
        }
    }
}
