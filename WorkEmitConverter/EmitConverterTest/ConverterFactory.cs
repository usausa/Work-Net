namespace EmitConverterTest
{
    using System;
    using System.Reflection.Emit;

    using EmitConverter;

    public static class ConverterFactory
    {
        public static Func<TSource, TDestination> Create<TSource, TDestination>()
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var dynamicMethod = new DynamicMethod(string.Empty, destinationType, new[] { sourceType }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // Argument
            ilGenerator.Emit(OpCodes.Ldarg_0);

            // Convert
            if (!ilGenerator.EmitPrimitiveConvert(sourceType, destinationType))
            {
                throw new NotSupportedException();
            }

            // Return
            ilGenerator.Emit(OpCodes.Ret);

            return (Func<TSource, TDestination>)dynamicMethod.CreateDelegate(typeof(Func<TSource, TDestination>), null);
        }
    }
}
