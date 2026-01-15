using ClipViewer.API.Models;

namespace ClipViewer.API.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
