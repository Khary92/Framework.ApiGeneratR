namespace Presentation.Web.Services;

public class PasswordGenerator : IPasswordGenerator
{
    private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public string Generate(int length = 12)
    {
        var random = new Random();
        var password = new char[length];

        for (var i = 0; i < length; i++) password[i] = Chars[random.Next(Chars.Length)];

        return new string(password);
    }
}