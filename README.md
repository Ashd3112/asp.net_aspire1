# .NET Aspire Task Management Workspace

A distributed, modern Task Management Kanban Board application built using **.NET Aspire**, **ASP.NET Core Minimal APIs**, and **Blazor Interactive Server Components**. 

This repository leverages the orchestration capabilities of .NET Aspire to manage service connection, discovery, and resiliency between the backend API and frontend web projects.

---

## Architecture Overview

The application consists of the following components:

```mermaid
graph TD
    AppHost[AspireApp.AppHost] -->|Orchestrates| ApiService[AspireApp.ApiService]
    AppHost -->|Orchestrates| Web[AspireApp.Web]
    Web -->|Service Discovery: HTTP| ApiService
```

* **`AspireApp.AppHost`**: The entrypoint project that defines the distributed system, orchestrates service startup, handles service discovery, and spins up the Aspire Dashboard.
* **`AspireApp.ApiService`**: The ASP.NET Core backend hosting REST CRUD endpoints for tasks, using an in-memory thread-safe store.
* **`AspireApp.Web`**: A Blazor server frontend incorporating a highly visual glassmorphic dashboard (Kanban Board) for task tracking.
* **`AspireApp.ServiceDefaults`**: Standard configuration helpers for OpenTelemetry, health checks, and service resilience patterns.

---

## Technology Stack

* **Orchestration**: .NET Aspire 9.0+
* **AI Engine**: Microsoft Semantic Kernel (v1.77.0)
* **LLM Integrations**: Ollama (local offline models like `llama3.2`) and OpenAI (cloud model connection via API key)
* **Backend**: ASP.NET Core Web APIs (Minimal API)
* **Frontend**: Blazor Web App (Interactive Server render mode)
* **Styling**: Vanilla CSS with glassmorphic styling, glowing borders, custom typography (Outfit and Inter Google Fonts), and responsive flex layouts.

---

## Features

* **AI Copilot Sidebar**: Interactive, glassmorphic chat assistant integrated into the dashboard. It uses Semantic Kernel to read real-time task board content and answer queries or summarize work.
* **Express Task Idea (AI Auto-Fill)**: Allows you to type an informal task idea (e.g. *"fix memory leak and assign to Sarah with high priority"*), and the AI automatically extracts and suggests structured Title, Description, Priority, and Assignee details.
* **Distributed Orchestration**: Auto-configured service connection strings and base URLs using Aspire Service Discovery (`https+http://apiservice`).
* **Visual Kanban Board**: Filterable columns representing states: *To Do*, *In Progress*, *In Review*, and *Completed*.
* **Dynamic Analytics**: Live task status counters at the top of the interface.
* **Task CRUD Operations**: Inline form for task creation (Title, Assignee, Description, Priority, Due Date), status transition buttons, and delete actions.

---

## AI Configuration

The application is configured to run in **Mock Mode** by default. To connect a live LLM, configure the `AISettings` section in `AspireApp.ApiService/appsettings.json`:

```json
  "AISettings": {
    "Provider": "Ollama", // Choose: "Ollama", "OpenAI", or "None" (Mock Fallback)
    "ApiKey": "", // Put your OpenAI ApiKey here if using OpenAI provider
    "ModelId": "gpt-4o-mini", // OpenAI model ID
    "Endpoint": "http://localhost:11434", // Local Ollama endpoint
    "OllamaModelId": "llama3.2" // Local Ollama model to pull/use
  }
```

*Note: If using Ollama, ensure the Ollama app is running locally and you've pulled the model via `ollama pull llama3.2`.*

---

## Getting Started

### Prerequisites

* [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
* [.NET Aspire Workload](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)

### Trust the Development Certificate

Before running the application, make sure the local HTTPS development certificate is trusted:

```powershell
dotnet dev-certs https --trust
```

### Running the Application

To start the entire distributed system, run the AppHost project:

```powershell
dotnet run --project AspireApp.AppHost
```

Once started, the console will output the URL for the **Aspire Dashboard** (e.g., `https://localhost:17229`). Navigate to this page in your browser to view telemetry, logs, and click the link for the `webfrontend` to access the Task Board.
<img width="1919" height="843" alt="image" src="https://github.com/user-attachments/assets/973702e9-d044-4ffa-a5c5-4f202fe28092" />
<img width="1181" height="523" alt="image" src="https://github.com/user-attachments/assets/a8402b69-699a-4fd8-90b0-699348aef618" />
<img width="1919" height="625" alt="image" src="https://github.com/user-attachments/assets/d38dd20d-7f4a-4292-a0ab-3007a19d91e9" />


