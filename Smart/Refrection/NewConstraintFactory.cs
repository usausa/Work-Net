namespace Smart.Refrection
{
    public static class NewConstraintFactory<T>
        where T : new()
    {
        public static T Create()
        {
            return new T();
        }
    }
}
