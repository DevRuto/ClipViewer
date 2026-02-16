using ClipViewer.Data.Models;

namespace ClipViewer.API.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
