﻿using Newtonsoft.Json;
using PT.PM.Common;
using PT.PM.Common.Nodes;
using PT.PM.Common.Nodes.Tokens.Literals;
using PT.PM.Matching.Patterns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PT.PM.Matching
{
    public class PatternRoot : ILoggable
    {
        private HashSet<Language> languages = new HashSet<Language>();
        private Regex pathWildcardRegex;

        public static readonly string[] KeySeparators = new[] { ",", ";" };

        public ILogger Logger { get; set; } = DummyLogger.Instance;

        public string Key { get; set; } = "";

        public string FilenameWildcard { get; set; } = "";

        public Regex FilenameWildcardRegex
        {
            get
            {
                if (!string.IsNullOrEmpty(FilenameWildcard) && pathWildcardRegex == null)
                {
                    pathWildcardRegex = new WildcardConverter().Convert(FilenameWildcard);
                }
                return pathWildcardRegex;
            }
        }

        public HashSet<Language> Languages
        {
            get => languages;
            set
            {
                Language notPatternLang = value.FirstOrDefault(lang => !lang.IsPattern);
                if (notPatternLang != null)
                {
                    throw new ArgumentException($"Unable to create pattern for {notPatternLang.Title}");
                }
                languages = value;
            }
        }

        public string DataFormat { get; set; } = "";

        [JsonIgnore]
        public string DebugInfo { get; set; } = "";

        public PatternUst Node { get; set; } = new PatternAny();

        public CodeFile CodeFile { get; set; } = CodeFile.Empty;

        public PatternRoot()
        {
        }

        public PatternRoot(PatternUst node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }

        public override string ToString()
        {
            return (!string.IsNullOrEmpty(DebugInfo) ? DebugInfo : Key) ?? "";
        }

        public List<MatchResult> Match(Ust ust)
        {
            var context = new MatchContext(this) { Logger = Logger };
            var results = new List<MatchResult>();

            if (ust is RootUst rootUst)
            {
                if (Node is PatternCommentRegex ||
                   (Node is PatternOr patternOr && patternOr.Patterns.Any(v => v is PatternCommentRegex)))
                {
                    foreach (CommentLiteral commentNode in rootUst.Comments)
                    {
                        MatchAndAddResult(Node, commentNode, context, results);
                    }
                }
                else
                {
                    TraverseChildren(Node, rootUst, context, results);
                }
            }

            return results;
        }

        private static void TraverseChildren(PatternUst patternUst, Ust ust, MatchContext context, List<MatchResult> results)
        {
            MatchAndAddResult(patternUst, ust, context, results);

            if (ust != null && !(patternUst is PatternAny))
            {
                foreach (Ust child in ust.Children)
                {
                    TraverseChildren(patternUst, child, context, results);
                }
            }
        }

        private static void MatchAndAddResult(
            PatternUst patternUst, Ust ust, MatchContext context, List<MatchResult> results)
        {
            if (patternUst.MatchUst(ust, context).Success)
            {
                if (context.Locations.Count == 0)
                {
                    context.Locations.Add(ust.TextSpan);
                }

                var match = new MatchResult(
                    ust.RootOrThis, context.PatternUst, context.Locations);

                results.Add(match);
                context.Logger.LogInfo(match);
            }

            context.Locations.Clear();
        }
    }
}
