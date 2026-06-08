var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

// --- Task Manager API ---
var tasksStore = new System.Collections.Concurrent.ConcurrentDictionary<Guid, TaskItem>();

// Seed sample tasks
var seedTasks = new System.Collections.Generic.List<TaskItem>
{
    new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Design App Layout", "Create glassmorphic layout and Figma mockups for the front page.", "InProgress", "High", "Sarah Connor", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(2)),
    new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Setup Distributed Service", "Configure .NET Aspire service discovery and resilience defaults.", "Todo", "Medium", "Ashwini D.", DateTime.UtcNow, DateTime.UtcNow.AddDays(5)),
    new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Setup CI/CD Pipeline", "Configure GitHub actions to build and release to Azure Container Apps.", "Todo", "Low", "Aria Stark", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(10)),
    new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Dashboard UI Polish", "Fix sidebar transition animations and improve typography readability.", "InReview", "High", "Emily Stone", DateTime.UtcNow.AddDays(-3), DateTime.UtcNow.AddDays(1)),
    new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "Write Unit Tests", "Implement unit tests for the core service models.", "Completed", "Medium", "David Miller", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-1))
};

foreach (var task in seedTasks)
{
    tasksStore.TryAdd(task.Id, task);
}

app.MapGet("/api/tasks", () => tasksStore.Values.OrderByDescending(t => t.CreatedAt));

app.MapGet("/api/tasks/{id}", (Guid id) =>
    tasksStore.TryGetValue(id, out var task) ? Results.Ok(task) : Results.NotFound());

app.MapPost("/api/tasks", (TaskItem task) =>
{
    var newTask = task with { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
    tasksStore.TryAdd(newTask.Id, newTask);
    return Results.Created($"/api/tasks/{newTask.Id}", newTask);
});

app.MapPut("/api/tasks/{id}", (Guid id, TaskItem updatedTask) =>
{
    if (!tasksStore.ContainsKey(id))
    {
        return Results.NotFound();
    }
    var task = updatedTask with { Id = id };
    tasksStore[id] = task;
    return Results.Ok(task);
});

app.MapDelete("/api/tasks/{id}", (Guid id) =>
    tasksStore.TryRemove(id, out _) ? Results.NoContent() : Results.NotFound());
// ------------------------

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record TaskItem(
    Guid Id,
    string Title,
    string Description,
    string Status, // Todo, InProgress, InReview, Completed
    string Priority, // Low, Medium, High
    string Assignee,
    DateTime CreatedAt,
    DateTime? DueDate
);

