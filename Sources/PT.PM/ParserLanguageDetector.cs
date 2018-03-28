﻿using PT.PM.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace PT.PM
{
    public class ParserLanguageDetector : LanguageDetector
    {
        private readonly static Regex openTagRegex = new Regex("<\\w+>", RegexOptions.Compiled);
        private readonly static Regex closeTagRegex = new Regex("<\\/\\w+>", RegexOptions.Compiled);

        public TimeSpan LanguageParseTimeout = TimeSpan.FromSeconds(20);

        public TimeSpan CheckParseResultTimeSpan = TimeSpan.FromMilliseconds(100);

        public override DetectionResult Detect(string sourceCode, IEnumerable<Language> languages = null)
        {
            List<Language> langs = (languages ?? LanguageUtils.Languages.Values).ToList();
            langs.Remove(Uncertain.Language);
            // Any PHP file contains start tag.
            if (!sourceCode.Contains("<?"))
            {
                langs.Remove(langs.FirstOrDefault(l => l.Key == "Php"));
            }
            // Aspx and html code contains at least one tag.
            if (!openTagRegex.IsMatch(sourceCode) || !closeTagRegex.IsMatch(sourceCode))
            {
                langs.Remove(langs.FirstOrDefault(l => l.Key == "Aspx"));
                langs.Remove(langs.FirstOrDefault(l => l.Key == "Html"));
            }
            var sourceCodeFile = new CodeFile(sourceCode);
            var parseUnits = new Queue<ParserUnit>(langs.Count);

            langs = langs
                .GroupBy(l => l.CreateParser())
                .Select(l => l.First())
                .ToList();

            if (langs.Count == 1)
            {
                return new DetectionResult(langs[0]);
            }

            foreach (Language language in langs)
            {
                Thread thread = new Thread((object obj) =>
                {
                    ((ParserUnit)obj).Parse(sourceCodeFile);
                });
                thread.IsBackground = true;

                ParserUnit parseUnit = new ParserUnit(language, thread);
                thread.Start(parseUnit);

                parseUnits.Enqueue(parseUnit);
            }

            int checkParseResultMs = CheckParseResultTimeSpan.Milliseconds;
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Check every parseUnit completion every checkParseResultMs ms.
            while (parseUnits.Any(parseUnit => parseUnit.IsAlive) &&
                   stopwatch.Elapsed < LanguageParseTimeout)
            {
                ParserUnit parseUnit = parseUnits.Dequeue();
                parseUnits.Enqueue(parseUnit);

                if (parseUnit.IsAlive)
                {
                    Thread.Sleep(checkParseResultMs);
                }
                else
                {
                    if (parseUnit.ParseErrorCount == 0 && parseUnit.Language.Key != "Aspx")
                    {
                        break;
                    }
                }
            }

            int minErrorCount = int.MaxValue;
            ParserUnit resultWithMinErrors = null;

            foreach (ParserUnit parseUnit in parseUnits)
            {
                parseUnit.Abort();

                int errorCount = parseUnit.ParseErrorCount;
                if (errorCount < minErrorCount)
                {
                    minErrorCount = errorCount;
                    resultWithMinErrors = parseUnit;
                }
            }

            if (resultWithMinErrors != null)
            {
                return new DetectionResult(resultWithMinErrors.Language, resultWithMinErrors.ParseTree,
                    resultWithMinErrors.Errors, resultWithMinErrors.Infos, resultWithMinErrors.Debugs);
            }

            return null;
        }
    }
}
