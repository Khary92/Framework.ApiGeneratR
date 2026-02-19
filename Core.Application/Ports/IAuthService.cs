namespace Core.Application.Ports;

public interface IAuthService
{
    string GetToken(Guid userIdentityId, string requestEmail, string clearPassword, string userRole);
    bool CreateIdentityUser(Guid userId, string userName, string password);
    void DeleteIdentityUser(Guid userIdentityId);
}