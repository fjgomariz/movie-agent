# Movie Finder Agent

A production-ready AI agent built with Microsoft Agent Framework (.NET) that identifies movies based on user descriptions. The agent runs as an HTTP service in Azure Container Apps and uses Microsoft Foundry models (Azure OpenAI).

## Features

- **Movie Identification**: Accepts a description and returns structured movie information
- **Microsoft Agent Framework**: Uses .NET Agent Framework with Azure AI Foundry models
- **OpenTelemetry Integration**: Full observability with traces, logs, and metrics
- **Application Insights**: Native integration with Azure Monitor for centralized monitoring
- **Azure Container Apps**: Production-ready containerized deployment
- **Health Checks**: Built-in health and readiness endpoints
- **Confidence Scoring**: Transparent about uncertainty in AI responses

## API Endpoints

### POST /api/movie
Find a movie based on a description.

**Request:**
```json
{
  "conversationId": "optional-string",
  "description": "text describing the movie",
  "history": [
    {
      "role": "user",
      "content": "previous message"
    },
    {
      "role": "assistant",
      "content": "previous response"
    }
  ]
}
```

**Response:**
```json
{
  "conversationId": "string",
  "result": {
    "title": "The Matrix",
    "releaseYear": "1999",
    "director": "The Wachowskis",
    "cast": ["Keanu Reeves", "Laurence Fishburne", "Carrie-Anne Moss"],
    "imdbRating": "8.7",
    "confidence": "high",
    "notes": "IMDb rating is approximate and may have changed since training data."
  },
  "rawAnswer": "raw AI response"
}
```

**Confidence Levels:**
- `high`: Very confident in the identification
- `medium`: Reasonably confident but some uncertainty
- `low`: Uncertain or information may be approximate

### GET /healthz
Health check endpoint for container orchestration.

### GET /readyz
Readiness check endpoint.

## Environment Variables

### Required

- `AZURE_AI_PROJECT_ENDPOINT`: Your Azure AI Foundry project endpoint (e.g., `https://your-project.openai.azure.com/`)
  - **Note:** This uses Azure OpenAI endpoints that are part of your Azure AI Foundry project
  - The endpoint should point to your Azure OpenAI resource associated with Foundry
- `AZURE_AI_MODEL_DEPLOYMENT_NAME`: Model deployment name in your Foundry project (default: `gpt-4o-mini`)

### Optional

- `AZURE_AI_AGENT_ID`: Existing persistent agent ID in Foundry (if reusing an agent)
- `MOVIE_AGENT_NAME`: Agent name (default: `MovieFinder`)
- `MOVIE_AGENT_INSTRUCTIONS`: Custom system prompt override
- `LOG_LEVEL`: Logging level (default: `Information`)
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: Application Insights connection string for telemetry export

### Azure Authentication

The application uses `DefaultAzureCredential` for authentication. Supported methods:
- **Local Development**: Azure CLI (`az login`)
- **Managed Identity**: Enabled in Azure Container Apps
- **Environment Variables**: Set `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`

## Local Development

### Prerequisites

- .NET 10 SDK
- Azure CLI
- Access to Azure AI Foundry project

### Setup

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd movie-agent
   ```

2. Create `.env` file from template:
   ```bash
   cp .env.example .env
   ```

3. Configure environment variables in `.env`:
   ```bash
   AZURE_AI_PROJECT_ENDPOINT=https://your-project.cognitiveservices.azure.com/
   AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4o-mini
   ```

4. Authenticate with Azure:
   ```bash
   az login
   ```

5. Run the application:
   ```bash
   cd src/MovieAgent
   dotnet run
   ```

6. Test the endpoint:
   ```bash
   curl -X POST http://localhost:5000/api/movie \
     -H "Content-Type: application/json" \
     -d '{"description": "A movie about dreams within dreams with Leonardo DiCaprio"}'
   ```

## Docker

### Build

```bash
docker build -t movie-agent:latest .
```

### Run

```bash
docker run -p 8080:8080 \
  -e AZURE_AI_PROJECT_ENDPOINT="https://your-project.cognitiveservices.azure.com/" \
  -e AZURE_AI_MODEL_DEPLOYMENT_NAME="gpt-4o-mini" \
  -e AZURE_TENANT_ID="your-tenant-id" \
  -e AZURE_CLIENT_ID="your-client-id" \
  -e AZURE_CLIENT_SECRET="your-client-secret" \
  movie-agent:latest
```

## Azure Container Apps Deployment

### Prerequisites

1. Azure subscription
2. Azure Container Registry (ACR)
3. Azure Container Apps Environment
4. Azure AI Foundry project with model deployment

### Manual Deployment

1. **Create resource group** (if needed):
   ```bash
   az group create --name movie-agent-rg --location eastus
   ```

2. **Create Container Registry**:
   ```bash
   az acr create --resource-group movie-agent-rg \
     --name mymoviewagentacr --sku Basic
   ```

3. **Build and push image**:
   ```bash
   az acr build --registry mymoviewagentacr \
     --image movie-agent:latest .
   ```

4. **Create Container Apps Environment**:
   ```bash
   az containerapp env create \
     --name movie-agent-env \
     --resource-group movie-agent-rg \
     --location eastus
   ```

5. **Deploy Container App**:
   ```bash
   az containerapp create \
     --name movie-agent-app \
     --resource-group movie-agent-rg \
     --environment movie-agent-env \
     --image mymoviewagentacr.azurecr.io/movie-agent:latest \
     --target-port 8080 \
     --ingress external \
     --registry-server mymoviewagentacr.azurecr.io \
     --env-vars \
       AZURE_AI_PROJECT_ENDPOINT="secretref:ai-endpoint" \
       AZURE_AI_MODEL_DEPLOYMENT_NAME="gpt-4o-mini" \
       APPLICATIONINSIGHTS_CONNECTION_STRING="secretref:appinsights-conn" \
     --cpu 0.5 --memory 1.0Gi \
     --min-replicas 1 --max-replicas 5
   ```

### GitHub Actions CI/CD

The repository includes a GitHub Actions workflow for automated deployment.

**Required Secrets:**
- `AZURE_CREDENTIALS`: Azure service principal credentials (JSON)
- `RESOURCE_GROUP`: Azure resource group name
- `CONTAINER_APP_NAME`: Container App name
- `ACR_NAME`: Azure Container Registry name
- `AZURE_AI_PROJECT_ENDPOINT`: Foundry project endpoint
- `AZURE_AI_MODEL_DEPLOYMENT_NAME`: Model deployment name
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: App Insights connection string (optional)

**To set up:**

1. Create a service principal:
   ```bash
   az ad sp create-for-rbac --name "movie-agent-sp" \
     --role contributor \
     --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group} \
     --sdk-auth
   ```

2. Add the JSON output as `AZURE_CREDENTIALS` secret in GitHub repository settings.

3. Add other required secrets to GitHub repository.

4. Push to `main` branch to trigger deployment.

## Observability

### Application-Level OpenTelemetry Export (Recommended)

This repository uses **Azure.Monitor.OpenTelemetry.AspNetCore** for application-level telemetry export to Application Insights.

**Setup:**
1. Create an Application Insights resource in Azure
2. Get the connection string from the resource
3. Set `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable

**Benefits:**
- Full control over telemetry configuration
- Works with any hosting environment
- Supports traces, logs, and metrics
- Automatic instrumentation for ASP.NET Core and HttpClient

**Example:**
```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=xxx;IngestionEndpoint=https://xxx.in.applicationinsights.azure.com/"
```

### Container Apps Managed OpenTelemetry Agent (Alternative)

Azure Container Apps also supports a **managed OpenTelemetry agent** configured at the Container Apps Environment level.

**Important Notes:**
- Managed agent requires code instrumentation (not auto-instrumentation)
- Supports logs and traces to App Insights
- Does NOT support metrics for App Insights destination
- Configured at environment level, not app level

**Setup:**
```bash
az containerapp env telemetry app-insights set \
  --name movie-agent-env \
  --resource-group movie-agent-rg \
  --connection-string "InstrumentationKey=xxx;..."
```

**Documentation:**
- [Collect OpenTelemetry in Container Apps](https://learn.microsoft.com/azure/container-apps/opentelemetry-agents)
- [Container Apps Observability](https://learn.microsoft.com/azure/container-apps/observability)

### GenAI Semantic Conventions

The agent emits OpenTelemetry spans with **GenAI semantic conventions** for AI operations:
- `gen_ai.system`: AI system (e.g., "azure_ai_foundry")
- `gen_ai.operation.name`: Operation name (e.g., "chat.completion")
- `gen_ai.request.model`: Model used
- `gen_ai.usage.input_tokens`: Input token count
- `gen_ai.usage.output_tokens`: Output token count
- `gen_ai.response.finish_reasons`: Completion reason

These conventions enable rich monitoring in Application Insights and other observability platforms.

## Register Agent in Microsoft Foundry Control Plane

Microsoft Foundry Control Plane can register and manage custom agents running in Azure compute, providing centralized monitoring and proxy capabilities through API Management.

### Concept

When you register your custom agent in Foundry:
- **Foundry acts as a proxy** using Azure API Management
- **Centralized management** of all agents (Foundry-hosted and custom)
- **Unified observability** through the Agent Monitoring Dashboard
- **OpenTelemetry integration** for tracking metrics like token usage, latency, and success rate

### Prerequisites

Before registering your agent in Foundry Control Plane:

1. **Azure AI Foundry Project**: You need an active Foundry project
2. **AI Gateway Configured**: An Azure API Management instance configured as your Foundry AI gateway
3. **Agent Endpoint Reachable**: Your agent must be accessible from the Foundry network (public or private endpoint)
4. **Supported Protocol**: HTTP or Agent-to-Agent (A2A) protocol
5. **OpenTelemetry Instrumentation** (Recommended): Agent should emit GenAI semantic conventions for best observability

This repository already implements OpenTelemetry with GenAI semantic conventions, so it's ready for Foundry registration.

### Registration Process

To register this custom agent in Foundry Control Plane, follow the official Microsoft documentation:

**Official Documentation:**
- [Register a custom agent in Foundry Control Plane](https://learn.microsoft.com/azure/ai-studio/how-to/develop/agents-control-plane#register-a-custom-agent)
- [Agent Control Plane Overview](https://learn.microsoft.com/azure/ai-studio/how-to/develop/agents-control-plane)

**High-Level Steps:**
1. Navigate to your Azure AI Foundry project in Azure portal
2. Go to the Agents section
3. Select "Register custom agent"
4. Provide agent endpoint URL (your Container App URL)
5. Configure authentication and protocol settings
6. Validate the registration

### Agent Monitoring Dashboard

Once registered, you can monitor your agent through the **Foundry Agent Monitoring Dashboard**:

**Features:**
- **Token Usage**: Track input/output token consumption
- **Latency Metrics**: Monitor response times
- **Success Rate**: Track successful vs. failed requests
- **Error Analysis**: Investigate failures and exceptions
- **Cost Tracking**: Understand usage costs

**Setup:**
1. Connect Application Insights to your Foundry project
2. Navigate to the "Monitor" tab in your Foundry project
3. View agent metrics and traces

**Documentation:**
- [Agent Monitoring Dashboard](https://learn.microsoft.com/azure/ai-studio/how-to/develop/agents-control-plane#monitoring-and-observability)
- [Agent Tracing and Monitoring](https://learn.microsoft.com/azure/ai-studio/how-to/develop/trace-production-sdk)
- [Trace and Evaluate with OpenTelemetry](https://learn.microsoft.com/azure/ai-studio/how-to/develop/trace-production-sdk#opentelemetry)

### Why Register with Foundry?

- **Unified Management**: Manage all agents (Foundry and custom) in one place
- **Enhanced Security**: Use Foundry's API Management for authentication and rate limiting
- **Better Monitoring**: Leverage Foundry's built-in observability dashboards
- **Cost Optimization**: Track and optimize token usage across all agents
- **Compliance**: Centralized logging and auditing

## Architecture

```
┌─────────────────┐
│   Client App    │
└────────┬────────┘
         │ HTTP POST /api/movie
         ▼
┌─────────────────────────────────┐
│  Azure Container Apps           │
│  ┌───────────────────────────┐  │
│  │   MovieAgent API          │  │
│  │  (ASP.NET Core)           │  │
│  └───────────┬───────────────┘  │
│              │                   │
│  ┌───────────▼───────────────┐  │
│  │   MovieFinderAgent        │  │
│  │  (Agent Framework)        │  │
│  └───────────┬───────────────┘  │
│              │                   │
│  ┌───────────▼───────────────┐  │
│  │  OpenTelemetryChatClient  │  │
│  │  (with GenAI conventions) │  │
│  └───────────┬───────────────┘  │
└──────────────┼───────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  Azure AI Foundry               │
│  ┌───────────────────────────┐  │
│  │   Model Deployment        │  │
│  │   (gpt-4o-mini, etc.)     │  │
│  └───────────────────────────┘  │
└─────────────────────────────────┘
               │
               │ Telemetry
               ▼
┌─────────────────────────────────┐
│  Application Insights           │
│  (Azure Monitor)                │
└─────────────────────────────────┘
```

## Code Structure

```
movie-agent/
├── src/
│   └── MovieAgent/
│       ├── Agent/
│       │   ├── MovieFinderAgent.cs          # Main agent logic
│       │   └── OpenTelemetryChatClient.cs   # OTel wrapper
│       ├── Models/
│       │   ├── MovieRequest.cs              # Request DTO
│       │   └── MovieResponse.cs             # Response DTO
│       ├── Observability/
│       │   └── OpenTelemetryConfiguration.cs # OTel setup
│       ├── Program.cs                        # Application entry point
│       └── MovieAgent.csproj                 # Project file
├── .github/
│   └── workflows/
│       └── deploy.yml                        # CI/CD workflow
├── Dockerfile                                # Multi-stage build
├── .env.example                              # Environment template
└── README.md                                 # This file
```

## Accuracy and Limitations

⚠️ **Important**: This agent uses AI models without external data sources by default.

**Limitations:**
- Movie information is based on model training data
- IMDb ratings may be outdated or approximate
- Cast and crew information may be incomplete
- Confidence levels indicate uncertainty

**Recommendations:**
- Use `confidence` field to assess reliability
- Check `notes` field for accuracy warnings
- For production use with high accuracy requirements, consider integrating external APIs (OMDb, TMDb)

### Optional: Adding External Data Tools

To improve accuracy, you can add an optional "data tool" pattern:

1. Create an interface for movie data providers
2. Implement OMDb or TMDb API client
3. Add behind a feature flag
4. Configure API key via environment variable
5. Update agent to use tool when available

This repository keeps the default implementation tool-less for simplicity.

## License

MIT

## Support

For issues and questions:
- Open an issue in this repository
- Refer to [Microsoft Agent Framework documentation](https://learn.microsoft.com/azure/ai-studio/how-to/develop/agents)
- Check [Azure Container Apps documentation](https://learn.microsoft.com/azure/container-apps/)
