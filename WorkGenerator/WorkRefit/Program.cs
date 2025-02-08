using Refit;

namespace WorkRefit;

internal class Program
{
    static void Main(string[] args)
    {
    }
}

public interface IApi
{
    [Get("/users/{user}")]
    Task<User> GetUser(string user);
}

public class User
{
}
