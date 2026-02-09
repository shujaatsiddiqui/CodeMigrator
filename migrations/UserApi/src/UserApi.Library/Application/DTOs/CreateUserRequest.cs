namespace UserApi.Library.Application.DTOs;

public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}
