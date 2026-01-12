namespace RAG.Services;


public static class CategoryDetector
{
    public static string DetectRuleBased(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "general";

        text = text.ToLowerInvariant();

        if (text.Contains("cloud") ||
            text.Contains("aws") ||
            text.Contains("azure") ||
            text.Contains("devops"))
            return "cloud";

        if (text.Contains("asp.net") ||
            text.Contains(".net") ||
            text.Contains("c#"))
            return "dotnet";

        if (text.Contains("hr") ||
            text.Contains("salary") ||
            text.Contains("career"))
            return "hr";

        return "general";
    }
}
