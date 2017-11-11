namespace ConsoleApp
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using ConsoleApp.Reflection;

    using Smart.Reflection;
    using Smart.Reflection.Emit;

    public static class Program
    {
        public static void Main(string[] args)
        {
            //WorkDynamicProxy.Test();

            var type0 = typeof(Class0);
            var type1 = typeof(Class1);
            var ctor0 = type0.GetConstructors().First();
            var ctor1 = type1.GetConstructors().First();

            var activator0 = EmitMethodGenerator.CreateActivator(ctor0);
            var o = activator0.Create(null);
        }

        // Test

        private static readonly AssemblyBuilder AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("DynamicActivatorAssembly"),
            AssemblyBuilderAccess.RunAndSave);

        private static readonly ModuleBuilder ModuleBuilder = AssemblyBuilder.DefineDynamicModule(
            "DynamicActivatorModule",
            "test.dll");

        private static readonly Type PropertyInfoType = typeof(PropertyInfo);

        private static readonly Type BoolType = typeof(bool);

        private static readonly Type TypeType = typeof(Type);

        private static readonly Type StringType = typeof(string);

        public static IAccessor CreateAccessor(PropertyInfo pi)
        {
            var typeBuilder = ModuleBuilder.DefineType(
                $"{pi.DeclaringType.FullName}_{pi.Name}_DynamicActivator",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);

            typeBuilder.AddInterfaceImplementation(typeof(IAccessor));

            // Fields
            var sourceField = typeBuilder.DefineField(
                 "source",
                 PropertyInfoType,
                 FieldAttributes.Private | FieldAttributes.InitOnly);

            // Property
            DefineSourceProperty(typeBuilder, sourceField);

            // TODO
            // Name
            var nameProperty = typeBuilder.DefineProperty(
                "Name",
                PropertyAttributes.None,
                StringType,
                null);

            // TODO
            // Type
            var typeProperty = typeBuilder.DefineProperty(
                "Type",
                PropertyAttributes.None,
                TypeType,
                null);

            // CanRead
            DefineAccessibilityProperty(typeBuilder, pi.CanRead, "CanRead");
            // CanWrite
            DefineAccessibilityProperty(typeBuilder, pi.CanWrite, "CanWrite");

            // TODO ctor
            // TODO get
            // TODO set

            //    // Constructor
            //    var ctor = typeBuilder.DefineConstructor(
            //        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            //        CallingConventions.Standard,
            //        new[] { CtorType });
            //    var superCtor = typeof(object).GetConstructors().First();

            //    var ctorIl = ctor.GetILGenerator();
            //    ctorIl.Emit(OpCodes.Ldarg_0);
            //    ctorIl.Emit(OpCodes.Call, superCtor);
            //    ctorIl.Emit(OpCodes.Ldarg_0);
            //    ctorIl.Emit(OpCodes.Ldarg_1);
            //    ctorIl.Emit(OpCodes.Stfld, sourceField);
            //    ctorIl.Emit(OpCodes.Ret);

            //    // Create
            //    var createMethod = typeBuilder.DefineMethod(
            //        nameof(IActivator.Create),
            //        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
            //        typeof(object),
            //        new[] { typeof(object[]) });
            //    typeBuilder.DefineMethodOverride(createMethod, typeof(IActivator).GetMethod(nameof(IActivator.Create)));

            //    var createIl = createMethod.GetILGenerator();

            //    createIl.Emit(OpCodes.Newobj, ci);
            //    createIl.Emit(OpCodes.Ret);

            var type = typeBuilder.CreateType();

            // Debug
            AssemblyBuilder.Save("test.dll");

            return (IAccessor)Activator.CreateInstance(type, pi);
        }

        private static void DefineSourceProperty(TypeBuilder typeBuilder, FieldBuilder sourceField)
        {
            var property = typeBuilder.DefineProperty(
                "Source",
                PropertyAttributes.None,
                PropertyInfoType,
                null);
            var method = typeBuilder.DefineMethod(
                "get_Source",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
                PropertyInfoType,
                Type.EmptyTypes);
            property.SetGetMethod(method);

            var ilGenerator = method.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, sourceField);
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static void DefineAccessibilityProperty(TypeBuilder typeBuilder, bool enable, string name)
        {
            var property = typeBuilder.DefineProperty(
                name,
                PropertyAttributes.None,
                BoolType,
                null);
            var method = typeBuilder.DefineMethod(
                $"get_{name}",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
                BoolType,
                Type.EmptyTypes);
            property.SetGetMethod(method);

            var ilGenerator = method.GetILGenerator();

            if (enable)
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
            }
            ilGenerator.Emit(OpCodes.Ret);
        }
    }
}
