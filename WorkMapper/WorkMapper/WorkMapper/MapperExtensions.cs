namespace WorkMapper
{
    using WorkMapper.Handlers;

    public static class MapperExtensions
    {
        //--------------------------------------------------------------------------------
        // Config
        //--------------------------------------------------------------------------------

        public static Mapper ToMapper(this MapperConfig config) => new(config);

        public static MapperConfig AddDefaultMapper(this MapperConfig config)
        {
            config.Configure(c =>
            {
                c.Add<IMissingHandler, DefaultMapperHandler>();
            });

            return config;
        }
    }
}
