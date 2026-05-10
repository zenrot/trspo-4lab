namespace LabServer.Server.Service;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public record LlmSimilarityResult(System.Double Similarity, System.String Explanation);

public interface ILlmCodeSimilarityAnalyzer
{
    Task<LlmSimilarityResult> AnalyzeAsync(
        System.String submissionCode,
        System.String referenceCode,
        System.Double tokenSimilarity,
        CancellationToken cancellationToken = default);
}

public class OllamaGemmaAnalyzer : ILlmCodeSimilarityAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OllamaGemmaAnalyzer(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.GetValue("Plagiarism:Ollama:TimeoutSeconds", 60));
    }

    public async Task<LlmSimilarityResult> AnalyzeAsync(
        System.String submissionCode,
        System.String referenceCode,
        System.Double tokenSimilarity,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration.GetValue<System.String>("Plagiarism:Ollama:BaseUrl") ?? "http://localhost:11434";
        var model = _configuration.GetValue<System.String>("Plagiarism:Ollama:Model") ?? "qwen2.5-coder:0.5b";
        var prompt = BuildPrompt(submissionCode, referenceCode, tokenSimilarity);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{baseUrl.TrimEnd('/')}/api/generate",
                new OllamaGenerateRequest(model, prompt, Stream: false),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new LlmSimilarityResult(tokenSimilarity, $"LLM request failed: {(System.Int32)response.StatusCode}");

            var body = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken);
            if (body?.Response == null)
                return new LlmSimilarityResult(tokenSimilarity, "LLM response is empty");

            var parsed = ParseModelJson(body.Response);
            return parsed ?? new LlmSimilarityResult(tokenSimilarity, "LLM response could not be parsed; token score was used");
        }
        catch (Exception ex)
        {
            return new LlmSimilarityResult(tokenSimilarity, $"LLM unavailable; token score was used: {ex.Message}");
        }
    }

    private static System.String BuildPrompt(System.String submissionCode, System.String referenceCode, System.Double tokenSimilarity)
        => $$"""
        You are a code plagiarism analyzer. Compare two C/C++/C# code fragments.
        Return only JSON in this exact format: {"similarity":0.0,"explanation":"short reason"}.
        similarity must be between 0 and 1.

        Code A:
        ```c
        {{TrimForPrompt(submissionCode)}}
        ```

        Code B:
        ```c
        {{TrimForPrompt(referenceCode)}}
        ```
        """;

    private static System.String TrimForPrompt(System.String code)
        => code.Length <= 8000 ? code : code[..8000];

    private static LlmSimilarityResult? ParseModelJson(System.String response)
    {
        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;

        var json = response[start..(end + 1)];
        try
        {
            var parsed = JsonSerializer.Deserialize<LlmJsonResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null)
                return null;

            return new LlmSimilarityResult(Math.Clamp(parsed.Similarity, 0, 1), parsed.Explanation ?? System.String.Empty);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private record OllamaGenerateRequest(
        [property: JsonPropertyName("model")] System.String Model,
        [property: JsonPropertyName("prompt")] System.String Prompt,
        [property: JsonPropertyName("stream")] System.Boolean Stream);

    private class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public System.String? Response { get; set; }
    }

    private class LlmJsonResult
    {
        public System.Double Similarity { get; set; }
        public System.String? Explanation { get; set; }
    }
}
