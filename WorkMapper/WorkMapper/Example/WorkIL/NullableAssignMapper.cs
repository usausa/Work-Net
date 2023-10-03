using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkIL
{
    public class NullableAssignMapper
    {
        public Func<int?, int> converter;

        public void Map(Source s, Destination d)
        {
            d.Value = converter(s.Value);
        }


        public class Source
        {
            public int Value { get; set; }
        }

        public class Destination
        {
            public int? Value { get; set; }
        }
    }
}
