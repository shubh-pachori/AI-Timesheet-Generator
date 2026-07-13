using AITimesheet.IdentityService.Entities;
using AITimesheet.IdentityService.DTOs;

namespace AITimesheet.IdentityService.ServiceLayer.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<User>> GetUsersByManagerIdAsync(Guid managerId, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
}
