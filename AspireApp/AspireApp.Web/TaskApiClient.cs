using System.Net.Http.Json;

namespace AspireApp.Web;

public class TaskApiClient(HttpClient httpClient)
{
    public async Task<TaskItem[]> GetTasksAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<TaskItem[]>("/api/tasks", cancellationToken) ?? [];
    }

    public async Task<TaskItem?> GetTaskByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<TaskItem>($"/api/tasks/{id}", cancellationToken);
    }

    public async Task<TaskItem?> CreateTaskAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/tasks", task, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TaskItem>(cancellationToken: cancellationToken);
        }
        return null;
    }

    public async Task<TaskItem?> UpdateTaskAsync(Guid id, TaskItem task, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/tasks/{id}", task, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TaskItem>(cancellationToken: cancellationToken);
        }
        return null;
    }

    public async Task<bool> DeleteTaskAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/tasks/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<AiSuggestResponse?> SuggestTaskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/ai/suggest-task", new AiSuggestRequest(prompt), cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AiSuggestResponse>(cancellationToken: cancellationToken);
        }
        return null;
    }

    public async Task<AiChatResponse?> ChatAsync(string message, List<ChatMessageDto> history, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/ai/chat", new AiChatRequest(message, history), cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AiChatResponse>(cancellationToken: cancellationToken);
        }
        return null;
    }
}

public record AiSuggestRequest(string Prompt);
public record AiSuggestResponse(string Title, string Description, string Priority, string Assignee);
public record AiChatRequest(string Message, List<ChatMessageDto> History);
public record ChatMessageDto(string Role, string Content);
public record AiChatResponse(string Response);

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
