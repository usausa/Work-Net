using System;
using System.Diagnostics;

using AutoMapper;

using Nelibur.ObjectMapper;

namespace WorkEnumerableInstance
{
    class Program
    {
        static void Main(string[] args)
        {
            // Auto
            var config = new MapperConfiguration(c =>
            {
                c.CreateMap<Source, Destination>();
            });
            var mapper = config.CreateMapper();

            var source = new Source { Values = new[] { 1, 2, 3 } };
            var d1 = mapper.Map<Destination>(source);
            Debug.WriteLine(source.Values == d1.Values);

            // Tiny
            TinyMapper.Bind<Source, Destination>();

            var d2 = TinyMapper.Map<Source, Destination>(source);
            Debug.WriteLine(source.Values == d2.Values);
        }
    }

    public class Source
    {
        public int[] Values { get; set; }
    }

    public class Destination
    {
        public int[] Values { get; set; }
    }
}
