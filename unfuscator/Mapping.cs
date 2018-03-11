using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;


namespace Unfuscator.Core
{
    public interface IMapping
    {
        /// <summary>
        ///   Gets all matching signatures that may be the 
        /// </summary>
        /// <param name="obfuscated"></param>
        /// <returns></returns>
        IEnumerable<Record> Get(Signature obfuscated);

        /// <summary>
        ///   Inserts records into the db.
        /// </summary>
        /// <param name="records"></param>
        void Insert(IEnumerable<Record> records);

        /// <summary>
        ///   Gets the available versions in the mapping db.
        /// </summary>
        IEnumerable<Version> Versions { get; }
    }

    public static class Mapping
    {
        /// <summary>
        ///   Creates a new, empty mapping.
        /// </summary>
        /// <returns></returns>
        public static IMapping Empty()
        {
            return new MappingDb();
        }
    }

    internal sealed class MappingList : IMapping
    {
        private readonly List<Record> records;

        public MappingList()
        {
            this.records = new List<Record>();
        }

        public IEnumerable<Record> Get(Signature obfuscated)
        {
            return records.Where(r => r.Obfuscated == obfuscated.ToString());
        }

        public void Insert(IEnumerable<Record> rs)
        {
            records.AddRange(rs);
        }

        public IEnumerable<Version> Versions
        {
            get { return records.Select(r => r.Version).Distinct().Select(s => s == null ? null : new Version(s)); }
        }
    }

    public static class MappingExtensions
    {
        // Matches Foo-1.2.3.4.xml
        private const string FileRegex = @"\w+-(?<ver>\d+((\.\d+){0,3})?)?\.xml";

        private static Version GetVersionFromFilename(string path)
        {
            var fn = Path.GetFileName(path);
            var match = Regex.Match(fn, FileRegex);
            if (!match.Success)
                return null;
            return new Version(match.Groups["ver"].Value);
        }

        /// <summary>
        ///   Loads a single Dotfuscator map file into the mapping.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="path">Path to the map.xml file.</param>
        /// <param name="version">Optional version to assign to the loaded obfuscations.</param>
        /// <param name="progress">Progress callback.</param>
        public static void LoadDotfuscator(this IMapping db, string path, Version version = null, Action<float> progress = null)
        {
            db.Insert(DotfuscatorParser.ReadMap(path, version, progress));
        }

        /// <summary>
        ///   Loads dotfuscator xml map files using a naming convention of name-version.xml. 
        ///   The version can be 1..4 digits.
        /// </summary>
        /// <param name="mapping">Mapping to load into.</param>
        /// <param name="paths">Paths of xml files to load.</param>
        /// <param name="errorHandler">Called for each file that fails to load.</param>
        /// <param name="progress">Progress handler</param>
        public static void LoadDotfuscator(this IMapping mapping, IEnumerable<string> paths, Action<string, Exception> errorHandler, Action<string, float> progress)
        {
            int fileNum = 0;
            foreach (var path in paths)
            {
                try
                {
                    var version = GetVersionFromFilename(path);
                    mapping.LoadDotfuscator(path, version, f => progress(path, (fileNum + f) / paths.Count()));
                    fileNum++;
                }
                catch (Exception ex)
                {
                    errorHandler?.Invoke(path, ex);
                }
            }
        }

        /// <summary>
        ///   Loads dotfuscator xml map files using a naming convention of name-version.xml. 
        ///   The version can be 1..4 digits.
        /// </summary>
        /// <param name="mapping">Mapping to load into.</param>
        /// <param name="directory">Path to a directory from which all files matching thee name-1.2.3.4.xml pattern pattern will be loaded.</param>
        /// <param name="errorHandler">Called for each file that fails to load.</param>
        /// <param name="progress">Progress callback.</param>
        public static void LoadDotfuscator(this IMapping mapping, string directory, Action<string, Exception> errorHandler, Action<string, float> progress)
        {
            var paths = Directory.GetFiles(directory.Trim(' ', '"'), "*.xml").Where(f => Regex.IsMatch(f, FileRegex));
            LoadDotfuscator(mapping, paths, errorHandler, progress);
        }
    }
}
