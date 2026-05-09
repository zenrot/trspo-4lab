namespace LabServer.Server.Controllers;

using System.Collections.Concurrent;

using Microsoft.AspNetCore.Mvc;

using LabServer.Server.Service;
using LabServer.Shared.Models.Plagiarism;
using LabServer.Shared.Models.TestAPI;

[Route("test/plagiarism")]
[ApiController]
public class PlagiarismTestController : Controller
{
    private static readonly ConcurrentDictionary<Tuple<System.Int64, System.String>, PlagiarismCheckResponseModel> s_testResults = new();

    private readonly ISubmissionCodeExtractor _codeExtractor;
    private readonly IReferenceCodeProvider _referenceCodeProvider;
    private readonly IPlagiarismAnalyzer _plagiarismAnalyzer;
    private readonly IOnlineLearningCorpus _learningCorpus;

    public PlagiarismTestController(
        ISubmissionCodeExtractor codeExtractor,
        IReferenceCodeProvider referenceCodeProvider,
        IPlagiarismAnalyzer plagiarismAnalyzer,
        IOnlineLearningCorpus learningCorpus)
    {
        _codeExtractor = codeExtractor;
        _referenceCodeProvider = referenceCodeProvider;
        _plagiarismAnalyzer = plagiarismAnalyzer;
        _learningCorpus = learningCorpus;
    }

    [HttpPost("schedule")]
    public async Task<ScheduleTestResponseModel> ScheduleTest([FromBody] ScheduleTestRequestModel scheduleTestRequest)
    {
        var key = new Tuple<long, string>(
            scheduleTestRequest.MergeRequest.SourceProjectId,
            scheduleTestRequest.MergeRequest.CommitHash);

        var submissionFiles = FilterChangedFiles(
            _codeExtractor.Extract(scheduleTestRequest.RepoArchive),
            scheduleTestRequest.ChangedFilePaths);
        var referenceFiles = _referenceCodeProvider.GetReferenceFiles();
        var result = await _plagiarismAnalyzer.AnalyzeAsync(submissionFiles, referenceFiles);
        if (result.Success)
        {
            await _learningCorpus.LearnAsync(
                submissionFiles,
                scheduleTestRequest.MergeRequest.SourceProjectId,
                scheduleTestRequest.MergeRequest.CommitHash);
        }

        s_testResults.AddOrUpdate(key, result, (_, _) => result);
        return new ScheduleTestResponseModel
        {
            Success = result.Success
        };
    }

    private static IReadOnlyList<CodeFileModel> FilterChangedFiles(
        IReadOnlyList<CodeFileModel> extractedFiles,
        ICollection<System.String> changedFilePaths)
    {
        if (changedFilePaths.Count == 0)
            return extractedFiles;

        var changedNames = changedFilePaths
            .Select(NormalizePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return extractedFiles
            .Where(file =>
            {
                var normalized = NormalizePath(file.FileName);
                return changedNames.Any(changed => normalized.EndsWith(changed, StringComparison.OrdinalIgnoreCase));
            })
            .ToList();
    }

    private static System.String NormalizePath(System.String path)
        => path.Replace('\\', '/').TrimStart('/');

    [HttpPost("getresult")]
    public Task<GetTestResultResponseModel> GetTestResult([FromBody] GetTestResultRequestModel getTestResultRequest)
    {
        var key = new Tuple<long, string>(
            getTestResultRequest.SourceProjectId,
            getTestResultRequest.CommitHash);

        if (!s_testResults.Remove(key, out var result))
        {
            return Task.FromResult(new GetTestResultResponseModel
            {
                TestCompleted = false,
                Success = null,
                Message = "Plagiarism analysis is not completed"
            });
        }

        var message = BuildMessage(result);
        return Task.FromResult(new GetTestResultResponseModel
        {
            TestCompleted = true,
            Success = result.Success && !result.IsPlagiarism,
            Message = message
        });
    }

    private static System.String BuildMessage(PlagiarismCheckResponseModel result)
    {
        if (!result.Success)
            return result.Message;

        var bestMatch = result.Matches.FirstOrDefault();
        var bestMatchText = bestMatch == null
            ? "no matches"
            : $"{bestMatch.ReferenceFile}: {bestMatch.Similarity:P1}";

        return $"{result.Message}. Similarity={result.Similarity:P1}, threshold={result.Threshold:P1}, best match={bestMatchText}";
    }
}
