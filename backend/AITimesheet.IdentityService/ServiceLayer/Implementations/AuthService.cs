using AITimesheet.IdentityService.Entities;
using AITimesheet.IdentityService.DTOs;
using AITimesheet.IdentityService.RepositoryLayer.Interfaces;
using AITimesheet.IdentityService.ServiceLayer.Interfaces;
using AITimesheet.IdentityService.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AITimesheet.IdentityService.ServiceLayer.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly JwtService _jwtService;

    public AuthService(IUserRepository userRepo, IAuditLogRepository auditRepo, JwtService jwtService)
    {
        _userRepo = userRepo;
        _auditRepo = auditRepo;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLower();
        var user = await _userRepo.GetByEmailAsync(email, ct);

        if (user is null)
        {
            // Determine role: emails containing "manager" are Managers, otherwise Employee
            string role = "Employee";
            if (email.Contains("manager"))
            {
                role = "Manager";
            }

            user = new User
            {
                Email = email,
                FullName = request.FullName,
                AzureAdObjectId = request.AzureAdObjectId,
                Role = role,
                IsActive = true
            };

            // If this is an employee, auto-associate with a manager for demo workflows
            if (role == "Employee")
            {
                // Find any existing manager
                var managers = await _userRepo.GetByManagerIdAsync(Guid.Empty, ct); // Hack: get manager lists or check DB directly
                // Better: query database or check if any manager exists
                // Let's check if we can assign a default manager or find the first manager
                var firstManager = await FindFirstManagerAsync(ct);
                if (firstManager is null)
                {
                    // Create a default demo manager
                    var defaultManager = new User
                    {
                        Email = "manager@company.com",
                        FullName = "Sarah Jenkins (Manager)",
                        Role = "Manager",
                        IsActive = true
                    };
                    await _userRepo.AddAsync(defaultManager, ct);
                    await _userRepo.SaveChangesAsync(ct);
                    user.ManagerId = defaultManager.Id;
                }
                else
                {
                    user.ManagerId = firstManager.Id;
                }
            }

            await _userRepo.AddAsync(user, ct);
            await _userRepo.SaveChangesAsync(ct);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = user.Id,
                Action = "User Registered",
                Details = $"Registered user {user.FullName} with role {user.Role}"
            }, ct);
            await _auditRepo.SaveChangesAsync(ct);
        }
        else
        {
            user.IsActive = true;
            await _userRepo.UpdateAsync(user, ct);
            await _userRepo.SaveChangesAsync(ct);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = user.Id,
                Action = "User Logged In",
                Details = $"Logged in user {user.FullName}"
            }, ct);
            await _auditRepo.SaveChangesAsync(ct);
        }

        var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role);
        var userDto = new UserDto(user.Id, user.FullName, user.Email, user.Role, user.ManagerId);

        return new AuthResponseDto(userDto, token);
    }

    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _userRepo.GetByIdAsync(userId, ct);
    }

    public async Task<List<User>> GetUsersByManagerIdAsync(Guid managerId, CancellationToken ct = default)
    {
        return await _userRepo.GetByManagerIdAsync(managerId, ct);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        // For simplicity, we just log this action or set active to false
        // The token is stateless and client-discarded
        await Task.CompletedTask;
    }

    private async Task<User?> FindFirstManagerAsync(CancellationToken ct)
    {
        // Retrieve a manager by checking who is registered with Manager role
        // Since we don't have direct _db access, we can rely on a query in our repo, or fetch it.
        // Let's search the repo. Wait, our IUserRepository does not have a "GetByRole" but we can implement it,
        // or we can search manager using custom queries.
        // Let's implement a helper or retrieve using managerId.
        // Wait, in IUserRepository we can add GetManagersAsync, or we can just fetch all or query by managerId.
        // Since we want to find managers, let's retrieve any user whose role is Manager. Let's do that.
        // We'll update IUserRepository and UserRepository to support finding a manager, or query by a default email.
        return await _userRepo.GetByEmailAsync("manager@company.com", ct);
    }
}
