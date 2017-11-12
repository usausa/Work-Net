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

            //var type0 = typeof(Class0);
            //var type1 = typeof(Class1);
            //var ctor0 = type0.GetConstructors().First();
            //var ctor1 = type1.GetConstructors().First();

            //var activator0 = EmitMethodGenerator.CreateActivator(ctor0);
            //var o = activator0.Create(null);

            var data = new DataClass();
            var pi = data.GetType().GetProperty(nameof(DataClass.StringValue));

            var a = CreateAccessor(pi);
            a.SetValue(data, "a");
            var ret = a.GetValue(data);
        }

        // Test

        private static readonly AssemblyBuilder AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("DynamicActivatorAssembly"),
            AssemblyBuilderAccess.RunAndSave);

        private static readonly ModuleBuilder ModuleBuilder = AssemblyBuilder.DefineDynamicModule(
            "DynamicActivatorModule",
            "test.dll");

        private static readonly Type ObjectType = typeof(object);

        private static readonly Type VoidType = typeof(void);

        private static readonly Type PropertyInfoType = typeof(PropertyInfo);

        private static readonly Type BoolType = typeof(bool);

        private static readonly Type StringType = typeof(string);

        private static readonly Type TypeType = typeof(Type);

        private static readonly MethodInfo PropertyInfoNameGetMethod =
            PropertyInfoType.GetProperty(nameof(PropertyInfo.Name)).GetGetMethod();

        private static readonly MethodInfo PropertyInfoPropertyTypeGetMethod =
            PropertyInfoType.GetProperty(nameof(PropertyInfo.PropertyType)).GetGetMethod();

        private static readonly ConstructorInfo ObjectConstructor = ObjectType.GetConstructor(Type.EmptyTypes);

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
            DefineNameProperty(typeBuilder, sourceField);
            DefineTypeProperty(typeBuilder, sourceField);
            DefineAccessibilityProperty(typeBuilder, pi.CanRead, nameof(IAccessor.CanRead));
            DefineAccessibilityProperty(typeBuilder, pi.CanWrite, nameof(IAccessor.CanWrite));

            // Constructor
            DefineConstructor(typeBuilder, sourceField);

            DefineGetValueMethod(typeBuilder, pi);
            DefineSetValueMethod(typeBuilder, pi);

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

        private static void DefineNameProperty(TypeBuilder typeBuilder, FieldBuilder sourceField)
        {
            var property = typeBuilder.DefineProperty(
                "Name",
                PropertyAttributes.None,
                StringType,
                null);
            var method = typeBuilder.DefineMethod(
                "get_Name",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
                StringType,
                Type.EmptyTypes);
            property.SetGetMethod(method);

            var ilGenerator = method.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, sourceField);
            ilGenerator.Emit(OpCodes.Callvirt, PropertyInfoNameGetMethod);
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static void DefineTypeProperty(TypeBuilder typeBuilder, FieldBuilder sourceField)
        {
            var property = typeBuilder.DefineProperty(
                "Type",
                PropertyAttributes.None,
                TypeType,
                null);
            var method = typeBuilder.DefineMethod(
                "get_Type",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
                TypeType,
                Type.EmptyTypes);
            property.SetGetMethod(method);

            var ilGenerator = method.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, sourceField);
            ilGenerator.Emit(OpCodes.Callvirt, PropertyInfoPropertyTypeGetMethod);
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

        private static void DefineConstructor(TypeBuilder typeBuilder, FieldBuilder sourceField)
        {
            var ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                new[] { PropertyInfoType });

            var ctorIl = ctor.GetILGenerator();

            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Call, ObjectConstructor);
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldarg_1);
            ctorIl.Emit(OpCodes.Stfld, sourceField);
            ctorIl.Emit(OpCodes.Ret);
        }

        private static void DefineGetValueMethod(TypeBuilder typeBuilder, PropertyInfo pi)
        {
            var method = typeBuilder.DefineMethod(
                nameof(IActivator.Create),
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                ObjectType,
                new[] { ObjectType });
            typeBuilder.DefineMethodOverride(method, typeof(IAccessor).GetMethod(nameof(IAccessor.GetValue)));

            var ilGenerator = method.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Castclass, pi.DeclaringType);
            ilGenerator.Emit(OpCodes.Callvirt, pi.GetGetMethod());
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static void DefineSetValueMethod(TypeBuilder typeBuilder, PropertyInfo pi)
        {
            var method = typeBuilder.DefineMethod(
                nameof(IActivator.Create),
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                VoidType,
                new[] { ObjectType, ObjectType });
            typeBuilder.DefineMethodOverride(method, typeof(IAccessor).GetMethod(nameof(IAccessor.SetValue)));

            var ilGenerator = method.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Castclass, pi.DeclaringType);
            ilGenerator.Emit(OpCodes.Ldarg_2);
            ilGenerator.Emit(OpCodes.Castclass, pi.PropertyType);
            ilGenerator.Emit(OpCodes.Callvirt, pi.GetSetMethod());
            ilGenerator.Emit(OpCodes.Ret);
        }
    }
}
