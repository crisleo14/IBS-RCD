namespace Accounting_System.Utility
{
    public class StringHelper
    {
        public static string NormalizeString(string? value)
        {
            return value?.Trim().ReplaceLineEndings(" ") ?? string.Empty;
        }
    }
}
