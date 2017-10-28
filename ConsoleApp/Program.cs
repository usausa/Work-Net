namespace ConsoleApp
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using Smart.Reflection;
    using Smart.Reflection.Emit;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var type0 = typeof(Class0);
            var type1 = typeof(Class1);
            var ctor0 = type0.GetConstructors().First();
            var ctor1 = type1.GetConstructors().First();

            var activator0 = CreateActivator(ctor0);
            var o = activator0.Create(null);
        }

        // Test

        // TODO Assembly?

        private static readonly AssemblyBuilder AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("DynamicActivator"),
            AssemblyBuilderAccess.RunAndSave);
            //AssemblyBuilderAccess.Run); // TODO

        private static readonly ModuleBuilder ModuleBuilder = AssemblyBuilder.DefineDynamicModule(
            "DynamicActivator",
            "test.dll");    // TODO remove

        private static readonly Type CtorType = typeof(ConstructorInfo);

        // TODO Attribute check

        public static IActivator CreateActivator(ConstructorInfo ci)
        {
            var typeBuilder = ModuleBuilder.DefineType(
                ci.DeclaringType.FullName + "_DynamicActivator",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);

            typeBuilder.AddInterfaceImplementation(typeof(IActivator));

            // Source
            var sourceField = typeBuilder.DefineField(
                "_source",
                CtorType,
                FieldAttributes.Private | FieldAttributes.InitOnly);
            var sourceProperty = typeBuilder.DefineProperty(
                "Source",
                PropertyAttributes.HasDefault,
                CtorType,
                null);
            var getSourceProperty = typeBuilder.DefineMethod(
                "get_Source",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
                CtorType,
                Type.EmptyTypes);
            sourceProperty.SetGetMethod(getSourceProperty);

            var getSourceIl = getSourceProperty.GetILGenerator();

            getSourceIl.Emit(OpCodes.Ldarg_0);
            getSourceIl.Emit(OpCodes.Ldfld, sourceField);
            getSourceIl.Emit(OpCodes.Ret);

            // Constructor
            var ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                new[] { CtorType });
            var superCtor = typeof(object).GetConstructors().First();

            var ctorIl = ctor.GetILGenerator();
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Call, superCtor);
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldarg_1);
            ctorIl.Emit(OpCodes.Stfld, sourceField);
            ctorIl.Emit(OpCodes.Ret);

            // Create
            var createMethod = typeBuilder.DefineMethod(
                nameof(IActivator.Create),
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                typeof(object),
                new[] { typeof(object[]) });
            typeBuilder.DefineMethodOverride(createMethod, typeof(IActivator).GetMethod(nameof(IActivator.Create)));

            var createIl = createMethod.GetILGenerator();

            for (var i = 0; i < ci.GetParameters().Length; i++)
            {
                createIl.Emit(OpCodes.Ldarg_1);
                createIl.EmitLdcI4(i);
                createIl.Emit(OpCodes.Ldelem_Ref);
                createIl.EmitTypeConversion(ci.GetParameters()[i].ParameterType);
            }

            createIl.Emit(OpCodes.Newobj, ci);
            createIl.Emit(OpCodes.Ret);

            var type = typeBuilder.CreateType();

            // TODO Debug
            AssemblyBuilder.Save("test.dll");

            return (IActivator)Activator.CreateInstance(type, ci);
        }
    }

    // Sample

    public sealed class Sample0Activator : IActivator
    {
        public ConstructorInfo Source { get; }

        public Sample0Activator(ConstructorInfo source)
        {
            Source = source;
        }

        public object Create(params object[] arguments)
        {
            return new Class0();
        }
    }

    public sealed class Sample1Activator : IActivator
    {
        public ConstructorInfo Source { get; }

        public Sample1Activator(ConstructorInfo source)
        {
            Source = source;
        }

        public object Create(params object[] arguments)
        {
            return new Class1((int)arguments[0]);
        }
    }
}
