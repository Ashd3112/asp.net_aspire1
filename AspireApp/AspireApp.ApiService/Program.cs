using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure Semantic Kernel
var aiSettings = builder.Configuration.GetSection("AISettings");
var provider = aiSettings["Provider"] ?? "None";
var apiKey = aiSettings["ApiKey"];
var modelId = aiSettings["ModelId"] ?? "gpt-4o-mini";
var endpoint = aiSettings["Endpoint"] ?? "http://localhost:11434";
var ollamaModelId = aiSettings["OllamaModelId"] ?? "llama3.2";

var kernelBuilder = Kernel.CreateBuilder();
bool hasAiService = false;

if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(apiKey))
{
    kernelBuilder.AddOpenAIChatCompletion(modelId, apiKey);
    hasAiService = true;
}
else if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
{
    var ollamaUri = new Uri(new Uri(endpoint), "/v1");
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: ollamaModelId,
        apiKey: "ollama",
        httpClient: new HttpClient { BaseAddress = ollamaUri }
    );
    hasAiService = true;
}

var kernel = kernelBuilder.Build();
builder.Services.AddSingleton(kernel);
builder.Services.AddSingleton(new AiProviderConfig(provider, hasAiService));

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

// --- AI Assistant Endpoints ---
app.MapPost("/api/ai/suggest-task", async (AiSuggestRequest request, Kernel skKernel, AiProviderConfig config) =>
{
    if (string.IsNullOrWhiteSpace(request.Prompt))
    {
        return Results.BadRequest("Prompt cannot be empty");
    }

    if (!config.HasAiService)
    {
        // Fallback Mock response
        var lowerPrompt = request.Prompt.ToLower();
        var title = "New Task";
        var description = $"Refined from prompt: {request.Prompt}";
        var priority = "Medium";
        var assignee = "AI Assistant";

        if (lowerPrompt.Contains("fix") || lowerPrompt.Contains("bug") || lowerPrompt.Contains("error"))
        {
            title = "Fix Issue";
            priority = "High";
        }
        else if (lowerPrompt.Contains("design") || lowerPrompt.Contains("ui") || lowerPrompt.Contains("layout"))
        {
            title = "Design UI / Layout";
            priority = "Medium";
        }
        else if (lowerPrompt.Contains("deploy") || lowerPrompt.Contains("ci") || lowerPrompt.Contains("pipeline"))
        {
            title = "CI/CD Deployment Setup";
            priority = "High";
        }

        // Try to extract assignee
        var words = request.Prompt.Split(' ');
        for (int i = 0; i < words.Length - 1; i++)
        {
            if (words[i].Equals("assignee", StringComparison.OrdinalIgnoreCase) || 
                words[i].Equals("assign", StringComparison.OrdinalIgnoreCase) ||
                words[i].Equals("to", StringComparison.OrdinalIgnoreCase))
            {
                assignee = words[i + 1].Trim(',', '.');
                break;
            }
        }

        return Results.Ok(new AiSuggestResponse(title, description, priority, assignee));
    }

    try
    {
        var chatService = skKernel.GetRequiredService<IChatCompletionService>();
        var promptTemplate = $@"You are a helpful project manager assistant. Analyze the user's task request and extract task details.
Return ONLY a valid JSON object matching this schema:
{{
  ""Title"": ""Short, clear title for the task"",
  ""Description"": ""Detailed description of what needs to be done"",
  ""Priority"": ""Low, Medium, or High"",
  ""Assignee"": ""Name of the person if mentioned, otherwise 'AI Suggested'""
}}
Do NOT wrap the output in markdown code blocks or return any additional text. Return ONLY the raw JSON string.

User request: ""{request.Prompt}""";

        var response = await chatService.GetChatMessageContentAsync(promptTemplate);
        var responseText = response.Content ?? "{}";

        if (responseText.StartsWith("```"))
        {
            responseText = responseText.Substring(responseText.IndexOf('\n')).Trim('`', ' ', '\r', '\n');
            if (responseText.StartsWith("json"))
            {
                responseText = responseText.Substring(4).Trim();
            }
        }

        var result = System.Text.Json.JsonSerializer.Deserialize<AiSuggestResponse>(responseText, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result != null)
        {
            return Results.Ok(result);
        }
    }
    catch (Exception)
    {
        // Fallback
    }

    return Results.Ok(new AiSuggestResponse("New Task from AI", request.Prompt, "Medium", "AI Suggested"));
});

app.MapPost("/api/ai/chat", async (AiChatRequest request, Kernel skKernel, AiProviderConfig config) =>
{
    var taskListString = string.Join("\n", tasksStore.Values.Select(t => $"- [{t.Status}] {t.Title} (Priority: {t.Priority}, Assigned: {t.Assignee})"));

    if (!config.HasAiService)
    {
        var msg = request.Message.ToLower();
        var reply = "This is a mock AI assistant response (No LLM provider configured). I can see you currently have " + tasksStore.Count + " tasks in this workspace.\n\n";

        if (msg.Contains("tasks") || msg.Contains("todo") || msg.Contains("list"))
        {
            reply += $"Here is the current task status:\n{taskListString}";
        }
        else if (msg.Contains("hello") || msg.Contains("hi"))
        {
            reply += "Hello! How can I help you manage your workspace tasks today?";
        }
        else
        {
            reply += $"You asked: '{request.Message}'. To enable real LLM intelligence, configure OpenAI or Ollama in `appsettings.json`.";
        }

        return Results.Ok(new AiChatResponse(reply));
    }

    try
    {
        var chatService = skKernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage($@"You are a premium AI Assistant integrated into a .NET Aspire Project Workspace. 
You help developers manage, analyze, and organize tasks.

Here is the current real-time task board content:
{taskListString}

Be concise, helpful, and professional. You have access to the above task context.");

        foreach (var msg in request.History)
        {
            if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                chatHistory.AddUserMessage(msg.Content);
            else
                chatHistory.AddAssistantMessage(msg.Content);
        }

        chatHistory.AddUserMessage(request.Message);

        var reply = await chatService.GetChatMessageContentAsync(chatHistory);
        return Results.Ok(new AiChatResponse(reply.Content ?? "No response generated."));
    }
    catch (Exception ex)
    {
        return Results.Ok(new AiChatResponse($"Error calling LLM: {ex.Message}. Make sure your API key or Ollama connection is correct."));
    }
});
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

public record AiProviderConfig(string Provider, bool HasAiService);
public record AiSuggestRequest(string Prompt);
public record AiSuggestResponse(string Title, string Description, string Priority, string Assignee);
public record AiChatRequest(string Message, List<ChatMessageDto> History);
public record ChatMessageDto(string Role, string Content);
public record AiChatResponse(string Response);

