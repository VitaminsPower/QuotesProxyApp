using System.Text.RegularExpressions;
using JetBrains.Annotations;

[UsedImplicitly]
internal partial class StartUp
{
    [GeneratedRegex(@"\b([A-Za-z]{6})\b")]
    private static partial Regex SixLetterRegex();
}