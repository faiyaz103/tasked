Writing that validation boilerplate in every single controller method is an anti-pattern. 

The industry standard way to handle this in a Controller-based architecture is to use an **Action Filter**. 

An Action Filter intercepts the HTTP request *just before* it hits your controller method. It will look at the incoming request, find the appropriate FluentValidator, run it, and if it fails, immediately return a `400 BadRequest` without your controller ever being called.

Here is how you can implement a clean, global validation filter.

### Step 1: Create the Validation Filter
Create a new folder in `f:\tasked\src\Shared\Shared.Infra` called `Filters` and add this class:

```csharp
// f:\tasked\src\Shared\Shared.Infra\Filters\ValidationFilter.cs
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infra.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 1. Look through all the arguments passed to the controller action
        foreach (var argument in context.ActionArguments.Values.Where(v => v != null))
        {
            var argumentType = argument!.GetType();

            // 2. Try to find a validator for this specific type in the DI container
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

            if (validator != null)
            {
                // 3. If a validator exists, run it
                var validationContext = new ValidationContext<object>(argument);
                var validationResult = await validator.ValidateAsync(validationContext);

                // 4. If validation fails, immediately return a 400 BadRequest with ProblemDetails format
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(x => x.ErrorMessage).ToArray()
                        );

                    context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors)
                    {
                        Title = "Validation Failed",
                        Status = 400
                    });

                    // Short-circuit the pipeline (the controller method will never execute)
                    return; 
                }
            }
        }

        // 5. If everything is valid, continue to the controller method
        await next();
    }
}
```

### Step 2: Register the Filter Globally
Now, tell ASP.NET Core to apply this filter to *every* controller in your application.

Open `f:\tasked\src\Host\WebApi\Program.cs` and update your `AddControllers` line:

```csharp
// In f:\tasked\src\Host\WebApi\Program.cs

using Shared.Infra.Filters; // Add this using

// ... existing code ...

// Update AddControllers to include the global filter
builder.Services.AddControllers(options => 
{
    options.Filters.Add<ValidationFilter>();
});

// ... existing code ...
```

### Step 3: Enjoy your incredibly clean Controllers!
Because the filter runs automatically, you can strip out **all** validation logic and even the `IValidator` injection from your controller. 

Your controller is now purely focused on routing and business logic execution:

```csharp
// f:\tasked\src\Modules\Users\Controllers\UsersController.cs
using Microsoft.AspNetCore.Mvc;
using Users.Dtos;
using Users.Services;
namespace Users.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    
    // NOTE: We completely removed IValidator<CreateProfileRequest> from the constructor!
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("profile")]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfileRequest request)
    {
        // NO VALIDATION BOILERPLATE!
        // If the request is invalid, the ValidationFilter blocks it and returns 400.
        // If the code reaches here, you are 100% guaranteed the request is valid.

        var (id, responseData) = await _userService.CreateProfileAsync(request);

        return CreatedAtAction(nameof(GetProfileById), new { id }, responseData);
    }
}
```

### Why this is awesome:
1. **DRY (Don't Repeat Yourself):** You write the validation execution logic exactly once in your entire project.
2. **True Separation of Concerns:** Controllers don't need to know *how* validation happens, they just expect valid data.
3. **Fails Fast:** The server rejects bad payloads before allocating memory for complex business logic in your services.

*(Note: If you eventually transition to the CQRS pattern with MediatR as suggested earlier, you would use a **MediatR Pipeline Behavior** instead of an Action Filter, which achieves the exact same clean result but at the Command level rather than the HTTP level.)*