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
            //var ctor0 = type0.GetConstructor(Type.EmptyTypes);
            //var ctor1 = type1.GetConstructors().First();

            //var activator0 = EmitMethodGenerator.CreateActivator(ctor0);
            //var o = activator0.Create(null);

            var data = new DataClass();
            var piStr = data.GetType().GetProperty(nameof(DataClass.StringValue));
            var piInt = data.GetType().GetProperty(nameof(DataClass.IntValue));
            var piStruct = data.GetType().GetProperty(nameof(DataClass.StructValue));

            // struct
            var accessorStruct = EmitMethodGenerator.CreateAccessor(piStruct);

            data.StructValue = new Size { X = 1, Y = 2 };
            var i = data.StructValue.X;
            accessorStruct.SetValue(data, null);
            i = data.StructValue.X;
            accessorStruct.SetValue(data, new Size { X = 2, Y = 3 });
            i = data.StructValue.X;

            // str
            var accessorStr = EmitMethodGenerator.CreateAccessor(piStr);
            accessorStr.SetValue(data, "a");
            var retStr = accessorStr.GetValue(data);

            // int
            var accessorInt = EmitMethodGenerator.CreateAccessor(piInt);
            // TODO
            accessorInt.SetValue(data, 1);
            var retInt = accessorInt.GetValue(data);
            accessorInt.SetValue(data, null);
            retInt = accessorInt.GetValue(data);
        }
    }
}
