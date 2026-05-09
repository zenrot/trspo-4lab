namespace LabServer.Server.Service;

public class CodeNormalizerTests
{
    [Fact]
    public void RemoveCComments_PreservesCommentMarkersInsideStrings()
    {
        var normalizer = new CodeNormalizer();
        var source = """
            int main() {
                printf("not a // comment");
                printf("not a /* comment */");
                // remove this
                return 0;
            }
            """;

        var result = normalizer.RemoveCComments(source);

        Assert.Contains("\"not a // comment\"", result);
        Assert.Contains("\"not a /* comment */\"", result);
        Assert.DoesNotContain("remove this", result);
    }

    [Fact]
    public void NormalizeToTokens_ReplacesIdentifiersWithStableToken()
    {
        var normalizer = new CodeNormalizer();

        var left = normalizer.NormalizeToTokens("int sum(int left, int right) { return left + right; }");
        var right = normalizer.NormalizeToTokens("int add(int a, int b) { return a + b; }");

        Assert.Equal(left, right);
    }
}
