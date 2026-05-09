namespace LabServer.Server.Service;

public class TokenSimilarity
{
    public System.Double Compare(IReadOnlyList<System.String> left, IReadOnlyList<System.String> right, System.Int32 shingleSize = 5)
    {
        if (left.Count == 0 || right.Count == 0)
            return 0;

        var leftShingles = BuildShingles(left, shingleSize);
        var rightShingles = BuildShingles(right, shingleSize);

        if (leftShingles.Count == 0 || rightShingles.Count == 0)
            return 0;

        var intersection = leftShingles.Intersect(rightShingles).Count();
        var union = leftShingles.Union(rightShingles).Count();

        return union == 0 ? 0 : (System.Double)intersection / union;
    }

    private static HashSet<System.String> BuildShingles(IReadOnlyList<System.String> tokens, System.Int32 shingleSize)
    {
        var actualSize = Math.Min(shingleSize, tokens.Count);
        var result = new HashSet<System.String>(StringComparer.Ordinal);

        for (var i = 0; i <= tokens.Count - actualSize; i++)
        {
            result.Add(System.String.Join(' ', tokens.Skip(i).Take(actualSize)));
        }

        return result;
    }
}
