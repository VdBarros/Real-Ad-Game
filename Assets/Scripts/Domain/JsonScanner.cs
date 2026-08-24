using System;
using System.Globalization;
using System.Text;

namespace Game.Domain
{
    sealed class JsonScanner
    {
        readonly string source;
        int index;

        public JsonScanner(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            this.source = source;
        }

        public void Expect(char symbol)
        {
            SkipWhitespace();
            if (index >= source.Length || source[index] != symbol)
            {
                throw Unexpected("'" + symbol + "'");
            }

            index++;
        }

        public bool TryExpect(char symbol)
        {
            SkipWhitespace();
            if (index < source.Length && source[index] == symbol)
            {
                index++;
                return true;
            }

            return false;
        }

        public void ExpectMember(string name)
        {
            var actual = ReadText();
            if (!string.Equals(actual, name, StringComparison.Ordinal))
            {
                throw new FormatException(
                    "Expected the member \"" + name + "\" but found \"" + actual + "\".");
            }

            Expect(':');
        }

        public void ExpectEnd()
        {
            SkipWhitespace();
            if (index != source.Length)
            {
                throw Unexpected("the end of the document");
            }
        }

        public string ReadText()
        {
            Expect('"');
            var text = new StringBuilder();
            while (true)
            {
                if (index >= source.Length)
                {
                    throw Unexpected("a closing quote");
                }

                var symbol = source[index++];
                if (symbol == '"')
                {
                    return text.ToString();
                }

                if (symbol != '\\')
                {
                    text.Append(symbol);
                    continue;
                }

                if (index >= source.Length)
                {
                    throw Unexpected("an escape sequence");
                }

                switch (source[index++])
                {
                    case '"':
                        text.Append('"');
                        break;
                    case '\\':
                        text.Append('\\');
                        break;
                    case '/':
                        text.Append('/');
                        break;
                    case 'b':
                        text.Append('\b');
                        break;
                    case 'f':
                        text.Append('\f');
                        break;
                    case 'n':
                        text.Append('\n');
                        break;
                    case 'r':
                        text.Append('\r');
                        break;
                    case 't':
                        text.Append('\t');
                        break;
                    case 'u':
                        if (index + 4 > source.Length)
                        {
                            throw Unexpected("four hexadecimal digits");
                        }

                        text.Append((char)int.Parse(
                            source.Substring(index, 4),
                            NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture));
                        index += 4;
                        break;
                    default:
                        throw Unexpected("a valid escape sequence");
                }
            }
        }

        public long ReadLong()
        {
            SkipWhitespace();
            var start = index;
            if (index < source.Length && source[index] == '-')
            {
                index++;
            }

            var firstDigit = index;
            while (index < source.Length && source[index] >= '0' && source[index] <= '9')
            {
                index++;
            }

            if (index == firstDigit)
            {
                throw Unexpected("a whole number");
            }

            return long.Parse(
                source.Substring(start, index - start),
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture);
        }

        public int ReadInt()
        {
            var value = ReadLong();
            if (value < int.MinValue || value > int.MaxValue)
            {
                throw new FormatException(
                    "The number " + value.ToString(CultureInfo.InvariantCulture) + " does not fit an int.");
            }

            return (int)value;
        }

        void SkipWhitespace()
        {
            while (index < source.Length
                && (source[index] == ' '
                    || source[index] == '\t'
                    || source[index] == '\n'
                    || source[index] == '\r'))
            {
                index++;
            }
        }

        FormatException Unexpected(string expectation)
        {
            var found = index < source.Length
                ? "'" + source[index] + "'"
                : "the end of the document";

            return new FormatException(
                "Expected " + expectation + " at offset "
                + index.ToString(CultureInfo.InvariantCulture) + " but found " + found + ".");
        }
    }
}
