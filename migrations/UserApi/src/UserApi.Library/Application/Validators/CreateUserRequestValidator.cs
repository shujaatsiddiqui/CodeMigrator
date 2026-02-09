using UserApi.Library.Application.DTOs;

namespace UserApi.Library.Application.Validators;

public class CreateUserRequestValidator
{
    private static readonly HashSet<string> BannedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "sa"
    };

    public ValidationResult Validate(CreateUserRequest? request)
    {
        var result = new ValidationResult { IsValid = true };

        if (request == null)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                PropertyName = "Request",
                ErrorMessage = "Request cannot be null"
            });
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                PropertyName = "Name",
                ErrorMessage = "Name is required"
            });
        }
        else if (BannedNames.Contains(request.Name))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                PropertyName = "Name",
                ErrorMessage = $"The name {request.Name} is not allowed"
            });
        }

        if (request.Age <= 0)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                PropertyName = "Age",
                ErrorMessage = "Age must be greater than 0"
            });
        }
        else if (request.Age > 150)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                PropertyName = "Age",
                ErrorMessage = "Age must be 150 or less"
            });
        }

        return result;
    }
}
