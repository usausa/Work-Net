using System;

using WorkMapper;
using WorkMapper.Functions;

namespace WorkIL
{
    public class ConvertSource
    {
        public bool ConverterValue { get; set; }

        public int? N2NValue { get; set; }
        public int? N2IValue { get; set; }
        public int I2NValue { get; set; }

        public double ExplicitValue { get; set; }
    }

    public class ConvertDestination
    {
        public int ConverterValue { get; set; }

        public int? N2NValue { get; set; }
        public int N2IValue { get; set; }
        public int? I2NValue { get; set; }

        public int ExplicitValue { get; set; }
    }

    public sealed class ConvertMapper1
    {
        public Func<bool, int> converter;

        public void Map(ConvertSource source, ConvertDestination destination)
        {
            destination.ConverterValue = converter(source.ConverterValue);
        }
    }

    public sealed class ConvertMapper2
    {
        public void Map(ConvertSource source, ConvertDestination destination)
        {
            destination.N2NValue = source.N2NValue;
            destination.I2NValue = source.I2NValue;
            destination.N2IValue = source.N2IValue ?? 0;
        }
    }

    public sealed class ConvertMapper3
    {
        public void Map(ConvertSource source, ConvertDestination destination)
        {
            destination.ExplicitValue = (int)source.ExplicitValue;
        }
    }
}
