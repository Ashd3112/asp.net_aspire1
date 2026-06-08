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
