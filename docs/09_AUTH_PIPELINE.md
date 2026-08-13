### Step 1: Install Required Packages

Run this in your terminal at the solution root:

```bash
dotnet add src/Host/WebApi/WebApi.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/Modules/Users/Users.csproj package BCrypt.Net-Next

```

*(We use `BCrypt.Net-Next` as the industry standard for hashing passwords and refresh tokens).*

---