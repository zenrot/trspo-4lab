namespace LabServer.Shared.Models.Plagiarism;

public class PlagiarismCheckResponseModel
{
    public System.Boolean Success { get; set; }
    public System.Boolean IsPlagiarism { get; set; }
    public System.Double Threshold { get; set; }
    public System.Double Similarity { get; set; }
    public System.String Message { get; set; } = System.String.Empty;
    public ICollection<PlagiarismMatchModel> Matches { get; set; } = new List<PlagiarismMatchModel>();
}
