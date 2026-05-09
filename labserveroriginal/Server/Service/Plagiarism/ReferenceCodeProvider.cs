namespace LabServer.Server.Service;

using LabServer.Shared.Models.Plagiarism;

public interface IReferenceCodeProvider
{
    IReadOnlyList<CodeFileModel> GetReferenceFiles();
}

public class ReferenceCodeProvider : IReferenceCodeProvider
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IOnlineLearningCorpus _learningCorpus;

    public ReferenceCodeProvider(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IOnlineLearningCorpus learningCorpus)
    {
        _configuration = configuration;
        _environment = environment;
        _learningCorpus = learningCorpus;
    }

    public IReadOnlyList<CodeFileModel> GetReferenceFiles()
    {
        var configuredDirectory = _configuration.GetValue<System.String>("Plagiarism:ReferenceDirectory");
        var referenceDirectory = System.String.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", ".."))
            : Path.GetFullPath(configuredDirectory);

        var files = new List<CodeFileModel>();

        if (Directory.Exists(referenceDirectory))
        {
            var configuredFiles = _configuration
                .GetSection("Plagiarism:ReferenceFiles")
                .Get<System.String[]>() ?? Array.Empty<System.String>();
            var referencePaths = configuredFiles.Length == 0
                ? Directory.EnumerateFiles(referenceDirectory, "*.c", SearchOption.TopDirectoryOnly)
                : configuredFiles.Select(fileName => Path.Combine(referenceDirectory, fileName)).Where(File.Exists);

            files.AddRange(referencePaths.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .Select(path => new CodeFileModel
                {
                    FileName = Path.GetFileName(path),
                    Content = File.ReadAllText(path)
                }));
        }

        files.AddRange(_learningCorpus.GetLearnedFiles());

        return files
            .GroupBy(file => file.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }
}
