﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PT.PM.Common.Utils
{
    public static class PathUtils
    {
        private const string LongPrefix = @"\\?\";

        public const int MaxDirLength = 248 - 1;
        public const int MaxPathLength = 260 - 1;

        public static string NormalizeFilePath(this string path) => NormalizePath(path, false);

        public static string NormalizeDirPath(this string path, bool force = false) => NormalizePath(path, true, force);

        private static string NormalizePath(this string path, bool isDirectory = true, bool force = false)
        {
            if ((path.Length > (isDirectory ? MaxDirLength : MaxPathLength) || force) &&
                CommonUtils.IsWindows && !CommonUtils.IsCoreApp && !path.StartsWith(LongPrefix))
            {
                if (path.StartsWith(@"\\"))
                {
                    return $@"{LongPrefix}UNC\{path.Substring(2)}";
                }

                path = path.NormalizeDirSeparator();

                return $"{LongPrefix}{path}";
            }

            return path.NormalizeDirSeparator();
        }

        public static string NormalizeDirSeparator(this string path)
        {
            string notPlatformSeparator = CommonUtils.IsWindows ? "/" : "\\";

            if (path.Contains(notPlatformSeparator))
            {
                return path.Replace(notPlatformSeparator, Path.DirectorySeparatorChar.ToString());
            }

            return path;
        }

        public static string DenormalizePath(this string path)
        {
            if (path.StartsWith(LongPrefix))
            {
                return path.Substring(LongPrefix.Length);
            }

            return path;
        }
    }

    public static class FileExt
    {
        public static string ReadAllText(string path) => File.ReadAllText(path.NormalizeFilePath());

        public static void WriteAllText(string path, string contents) => File.WriteAllText(path.NormalizeFilePath(), contents);

        public static void WriteAllLines(string path, IEnumerable<string> contents) => File.WriteAllLines(path.NormalizeFilePath(), contents);

        public static bool Exists(string path) => File.Exists(path.NormalizeFilePath());

        public static void Delete(string path) => File.Delete(path.NormalizeFilePath());

        public static void Move(string sourceFileName, string destFileName) => File.Move(sourceFileName.NormalizeFilePath(), destFileName.NormalizeFilePath());
    }

    public static class DirectoryExt
    {
        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            IEnumerable<string> files = Directory.EnumerateFiles(path.NormalizeDirPath(true), searchPattern, searchOption);
            return files.Select(file => file.DenormalizePath());
        }

        public static IEnumerable<string> EnumerateFileSystemEntries(string path)
        {
            IEnumerable<string> fileSystemEntries = Directory.EnumerateFileSystemEntries(path.NormalizeDirPath(true));
            return fileSystemEntries.Select(fileSystemEntry => fileSystemEntry.DenormalizePath());
        }

        public static string[] GetDirectories(string path)
        {
            string[] dirs = Directory.GetDirectories(path.NormalizeDirPath(true));

            for (int i = 0; i < dirs.Length; i++)
            {
                dirs[i] = dirs[i].DenormalizePath();
            }

            return dirs;
        }

        public static string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            string[] files = Directory.GetFiles(path.NormalizeDirPath(true), searchPattern, searchOption);

            for (int i = 0; i < files.Length; i++)
            {
                files[i] = files[i].DenormalizePath();
            }

            return files;
        }

        public static DirectoryInfo CreateDirectory(string path) => Directory.CreateDirectory(path.NormalizeDirPath());

        public static bool Exists(string path) => Directory.Exists(path.NormalizeDirPath());

        public static void Delete(string path) => Directory.Delete(path.NormalizeDirPath(true), true);

        public static void Move(string sourceDirName, string destDirName) => Directory.Move(sourceDirName.NormalizeDirPath(true), destDirName.NormalizeDirPath(true));
    }
}
