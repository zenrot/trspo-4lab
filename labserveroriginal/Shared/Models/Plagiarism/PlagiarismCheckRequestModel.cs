namespace LabServer.Shared.Models.Plagiarism;

public class PlagiarismCheckRequestModel
{
    public ICollection<CodeFileModel> SubmissionFiles { get; set; } = new List<CodeFileModel>();
    public ICollection<CodeFileModel> ReferenceFiles { get; set; } = new List<CodeFileModel>();
    public System.Double? Threshold { get; set; }
}
