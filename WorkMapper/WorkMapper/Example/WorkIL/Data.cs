namespace WorkIL
{
    public struct Point
    {
        public int X;
        public int Y;
    }

    public class Source
    {
        public int Value { get; set; }

        public string? ClassValue { get; set; }

        public Point StructValue { get; set; }
    }

    public class Destination
    {
        public int Value { get; set; }

        public string? ClassValue { get; set; }

        public Point StructValue { get; set; }
    }

    public class NullableSource
    {
        public int? Value { get; set; }

        public string? ClassValue { get; set; }
    }

    public class NullableDestination
    {
        public int? Value { get; set; }

        public string? ClassValue { get; set; }
    }

    public struct StructSource
    {
        public int Value { get; set; }

        public string Value2 { get; set; }

        public Point StructValue { get; set; }
    }

    public struct StructDestination
    {
        public int Value { get; set; }

        public string Value2 { get; set; }

        public Point StructValue { get; set; }
    }
}
