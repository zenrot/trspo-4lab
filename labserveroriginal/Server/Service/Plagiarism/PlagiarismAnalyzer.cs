namespace LabServer.Server.Service;

using LabServer.Shared.Models.Plagiarism;

public interface IPlagiarismAnalyzer
{
    Task<PlagiarismCheckResponseModel> AnalyzeAsync(
        IReadOnlyList<CodeFileModel> submissionFiles,
        IReadOnlyList<CodeFileModel> referenceFiles,
        System.Double? threshold = null,
        CancellationToken cancellationToken = default);
}

public class PlagiarismAnalyzer : IPlagiarismAnalyzer
{
    private readonly CodeNormalizer _normalizer;
    private readonly TokenSimilarity _tokenSimilarity;
    private readonly ILlmCodeSimilarityAnalyzer _llmAnalyzer;
    private readonly IConfiguration _configuration;

    public PlagiarismAnalyzer(
        CodeNormalizer normalizer,
        TokenSimilarity tokenSimilarity,
        ILlmCodeSimilarityAnalyzer llmAnalyzer,
        IConfiguration configuration)
    {
        _normalizer = normalizer;
        _tokenSimilarity = tokenSimilarity;
        _llmAnalyzer = llmAnalyzer;
        _configuration = configuration;
    }

    public async Task<PlagiarismCheckResponseModel> AnalyzeAsync(
        IReadOnlyList<CodeFileModel> submissionFiles,
        IReadOnlyList<CodeFileModel> referenceFiles,
        System.Double? threshold = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveThreshold = threshold ?? _configuration.GetValue("Plagiarism:Threshold", 0.75);
        if (submissionFiles.Count == 0)
        {
            return new PlagiarismCheckResponseModel
            {
                Success = false,
                Threshold = effectiveThreshold,
                Message = "No source files were found in submission"
            };
        }

        if (referenceFiles.Count == 0)
        {
            return new PlagiarismCheckResponseModel
            {
                Success = false,
                Threshold = effectiveThreshold,
                Message = "No reference source files were found"
            };
        }

        var submissionCode = System.String.Join(Environment.NewLine, submissionFiles.Select(f => f.Content));
        var submissionTokens = _normalizer.NormalizeToTokens(submissionCode);
        var matches = new List<PlagiarismMatchModel>();
        var maxLlmCandidates = _configuration.GetValue("Plagiarism:MaxLlmCandidates", 3);

        var tokenCandidates = referenceFiles
            .Select(reference =>
            {
                var referenceTokens = _normalizer.NormalizeToTokens(reference.Content);
                var tokenScore = _tokenSimilarity.Compare(submissionTokens, referenceTokens);
                return new
                {
                    Reference = reference,
                    TokenScore = tokenScore
                };
            })
            .OrderByDescending(candidate => candidate.TokenScore)
            .Take(Math.Max(1, maxLlmCandidates))
            .ToList();

        foreach (var candidate in tokenCandidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var llmResult = await _llmAnalyzer.AnalyzeAsync(
                submissionCode,
                candidate.Reference.Content,
                candidate.TokenScore,
                cancellationToken);
            var finalScore = (candidate.TokenScore * 0.45) + (llmResult.Similarity * 0.55);

            matches.Add(new PlagiarismMatchModel
            {
                ReferenceFile = candidate.Reference.FileName,
                Similarity = Math.Round(finalScore, 3),
                TokenSimilarity = Math.Round(candidate.TokenScore, 3),
                LlmSimilarity = Math.Round(llmResult.Similarity, 3),
                Explanation = llmResult.Explanation
            });
        }

        var orderedMatches = matches
            .OrderByDescending(match => match.Similarity)
            .Take(5)
            .ToList();
        var bestMatch = orderedMatches.First();

        return new PlagiarismCheckResponseModel
        {
            Success = true,
            IsPlagiarism = bestMatch.Similarity >= effectiveThreshold,
            Similarity = bestMatch.Similarity,
            Threshold = effectiveThreshold,
            Message = bestMatch.Similarity >= effectiveThreshold
                ? $"Possible plagiarism: closest file is {bestMatch.ReferenceFile}"
                : $"No plagiarism detected: closest file is {bestMatch.ReferenceFile}",
            Matches = orderedMatches
        };
    }
}
