using MovieAgent.Models;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace MovieAgent.Agent;

public class MovieFinderAgent
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<MovieFinderAgent> _logger;
    private readonly string _agentName;
    private readonly string _agentInstructions;

    public MovieFinderAgent(
        IChatClient chatClient,
        ILogger<MovieFinderAgent> logger,
        IConfiguration configuration)
    {
        _chatClient = chatClient;
        _logger = logger;
        _agentName = configuration["MOVIE_AGENT_NAME"] ?? "MovieFinder";
        _agentInstructions = configuration["MOVIE_AGENT_INSTRUCTIONS"] ?? GetDefaultInstructions();
    }

    private static string GetDefaultInstructions()
    {
        return """
        You are a movie identification expert. Your task is to identify movies based on user descriptions.
        
        When given a movie description, you must respond with a JSON object containing:
        - title: The movie title
        - releaseYear: The year or date the movie was released
        - director: The director's name
        - cast: An array of main actors/actresses
        - imdbRating: The IMDb rating (or approximate if uncertain)
        - confidence: One of "low", "medium", or "high" based on how certain you are
        - notes: Any relevant notes about accuracy or uncertainty
        
        IMPORTANT: 
        - Always respond with valid JSON matching this schema
        - If uncertain about IMDb ratings or cast, mark confidence as "low" or "medium"
        - In notes, mention if information may be approximate or outdated
        - Be honest about uncertainty rather than inventing information
        
        Example response format:
        {
          "title": "The Matrix",
          "releaseYear": "1999",
          "director": "The Wachowskis",
          "cast": ["Keanu Reeves", "Laurence Fishburne", "Carrie-Anne Moss"],
          "imdbRating": "8.7",
          "confidence": "high",
          "notes": "IMDb rating is approximate and may have changed since training data."
        }
        """;
    }

    public async Task<MovieResponse> FindMovieAsync(MovieRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing movie search request: {Description}", request.Description);

        // Build conversation history
        var messages = new List<Microsoft.Extensions.AI.ChatMessage>();
        
        // Add system message with instructions
        messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, _agentInstructions));

        // Add conversation history if provided
        if (request.History != null && request.History.Count > 0)
        {
            foreach (var msg in request.History)
            {
                var role = msg.Role.ToLowerInvariant() == "assistant" ? ChatRole.Assistant : ChatRole.User;
                messages.Add(new Microsoft.Extensions.AI.ChatMessage(role, msg.Content));
            }
        }

        // Add the current user message
        messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, request.Description));

        try
        {
            // Call the chat client
            var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
            var rawAnswer = response.Text ?? string.Empty;

            _logger.LogInformation("Received response from AI model");

            // Parse the JSON response
            var movieResult = ParseMovieResult(rawAnswer);

            var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();

            return new MovieResponse
            {
                ConversationId = conversationId,
                Result = movieResult,
                RawAnswer = rawAnswer
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing movie search request");
            throw;
        }
    }

    private MovieResult ParseMovieResult(string rawAnswer)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonStart = rawAnswer.IndexOf('{');
            var jsonEnd = rawAnswer.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = rawAnswer.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var result = JsonSerializer.Deserialize<MovieResult>(jsonContent, options);
                if (result != null)
                {
                    return result;
                }
            }

            _logger.LogWarning("Failed to parse JSON response, creating error result");
            
            // If parsing fails, return an error result
            return new MovieResult
            {
                Title = "Unknown",
                ReleaseYear = "Unknown",
                Director = "Unknown",
                Cast = new List<string>(),
                ImdbRating = "N/A",
                Confidence = "low",
                Notes = $"Failed to parse response. Raw answer: {rawAnswer}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing movie result");
            return new MovieResult
            {
                Title = "Error",
                ReleaseYear = "Unknown",
                Director = "Unknown",
                Cast = new List<string>(),
                ImdbRating = "N/A",
                Confidence = "low",
                Notes = $"Error parsing response: {ex.Message}"
            };
        }
    }
}
