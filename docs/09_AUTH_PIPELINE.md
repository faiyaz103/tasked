### Step 1: Install Required Packages

Run this in your terminal at the solution root:

```bash
dotnet add src/Host/WebApi/WebApi.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/Modules/Users/Users.csproj package BCrypt.Net-Next

```

*(We use `BCrypt.Net-Next` as the industry standard for hashing passwords and refresh tokens).*

---

### Step 2: Sign up service method
```csharp
public async Task<string> CreateUserAsync(CreateUserRequest request)
    {
        // validate email
        var emailExists = await _dbContext.Users.AnyAsync(u=> u.Email == request.Email);
        if (emailExists)
        {
            throw new InvalidOperationException("This email already exists");
        }

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        Roles userRole = string.IsNullOrWhiteSpace(request.Role) ? Roles.User : Enum.Parse<Roles>(request.Role, ignoreCase: true);

        var newUser = new UserEntity
        {
            Email = request.Email,
            Password = hashedPassword,
            Role = userRole
        };

        _dbContext.Users.Add(newUser);
        await _dbContext.SaveChangesAsync();

        return $"Registration Successful for {request.Email}";
    }
```

---