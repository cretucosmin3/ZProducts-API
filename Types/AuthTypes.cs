namespace ProductAPI.Types;

public class UserDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterUserDto
{
    public string AccessCode { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;
    public string Password { get; set; } = String.Empty;
    public string RepeatPassword { get; set; } = String.Empty;
}