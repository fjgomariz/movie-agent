namespace MovieAgent.Models;

public class MovieResponse
{
    public required string ConversationId { get; set; }
    public required MovieResult Result { get; set; }
    public required string RawAnswer { get; set; }
}

public class MovieResult
{
    public required string Title { get; set; }
    public required string ReleaseYear { get; set; }
    public required string Director { get; set; }
    public required List<string> Cast { get; set; }
    public required string ImdbRating { get; set; }
    public required string Confidence { get; set; }
    public required string Notes { get; set; }
}
