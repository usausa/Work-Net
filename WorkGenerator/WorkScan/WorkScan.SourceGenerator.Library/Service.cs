namespace WorkScan.SourceGenerator.Library;

public class FooService
{
    public void Execute()
    {
    }
}

public interface IBarService
{
    public void Execute();
}

public class BarService : IBarService
{
    public void Execute()
    {
    }
}

public interface IMixedService1
{
    public void Execute1();
}

public interface IMixedService2
{
    public void Execute2();
}

public class MixedService : IMixedService1, IMixedService2
{
    public void Execute1()
    {
    }

    public void Execute2()
    {
    }
}
