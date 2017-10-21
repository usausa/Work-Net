namespace ConsoleApp.Reflection
{
    using System;
    using System.Runtime.InteropServices.WindowsRuntime;

    public class EmitTypeMetadataFactory : IActivationFactory
    {
        public static EmitTypeMetadataFactory Default { get; } = new EmitTypeMetadataFactory();

        public object ActivateInstance()
        {
            throw new NotImplementedException();
        }
    }
}
