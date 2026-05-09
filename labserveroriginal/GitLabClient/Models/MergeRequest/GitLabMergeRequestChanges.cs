namespace GitLab.Models.MergeRequest;

using System.Text.Json.Serialization;

public class GitLabMergeRequestChange
{
    [JsonPropertyName("new_path")]
    public System.String NewPath { get; set; } = System.String.Empty;
    [JsonPropertyName("old_path")]
    public System.String OldPath { get; set; } = System.String.Empty;
    [JsonPropertyName("deleted_file")]
    public System.Boolean DeletedFile { get; set; }
    [JsonPropertyName("renamed_file")]
    public System.Boolean RenamedFile { get; set; }
    [JsonPropertyName("new_file")]
    public System.Boolean NewFile { get; set; }
}

public class GitLabMergeRequestChanges
{
    [JsonPropertyName("changes")]
    public List<GitLabMergeRequestChange> Changes { get; set; } = new();
}
