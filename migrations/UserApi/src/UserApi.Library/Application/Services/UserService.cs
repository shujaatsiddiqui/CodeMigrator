using UserApi.Library.Application.DTOs;
using UserApi.Library.Application.Interfaces;
using UserApi.Library.Domain.Entities;
using UserApi.Library.Domain.Interfaces;

namespace UserApi.Library.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    private static readonly HashSet<string> BannedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "sa"
    };

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _userRepository.GetAllAsync();
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        if (BannedNames.Contains(request.Name))
        {
            throw new ArgumentException($"The name {request.Name} is not allowed");
        }

        var user = new User
        {
            Name = request.Name,
            Age = request.Age
        };

        return await _userRepository.AddAsync(user);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return;
        }

        await _userRepository.DeleteAsync(user);
    }
}
