using UserApi.Library.Application.DTOs;
using UserApi.Library.Domain.Entities;

namespace UserApi.Library.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(Guid id);
    Task<User> CreateUserAsync(CreateUserRequest request);
    Task DeleteUserAsync(Guid id);
}
