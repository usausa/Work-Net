namespace WorkPasswordHash;

using System.Security.Cryptography;

internal class Program
{
    public static void Main()
    {
        var provider = new PasswordProvider(16, 32, 10000);
        var hash = provider.Hash("Passw0rd");
        var result = provider.Verify("Passw0rd", hash);
        Console.WriteLine(result);
    }
}

public sealed class PasswordProvider
{
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    private readonly int saltSize;
    private readonly int hashSize;
    private readonly int iterations;

    public PasswordProvider(int saltSize, int hashSize, int iterations)
    {
        this.saltSize = saltSize;
        this.hashSize = hashSize;
        this.iterations = iterations;
    }

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(saltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, hashSize);

        return Convert.ToHexString(salt) + Convert.ToHexString(hash);
    }

    public bool Verify(string password, string passwordHash)
    {
        var bytes = Convert.FromHexString(passwordHash);
        var salt = bytes[..saltSize];
        var hash = bytes[saltSize..];

        var hash2 = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, hashSize);

        return CryptographicOperations.FixedTimeEquals(hash, hash2);
    }
}
