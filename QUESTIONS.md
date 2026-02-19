# Questions Before Finalizing

As specified in the requirements, these questions need to be answered before completing the implementation:

## 1. Foundry Connection Style

**Question:** Which Foundry connection style should be used in this repo: direct model deployment or Foundry Agents (persistent) via AZURE_AI_PROJECT_ENDPOINT?

**Current Implementation:** The repository currently uses **direct model deployment** via Azure OpenAI endpoints. This approach:
- Connects directly to Azure AI Foundry model endpoints
- Uses `AzureOpenAIClient` with the project endpoint
- Does not create or manage persistent agents
- Is simpler and more straightforward for stateless operations

**Alternative:** We could switch to using persistent Foundry Agents via the Azure.AI.Projects SDK, which would:
- Allow reusing existing agent configurations
- Support the `AZURE_AI_AGENT_ID` environment variable
- Provide better integration with Foundry's agent management features
- Be more aligned with the "Agent Framework" concept

**Reference:** [Learn more about Foundry Agents](https://learn.microsoft.com/azure/ai-studio/how-to/develop/agents)

## 2. Response Parsing Strategy

**Question:** Should the response be strict JSON only (fail if parsing fails) or best-effort (attempt repair)?

**Current Implementation:** The repository uses a **best-effort approach**:
- Attempts to extract JSON from the response using substring matching
- Falls back to an error result if parsing fails completely
- Returns a MovieResult with error information rather than throwing exceptions

**Alternative:** We could implement strict JSON parsing that:
- Fails immediately if the response is not valid JSON
- Returns a 500 error to the client
- Provides clearer feedback when the model doesn't follow instructions
- Could be more reliable but less forgiving

**Trade-offs:**
- **Best-effort**: More resilient, better user experience, but may hide model instruction-following issues
- **Strict JSON**: Clearer errors, easier debugging, but less forgiving of model variations

## 3. External Movie Data Tool

**Question:** Do we want an optional external "movie facts" tool for better accuracy (e.g., OMDb) behind a flag, or keep it model-only?

**Current Implementation:** The repository is **model-only**:
- No external API integrations
- Relies entirely on the AI model's training data
- Includes confidence scoring and disclaimer notes
- Simpler setup with no additional API keys required

**Alternative:** We could add an optional OMDb or TMDb integration:
- More accurate and up-to-date movie information
- Real-time IMDb ratings
- Would require API key configuration
- Could be toggled with a feature flag like `USE_EXTERNAL_DATA_TOOL=true`
- Would need implementation of data tool pattern with agent framework

**Accuracy Considerations:**
- Model-only: Simpler but may have outdated or approximate information
- With external tool: More accurate but requires API subscription and adds complexity

## 4. Default Model Deployment Name

**Question:** What is the preferred default model deployment name (e.g., gpt-4o-mini)?

**Current Implementation:** The default is set to **`gpt-4o-mini`** in the code:
```csharp
var modelDeploymentName = builder.Configuration["AZURE_AI_MODEL_DEPLOYMENT_NAME"] ?? "gpt-4o-mini";
```

**Considerations:**
- `gpt-4o-mini`: Cost-effective, fast, suitable for most movie identification tasks
- `gpt-4o`: More capable, better reasoning, but more expensive
- `gpt-4`: Most capable, best for complex queries, highest cost
- The value should match what's deployed in the user's Foundry project

**Recommendation:** Keep `gpt-4o-mini` as default since:
- It's cost-effective for production use
- Sufficient capability for movie identification
- Users can easily override via environment variable
- Balances performance and cost

---

## Summary of Current Choices

Based on the implementation, the current defaults are:

1. **Connection Style**: Direct model deployment (not persistent agents)
2. **Parsing Strategy**: Best-effort with fallback
3. **External Tool**: Model-only (no external APIs)
4. **Default Model**: gpt-4o-mini

These choices prioritize simplicity, cost-effectiveness, and ease of deployment while still meeting the core requirements.

## Next Steps

Please review these questions and provide answers. Based on your preferences, I can:
- Maintain the current implementation (recommended for MVP)
- Switch to persistent Foundry agents if needed
- Add strict JSON parsing if preferred
- Implement optional external data tool integration
- Change the default model deployment name

