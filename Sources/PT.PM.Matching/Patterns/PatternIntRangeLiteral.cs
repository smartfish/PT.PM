﻿using PT.PM.Common;
using PT.PM.Common.Nodes.Tokens.Literals;
using System;

namespace PT.PM.Matching.Patterns
{
    public class PatternIntRangeLiteral : PatternUst<IntLiteral>
    {
        public long MinValue { get; set; } = long.MinValue;

        public long MaxValue { get; set; } = long.MaxValue;

        public PatternIntRangeLiteral()
            : this(long.MinValue, long.MaxValue)
        {
        }

        public PatternIntRangeLiteral(long value, TextSpan textSpan = default)
            : this(value, value, textSpan)
        {
        }

        public PatternIntRangeLiteral(long minValue, long maxValue, TextSpan textSpan = default)
            : base(textSpan)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public override bool Any => MinValue == long.MinValue && MaxValue == long.MaxValue;

        public void ParseAndPopulate(string range)
        {
            if (string.IsNullOrEmpty(range))
            {
                MinValue = long.MinValue;
                MaxValue = long.MaxValue;
                return;
            }

            if (range.StartsWith("<(") && range.EndsWith(")>"))
            {
                range = range.Substring(2, range.Length - 4);
            }

            int index = range.IndexOf("..");

            if (index != -1)
            {
                string value = range.Remove(index);
                long minValue;

                if (value == "")
                {
                    minValue = long.MinValue;
                }
                else if (!long.TryParse(value, out minValue))
                {
                    throw new FormatException($"Invalid or too big value {value} while {nameof(PatternIntRangeLiteral)} parsing.");
                }
                MinValue = minValue;

                value = range.Substring(index + 2);
                long maxValue;

                if (value == "")
                {
                    maxValue = long.MaxValue;
                }
                else if (!long.TryParse(value, out maxValue))
                {
                    throw new FormatException($"Invalid or too big value {value} while {nameof(PatternIntRangeLiteral)} parsing.");
                }
                MaxValue = maxValue;
            }
            else
            {
                long value;
                if (!long.TryParse(range, out value))
                {
                    throw new FormatException($"Invalid or too big value {range} while {nameof(PatternIntRangeLiteral)} parsing.");
                }

                MinValue = value;
                MaxValue = value;
            }
        }

        public override string ToString()
        {
            string result;

            if (MinValue == MaxValue)
            {
                result = MinValue.ToString();
            }
            else
            {
                if (MinValue == long.MinValue)
                {
                    result = MaxValue == long.MaxValue ? "" : $"..{MaxValue}";
                }
                else
                {
                    result = MaxValue == long.MaxValue ? $"{MinValue}.." : $"{MinValue}..{MaxValue}";
                }
            }

            return $"<({result})>";
        }

        public override MatchContext Match(IntLiteral intLiteral, MatchContext context)
        {
            return intLiteral.Value >= MinValue && intLiteral.Value < MaxValue
                ? context.AddMatch(intLiteral)
                : context.Fail();
        }
    }
}
