using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FormatAllFilesPlus.Text
{
    public class WildCard
    {
        public const char AnyCharacterPattern = '?';

        public const char AnyCharactersPattern = '*';

        public const char Delimiter = ';';

        private readonly Regex[] _regexes;

        public WildCard(string pattern, WildCardOptions options = WildCardOptions.SinglePattern)
        {
            _regexes = ConvertRegexPatterns(pattern, options).Select(x => new Regex(x)).ToArray();
        }

        public bool IsMatch(string input)
        {
            return _regexes.Any(x => x.IsMatch(input));
        }

        public static bool IsMatch(string input, string pattern, WildCardOptions options = WildCardOptions.SinglePattern)
        {
            return ConvertRegexPatterns(pattern, options).Any(x => Regex.IsMatch(input, x));
        }

        private static string ConvertRegexPattern(string wildCardPattern)
        {
            var regexPattern = Regex.Escape(wildCardPattern)
                .Replace(@"\" + AnyCharacterPattern, ".")
                .Replace(@"\" + AnyCharactersPattern, ".*");

            return $"^{regexPattern}$";
        }

        private static IEnumerable<string> ConvertRegexPatterns(string wildCardPattern, WildCardOptions options)
        {
            if (string.IsNullOrEmpty(wildCardPattern))
            {
                return Enumerable.Empty<string>();
            }
            else if (options.HasFlag(WildCardOptions.MultiPattern))
            {
                return wildCardPattern.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(ConvertRegexPattern);
            }
            else
            {
                return new[] { ConvertRegexPattern(wildCardPattern) };
            }
        }
    }
}
