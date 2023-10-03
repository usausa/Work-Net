using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkIL
{
    public class FieldMapper
    {
        public void MapFromSource(Source s, Destination d)
        {
            d.Value = s.Value;
        }

        public void MapFromStruct(StructSource s, Destination d)
        {
            d.Value = s.Value;
        }

        public void MapFromNestedField(NestedFieldSource s, Destination d)
        {
            d.Value = s.Source.Value;
        }

        public void MapFromNestedProperty(NestedPropertySource s, Destination d)
        {
            d.Value = s.Source.Value;
        }

        public class Source
        {
            public int Value;
        }

        public struct StructSource
        {
            public int Value;
        }

        public class NestedFieldSource
        {
            public Source Source;
        }

        public class NestedPropertySource
        {
            public Source Source { get; set; }
        }

        public class Destination
        {
            public int Value { get; set; }
        }
    }
}
