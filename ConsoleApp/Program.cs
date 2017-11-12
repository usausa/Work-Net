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
            var ctor0 = type0.GetConstructor(Type.EmptyTypes);
            var ctor1 = type1.GetConstructors().First();

            var activator0 = EmitMethodGenerator.CreateActivator(ctor0);
            var o = activator0.Create(null);

            var data = new DataClass();
            var pi = data.GetType().GetProperty(nameof(DataClass.StringValue));

            var a = EmitMethodGenerator.CreateAccessor(pi);
            a.SetValue(data, "a");
            var ret = a.GetValue(data);
        }

        // Test

        //private static readonly AssemblyBuilder AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
        //    new AssemblyName("DynamicActivatorAssembly"),
        //    AssemblyBuilderAccess.RunAndSave);

        //private static readonly ModuleBuilder ModuleBuilder = AssemblyBuilder.DefineDynamicModule(
        //    "DynamicActivatorModule",
        //    "test.dll");
    }
}
