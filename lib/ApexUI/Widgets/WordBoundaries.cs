namespace ApexUI.Widgets;

/// Word stops for text editing, shared by the text fields. A word is a run of letters and digits, or a run of
/// other symbols; white space separates them (a line break is white space too).
internal static class WordBoundaries
{
    public static int PreviousStop(string s, int from)
    {
        int i = from;
        while (i > 0 && char.IsWhiteSpace(s[i - 1])) i--;
        if (i == 0) return 0;
        int kind = CharKind(s[i - 1]);
        while (i > 0 && CharKind(s[i - 1]) == kind) i--;
        return i;
    }

    public static int NextStop(string s, int from)
    {
        int i = from;
        if (i < s.Length && !char.IsWhiteSpace(s[i]))
        {
            int kind = CharKind(s[i]);
            while (i < s.Length && CharKind(s[i]) == kind) i++;
        }
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        return i;
    }

    public static int WordStart(string s, int index)
    {
        if (s.Length == 0) return 0;
        int i = Math.Min(index, s.Length - 1);
        int kind = CharKind(s[i]);
        while (i > 0 && CharKind(s[i - 1]) == kind) i--;
        return i;
    }

    public static int WordEnd(string s, int index)
    {
        if (s.Length == 0) return 0;
        int i = Math.Min(index, s.Length - 1);
        int kind = CharKind(s[i]);
        while (i < s.Length && CharKind(s[i]) == kind) i++;
        return i;
    }

    private static int CharKind(char c)
        => char.IsWhiteSpace(c) ? 0 : char.IsLetterOrDigit(c) || c == '_' ? 1 : 2;
}
