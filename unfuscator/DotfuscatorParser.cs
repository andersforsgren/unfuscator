using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Unfuscator.Core
{
    /// <summary>
    ///   Parses Dotfuscator Map.xml files.
    /// </summary>
    internal static class DotfuscatorParser
    {
        internal static IEnumerable<Record> ReadMap(string path, Version version = null, Action<float> progress = null)
        {
            path = path.Trim('"', ' ');
            XDocument xdoc = XDocument.Load(path);
            var types = xdoc.Descendants("type").ToList();
            int numTypes = types.Count;

            int counter = 0;
            progress(0);
            foreach (var type in types)
            {
                counter++;

                if (counter % 100 == 0 && progress != null)
                {
                    progress((float)counter / numTypes);
                }

                string typeName = type.Element("name").Value;

                if (typeName.Contains("!"))
                    continue;

                foreach (var method in type.Descendants("method"))
                {
                    string obfuscatedSig;
                    string unfuscatedSig;

                    try
                    {
                        var methodNameElement = method.Element("name");
                        var obfuscatedNameElement = method.Element("newname");
                        var signatureElement = method.Element("signature");

                        if (methodNameElement == null || signatureElement == null)
                            continue;

                        if (methodNameElement.Value.StartsWith("<") || typeName.Contains("/") ||
                            methodNameElement.Value.Contains("/"))
                            continue;

                        string signature = signatureElement.Value;
                        if (signature.Contains("!"))
                            continue;

                        string obfuscatedName = obfuscatedNameElement?.Value ?? methodNameElement.Value;

                        Signature sig = Signature.ParseDotfuscator(signature, string.Format("{0}.{1}", typeName, obfuscatedName));
                        obfuscatedSig = sig.ToString();
                        unfuscatedSig = sig.WithMethodName(string.Format("{0}.{1}", typeName, methodNameElement.Value)).ToString();
                    }
                    catch (NotSupportedException)
                    {
                        continue;
                    }

                    yield return new Record(version?.ToString() ?? null, obfuscatedSig, unfuscatedSig);
                }
            }

            progress(1.0f);
        }
    }
}