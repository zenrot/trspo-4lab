namespace LabServer.Server.Service;

using System.IO.Compression;
using System.Text;
using LabServer.Shared.Models.Plagiarism;

public interface ISubmissionCodeExtractor
{
    IReadOnlyList<CodeFileModel> Extract(System.Byte[] archiveBytes);
}

public class SubmissionCodeExtractor : ISubmissionCodeExtractor
{
    private static readonly HashSet<System.String> s_supportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".c", ".h", ".cpp", ".hpp", ".cc", ".cxx", ".cs"
    };

    public IReadOnlyList<CodeFileModel> Extract(System.Byte[] archiveBytes)
    {
        if (archiveBytes.Length == 0)
            return Array.Empty<CodeFileModel>();

        if (!IsZipArchive(archiveBytes))
        {
            return new[]
            {
                new CodeFileModel
                {
                    FileName = "submission.c",
                    Content = Encoding.UTF8.GetString(archiveBytes)
                }
            };
        }

        using var stream = new MemoryStream(archiveBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var files = new List<CodeFileModel>();

        foreach (var entry in archive.Entries)
        {
            if (entry.Length == 0 || !s_supportedExtensions.Contains(Path.GetExtension(entry.FullName)))
                continue;

            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            files.Add(new CodeFileModel
            {
                FileName = entry.FullName,
                Content = reader.ReadToEnd()
            });
        }

        return files;
    }

    private static System.Boolean IsZipArchive(System.Byte[] bytes)
        => bytes.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4b;
}
