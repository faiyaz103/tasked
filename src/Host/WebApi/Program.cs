using Tasks;
using Users;
using Users.Entities;

var builder = WebApplication.CreateBuilder(args);

// add controllers
builder.Services.AddControllers();

// register modules
builder.Services.AddUserModule(builder.Configuration);
builder.Services.AddTasksModule(builder.Configuration);

// configure CORS (prepare for FE later)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- DATABASE CONNECTION CHECK ---
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

        // Attempts a ping connection to PostgreSQL
        if (await dbContext.Database.CanConnectAsync())
        {
            logger.LogInformation("PostgreSQL Database connected successfully!");
        }
        else
        {
            logger.LogWarning("Failed to establish a connection to PostgreSQL Database.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while trying to connect to PostgreSQL Database.");
    }
}

// Configure middleware
app.UseCors("AllowFrontend");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

// map controllers
app.MapControllers();

app.Run();
