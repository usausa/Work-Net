namespace WorkAop.Services;

public interface ITestService
{
    int Calc(int x, int y);
}

[Service(typeof(ITestService))]
public sealed class TestService : ITestService
{
    public int Calc(int x, int y) => x + y;
}
