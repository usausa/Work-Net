namespace WorkAop.Services;

public interface ITestService
{
    void Test();

    int Calc(int x, int y);

    public ValueTask AsyncMethod();

    public ValueTask<int> AsyncMethod1();
}

[Service(typeof(ITestService))]
public sealed class TestService : ITestService
{
    public void Test()
    {
    }

    public int Calc(int x, int y) => x + y;

    public async ValueTask AsyncMethod() => await Task.Delay(0);

    public ValueTask<int> AsyncMethod1() => ValueTask.FromResult(1);
}
