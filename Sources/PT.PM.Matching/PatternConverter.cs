﻿using PT.PM.Common;
using PT.PM.Common.Exceptions;
using PT.PM.Matching.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PT.PM.Matching
{
    public class PatternConverter : IPatternConverter<PatternRoot>
    {
        private ILogger logger = DummyLogger.Instance;

        public ILogger Logger
        {
            get => logger;
            set
            {
                logger = value;
                foreach (IPatternSerializer serializer in Serializers)
                {
                    serializer.Logger = logger;
                }
            }
        }

        public List<IPatternSerializer> Serializers { get; set; }

        public PatternConverter()
        {
            Serializers = new List<IPatternSerializer>
            {
                new JsonPatternSerializer()
            };
        }

        public PatternConverter(params IPatternSerializer[] serializers)
        {
            Serializers = serializers.ToList();
        }

        public PatternConverter(IEnumerable<IPatternSerializer> serializers)
        {
            Serializers = serializers.ToList();
        }

        public List<PatternRoot> Convert(IEnumerable<PatternDto> patternsDto)
        {
            var result = new List<PatternRoot>(patternsDto.Count());
            foreach (PatternDto patternDto in patternsDto)
            {
                CodeFile patternFile = new CodeFile(patternDto.Value)
                {
                    Name = patternDto.Name,
                    PatternKey = patternDto.Key
                };

                IPatternSerializer serializer = Serializers
                    .FirstOrDefault(s => s.Format.EqualsIgnoreCase(patternDto.DataFormat))
                    ?? Serializers.First();
                if (serializer == null)
                {
                    Logger.LogError(new ConversionException(
                        patternFile,
                        null, $"Serializer for {patternDto.DataFormat} not found"));
                    continue;
                }

                try
                {
                    PatternRoot pattern = serializer.Deserialize(patternFile);
                    HashSet<Language> languages = patternDto.Languages.ParseLanguages(patternLanguages: true);

                    if (languages.Count == 0)
                    {
                        Logger.LogInfo($"PatternNode \"{patternDto.Key}\" doesn't have proper target languages.");
                    }

                    pattern.DataFormat = serializer.Format;
                    pattern.Key = patternDto.Key;
                    pattern.Languages = languages;
                    pattern.DebugInfo = patternDto.Description;
                    pattern.FilenameWildcard = patternDto.FilenameWildcard;

                    result.Add(pattern);
                }
                catch (Exception ex) when (!(ex is ThreadAbortException))
                {
                    Logger.LogError(new ConversionException(
                        patternFile,
                        ex, $"Error while \"{patternDto.Key}\" pattern deserialising ({patternDto.Value}) "));
                }
            }
            return result;
        }

        public List<PatternDto> ConvertBack(IEnumerable<PatternRoot> patterns)
        {
            var result = new List<PatternDto>();
            foreach (PatternRoot pattern in patterns)
            {
                IPatternSerializer serializer = Serializers.FirstOrDefault(s => s.Format == pattern.DataFormat)
                    ?? Serializers.First();
                try
                {
                    if (serializer is JsonPatternSerializer jsonPatternSerializer)
                    {
                        jsonPatternSerializer.CurrectCodeFile = pattern.CodeFile;
                    }
                    string serialized = serializer.Serialize(pattern);
                    PatternDto patternDto = new PatternDto
                    {
                        DataFormat = pattern.DataFormat,
                        Key = pattern.Key,
                        Languages = new HashSet<string>(pattern.Languages.Select(lang => lang.Key)),
                        Value = serialized,
                        Description = pattern.DebugInfo,
                        FilenameWildcard = pattern.FilenameWildcard
                    };
                    result.Add(patternDto);
                }
                catch (Exception ex) when (!(ex is ThreadAbortException))
                {
                    Logger.LogError(new ConversionException(pattern.CodeFile, ex, $"Error while \"{pattern.Key}\" pattern serialising"));
                }
            }
            return result;
        }
    }
}
