namespace LabServer.Server.Service;

using LabServer.Shared.Models.Plagiarism;
using Microsoft.Extensions.Configuration;

public class PlagiarismAnalyzerTests
{
    [Fact]
    public async Task AnalyzeAsync_DetectsStructurallySimilarCode()
    {
        var analyzer = BuildAnalyzer();
        var submission = new[]
        {
            new CodeFileModel
            {
                FileName = "submission.c",
                Content = "int add(int x, int y) { int z = x + y; return z; }"
            }
        };
        var references = new[]
        {
            new CodeFileModel
            {
                FileName = "reference.c",
                Content = "int sum(int a, int b) { int result = a + b; return result; }"
            }
        };

        var result = await analyzer.AnalyzeAsync(submission, references, threshold: 0.7);

        Assert.True(result.Success);
        Assert.True(result.IsPlagiarism);
        Assert.Equal("reference.c", result.Matches.First().ReferenceFile);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsFailureForEmptySubmission()
    {
        var analyzer = BuildAnalyzer();

        var result = await analyzer.AnalyzeAsync(
            Array.Empty<CodeFileModel>(),
            new[] { new CodeFileModel { FileName = "reference.c", Content = "int main() { return 0; }" } });

        Assert.False(result.Success);
        Assert.Contains("No source files", result.Message);
    }

    private static PlagiarismAnalyzer BuildAnalyzer()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<System.String, System.String?>
            {
                ["Plagiarism:Threshold"] = "0.75"
            })
            .Build();

        return new PlagiarismAnalyzer(
            new CodeNormalizer(),
            new TokenSimilarity(),
            new EchoLlmAnalyzer(),
            configuration);
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
}
