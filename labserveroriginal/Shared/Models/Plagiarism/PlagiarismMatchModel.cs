namespace LabServer.Shared.Models.Plagiarism;

public class PlagiarismMatchModel
{
    public System.String ReferenceFile { get; set; } = System.String.Empty;
    public System.Double Similarity { get; set; }
    public System.Double TokenSimilarity { get; set; }
    public System.Double LlmSimilarity { get; set; }
    public System.String Explanation { get; set; } = System.String.Empty;
}
