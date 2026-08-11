using Users;

var builder = WebApplication.CreateBuilder(args);

// add controllers
builder.Services.AddControllers();

// register modules
builder.Services.AddUserModule();

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
