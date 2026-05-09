namespace LabServer.Server.Controllers;

using System.IO.Compression;
using System.Text;

using LabServer.Server.Service;
using LabServer.Shared.Models.Plagiarism;
using LabServer.Shared.Models.TestAPI;
using Microsoft.Extensions.Configuration;

public class PlagiarismTestControllerTests
{
    [Fact]
    public async Task ScheduleAndGetResult_ReturnsFailedTestForDetectedPlagiarism()
    {
        var controller = new PlagiarismTestController(
            new SubmissionCodeExtractor(),
            new StaticReferenceCodeProvider(),
            BuildAnalyzer(),
            new InMemoryLearningCorpus());

        var schedule = await controller.ScheduleTest(new ScheduleTestRequestModel
        {
            MergeRequest = new MergeRequestData
            {
                SourceProjectId = 15,
                CommitHash = "abc"
            },
            RepoArchiveBase64 = Convert.ToBase64String(CreateZip(("main.c", "int sum(int a, int b) { return a + b; }"))),
            ChangedFilePaths = new[] { "main.c" }
        });

        var result = await controller.GetTestResult(new GetTestResultRequestModel
        {
            SourceProjectId = 15,
            CommitHash = "abc"
        });

        Assert.True(schedule.Success);
        Assert.True(result.TestCompleted);
        Assert.False(result.Success);
        Assert.Contains("Possible plagiarism", result.Message);
    }

    [Fact]
    public async Task ScheduleTest_LearnsOnlyChangedFiles()
    {
        var learningCorpus = new InMemoryLearningCorpus();
        var controller = new PlagiarismTestController(
            new SubmissionCodeExtractor(),
            new StaticReferenceCodeProvider(),
            BuildAnalyzer(),
            learningCorpus);

        await controller.ScheduleTest(new ScheduleTestRequestModel
        {
            MergeRequest = new MergeRequestData
            {
                SourceProjectId = 16,
                CommitHash = "def"
            },
            RepoArchiveBase64 = Convert.ToBase64String(CreateZip(
                ("main.c", "int changed(void) { return 1; }"),
                ("old.c", "int old_file(void) { return 0; }"))),
            ChangedFilePaths = new[] { "main.c" }
        });

        var learned = learningCorpus.GetLearnedFiles();
        Assert.Single(learned);
        Assert.Equal("main.c", learned[0].FileName);
        Assert.Contains("changed", learned[0].Content);
    }

    private static PlagiarismAnalyzer BuildAnalyzer()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<System.String, System.String?>
            {
                ["Plagiarism:Threshold"] = "0.7"
            })
            .Build();

        return new PlagiarismAnalyzer(
            new CodeNormalizer(),
            new TokenSimilarity(),
            new EchoLlmAnalyzer(),
            configuration);
    }

    private static System.Byte[] CreateZip(params (System.String FileName, System.String Content)[] files)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.FileName);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write(file.Content);
            }
        }

        return stream.ToArray();
    }

    private class StaticReferenceCodeProvider : IReferenceCodeProvider
    {
        public IReadOnlyList<CodeFileModel> GetReferenceFiles()
            => new[]
            {
                new CodeFileModel
                {
                    FileName = "reference.c",
                    Content = "int add(int left, int right) { return left + right; }"
                }
            };
    }

    private class EchoLlmAnalyzer : ILlmCodeSimilarityAnalyzer
    {
        public Task<LlmSimilarityResult> AnalyzeAsync(
            System.String submissionCode,
            System.String referenceCode,
            System.Double tokenSimilarity,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new LlmSimilarityResult(tokenSimilarity, "test llm response"));
    }

    private class InMemoryLearningCorpus : IOnlineLearningCorpus
    {
        private readonly List<CodeFileModel> _learnedFiles = new();

        public IReadOnlyList<CodeFileModel> GetLearnedFiles() => _learnedFiles;

        public Task LearnAsync(
            IReadOnlyList<CodeFileModel> files,
            System.Int64 sourceProjectId,
            System.String commitHash,
            CancellationToken cancellationToken = default)
        {
            _learnedFiles.AddRange(files);
            return Task.CompletedTask;
        }
    }
}
