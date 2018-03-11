using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Unfuscator.Core
{
    public abstract class ResultWriter
    {
        public static readonly ResultWriter PlainText = new PlainTextResultWriter();
        public static readonly ResultWriter Json = new JsonResultWriter();
        public static readonly ResultWriter Xml = new XmlResultWriter();

        /// <summary>
        ///   Writes the un-obfuscation result to a stream.
        /// </summary>
        /// <param name="res">Result to write.</param>
        /// <param name="target">The known version of the obfuscated stacktrace. (optional).</param>
        /// <param name="stream">Output stream.</param>
        public abstract void Write(StackTraceUnobfuscationResult res, Version target, Stream stream);

        private sealed class PlainTextResultWriter : ResultWriter
        {
            public override void Write(StackTraceUnobfuscationResult res, Version target, Stream stream)
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8, 2048, true))
                {
                    foreach (var frame in res.StackFrames)
                    {
                        var alternatives = frame.Alternatives;
                        var best = alternatives.FirstOrDefault();

                        if (best == null)
                        {
                            writer.WriteLine($"{frame.InputStackLine}?");
                        }
                        else
                        {
                            if (VersionUtils.NumEqual(best.VersionNumber, target) >= 3)
                                writer.WriteLine(best.Unfuscated);
                            else
                                writer.WriteLine(best.Unfuscated + "(v" + alternatives[0].Version + ")");
                        }
                    }
                }
            }
        }

        private sealed class XmlResultWriter : ResultWriter
        {
            public override void Write(StackTraceUnobfuscationResult res, Version target, Stream stream)
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(StackTraceUnobfuscationResult));
                serializer.WriteObject(stream, res);
            }
        }

        private sealed class JsonResultWriter : ResultWriter
        {
            public override void Write(StackTraceUnobfuscationResult res, Version target, Stream stream)
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(StackTraceUnobfuscationResult));
                serializer.WriteObject(stream, res);
            }
        }
    }

    public static class ResultWriterExtensions
    {
        public static void Write(this ResultWriter writer, StackTraceUnobfuscationResult res, Version target, TextWriter textWriter)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                writer.Write(res, target, ms);
                ms.Seek(0, 0);
                using (var streamReader = new StreamReader(ms))
                {
                    textWriter.Write(streamReader.ReadToEnd());
                }
            }
        }
    }
}