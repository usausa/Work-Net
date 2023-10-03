namespace WorkMapper
{
    public static class Program
    {
        public static void Main()
        {
            var config = new MapperConfig()
                .AddDefaultMapper();
            //config.CreateMap<SourceData, DestinationData>();

            var mapper = config.ToMapper();

            var destination = mapper.Map<SourceData, DestinationData>(new SourceData { Value = 1 });
            mapper.Map(new SourceData { Value = 2 }, destination);
            var destination2 = mapper.Map<SourceData, DestinationData>(new SourceData { Value = 3 }, string.Empty);
            mapper.Map(new SourceData { Value = 4 }, destination2, string.Empty);
        }
    }

    public class SourceData
    {
        public int Value { get; set; }
    }

    public class DestinationData
    {
        public int Value { get; set; }
    }
}
