﻿using PT.PM.Common;
using PT.PM.Common.Exceptions;
using PT.PM.Common.Nodes;
using PT.PM.Common.Reflection;
using PT.PM.Matching.Patterns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace PT.PM.Matching
{
    public class PatternNormalizer : PatternVisitor<PatternUst>, ILoggable
    {
        private static PropertyCloner<PatternUst> propertyEnumerator = new PropertyCloner<PatternUst>
        {
            IgnoredProperties = new HashSet<string>() { nameof(Ust.Parent), nameof(Ust.Root) }
        };

        public PatternRoot Normalize(PatternRoot pattern)
        {
            var newPattern = new PatternRoot
            {
                Logger = pattern.Logger,
                Key = pattern.Key,
                FilenameWildcard = pattern.FilenameWildcard,
                CodeFile = pattern.CodeFile,
                Languages = new HashSet<Language>(pattern.Languages),
                DataFormat = pattern.DataFormat,
                DebugInfo = pattern.DebugInfo,
                Node = Visit(pattern.Node),
            };
            var ascendantsFiller = new PatternAscendantsFiller(newPattern);
            ascendantsFiller.FillAscendants();
            return newPattern;
        }

        public override PatternUst Visit(PatternArgs patternExpressions)
        {
            // #* #* ... #* -> #*
            List<PatternUst> args = patternExpressions.Args
                .Select(item => (PatternUst)Visit(item)).ToList();
            int index = 0;
            while (index < args.Count)
            {
                if (args[index] is PatternMultipleExpressions &&
                    index + 1 < args.Count &&
                    args[index + 1] is PatternMultipleExpressions)
                {
                    args.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
            var result = new PatternArgs(args);
            return result;
        }

        public override PatternUst Visit(PatternOr patternOr)
        {
            if (patternOr.Patterns.Count == 1)
            {
                return Visit(patternOr.Patterns[0]);
            }

            IEnumerable<PatternUst> exprs = patternOr.Patterns
                .Select(e => Visit(e))
                .OrderBy(e => e);
            return new PatternOr(exprs, patternOr.TextSpan);
        }

        public override PatternUst Visit(PatternIdRegexToken patternIdRegexToken)
        {
            string regexString = patternIdRegexToken.Regex.ToString();

            if (regexString.StartsWith("^") && regexString.EndsWith("$"))
            {
                string newRegexString = regexString.Substring(1, regexString.Length - 2);
                if (newRegexString.All(c => char.IsLetterOrDigit(c) || c == '_'))
                {
                    return new PatternIdToken(
                        newRegexString,
                        patternIdRegexToken.TextSpan);
                }
            }

            return new PatternIdRegexToken(regexString, patternIdRegexToken.TextSpan);
        }

        public override PatternUst Visit(PatternIntRangeLiteral patternIntLiteral)
        {
            if (patternIntLiteral.MinValue == patternIntLiteral.MaxValue)
            {
                return new PatternIntLiteral(
                    patternIntLiteral.MinValue,
                    patternIntLiteral.TextSpan);
            }
            else
            {
                return new PatternIntRangeLiteral(
                    patternIntLiteral.MinValue,
                    patternIntLiteral.MaxValue,
                    patternIntLiteral.TextSpan);
            }
        }

        public override PatternUst Visit(PatternAnd patternAnd)
        {
            if (patternAnd.Patterns.Count == 1)
            {
                return Visit(patternAnd.Patterns[0]);
            }

            IEnumerable<PatternUst> exprs = patternAnd.Patterns
                .Select(e => Visit(e))
                .OrderBy(e => e);
            return new PatternAnd(exprs, patternAnd.TextSpan);
        }

        public override PatternUst Visit(PatternNot patternNot)
        {
            if (patternNot.Pattern is PatternNot innerPatternNot)
            {
                return Visit(innerPatternNot.Pattern);
            }

            return VisitChildren(patternNot);
        }

        protected override PatternUst VisitChildren(PatternUst patternBase)
        {
            try
            {
                return propertyEnumerator.VisitProperties(patternBase, Visit);
            }
            catch (Exception ex) when (!(ex is ThreadAbortException))
            {
                Logger.LogError(new ConversionException(patternBase.Root?.CodeFile, ex)
                {
                    TextSpan = patternBase.TextSpan
                });
                return null;
            }
        }
    }
}
