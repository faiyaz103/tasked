namespace Users.Services;

public interface IUserService
{
    string GetHelloMessage();
}

public class UserService: IUserService
{
    public string GetHelloMessage()
    {
        return "Hello from .NET !";
    }
}