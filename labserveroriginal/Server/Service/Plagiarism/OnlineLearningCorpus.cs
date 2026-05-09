namespace LabServer.Server.Service;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using LabServer.Shared.Models.Plagiarism;

public interface IOnlineLearningCorpus
{
    IReadOnlyList<CodeFileModel> GetLearnedFiles();
    Task LearnAsync(
        IReadOnlyList<CodeFileModel> files,
        System.Int64 sourceProjectId,
        System.String commitHash,
        CancellationToken cancellationToken = default);
}

public class OnlineLearningCorpus : IOnlineLearningCorpus
{
    private readonly IConfiguration _configuration;
    private readonly object _syncRoot = new();

    public OnlineLearningCorpus(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IReadOnlyList<CodeFileModel> GetLearnedFiles()
    {
        var directory = GetLearningDirectory();
        if (!Directory.Exists(directory))
            return Array.Empty<CodeFileModel>();

        return Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(ReadRecord)
            .Where(record => record != null)
            .Select(record => new CodeFileModel
            {
                FileName = $"learned/{record!.SourceProjectId}/{record.CommitHash}/{record.FileName}",
                Content = record.Content
            })
            .ToList();
    }

    public async Task LearnAsync(
        IReadOnlyList<CodeFileModel> files,
        System.Int64 sourceProjectId,
        System.String commitHash,
        CancellationToken cancellationToken = default)
    {
        if (files.Count == 0)
            return;

        var directory = GetLearningDirectory();
        Directory.CreateDirectory(directory);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var record = new LearnedCodeRecord
            {
                SourceProjectId = sourceProjectId,
                CommitHash = commitHash,
                FileName = file.FileName,
                Content = file.Content,
                LearnedAtUtc = DateTime.UtcNow
            };

            var id = ComputeStableId(sourceProjectId, commitHash, file.FileName, file.Content);
            var path = Path.Combine(directory, $"{id}.json");
            var json = JsonSerializer.Serialize(record, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            lock (_syncRoot)
            {
                File.WriteAllText(path, json);
            }
        }

        await Task.CompletedTask;
    }

    private System.String GetLearningDirectory()
        => Path.GetFullPath(_configuration.GetValue<System.String>("Plagiarism:LearningDirectory") ?? "plagiarism-learning");

    private static LearnedCodeRecord? ReadRecord(System.String path)
    {
        try
        {
            return JsonSerializer.Deserialize<LearnedCodeRecord>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    private static System.String ComputeStableId(
        System.Int64 sourceProjectId,
        System.String commitHash,
        System.String fileName,
        System.String content)
    {
        var input = $"{sourceProjectId}:{commitHash}:{fileName}:{content}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private class LearnedCodeRecord
    {
        public System.Int64 SourceProjectId { get; set; }
        public System.String CommitHash { get; set; } = System.String.Empty;
        public System.String FileName { get; set; } = System.String.Empty;
        public System.String Content { get; set; } = System.String.Empty;
        public DateTime LearnedAtUtc { get; set; }
    }
}
