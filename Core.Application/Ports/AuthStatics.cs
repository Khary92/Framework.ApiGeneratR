namespace Core.Application.Ports;

public class AuthStatics
{
    public static string PrivateKeyPath = "/certs/private_signing_key.pem";
    public static string PublicKeyPath = "/certs/public_signing_key.pem";
    public static string Issuer = "Planner.Issuer";
    public static string Audience = "Planner.Audience.User";
}