﻿using PT.PM.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PT.PM
{
    public class ParserUnit
    {
        private Thread thread;

        private ParserUnitLogger logger;

        private ILanguageParser parser;

        public Language Language { get; }

        public ParseTree ParseTree { get; private set; }

        public int ParseErrorCount => parser.Logger.ErrorCount;

        public List<Exception> Errors => logger.Errors;

        public List<object> Infos => logger.Infos;

        public List<string> Debugs => logger.Debugs;

        public bool IsAlive => thread.IsAlive;

        public void Abort() => thread.Abort();

        public ParserUnit(Language language, Thread thread)
        {
            Language = language ?? throw new NullReferenceException(nameof(language));
            parser = language.CreateParser();
            logger = new ParserUnitLogger();
            parser.Logger = logger;
            this.thread = thread ?? throw new NullReferenceException(nameof(thread));
        }

        public void Parse(CodeFile codeFile)
        {
            ParseTree = parser.Parse(codeFile);
        }

        public override string ToString()
        {
            return $"{Language}; Errors: {ParseErrorCount}; IsAlive: {thread.IsAlive}";
        }
    }
}
