using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Unfuscator.Core
{
    public class UnObfuscator
    {
        private readonly IMapping mappingDb;

        public UnObfuscator(IMapping mappingDb)
        {
            this.mappingDb = mappingDb;
        }

        public StackTraceUnobfuscationResult Unfuscate(string stacktrace, Version target = null)
        {
            var lines = stacktrace.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l));
            List<StackFrameUnobfuscationResult> frames = new List<StackFrameUnobfuscationResult>();
            foreach (var line in lines)
            {
                var sig = Signature.ParseStackTraceLine(line);
                frames.Add(new StackFrameUnobfuscationResult(sig, mappingDb.Get(sig).OrderByDescending(r => VersionUtils.NumEqual(r.VersionNumber, target))));
            }
            return new StackTraceUnobfuscationResult(frames);
        }
    }

    [DataContract]
    public class StackFrameUnobfuscationResult
    {
        public StackFrameUnobfuscationResult(Signature inputStackLine, IEnumerable<Record> alternatives)
        {
            InputStackLine = inputStackLine;
            Alternatives = alternatives.ToList();
        }

        [DataMember]
        public Signature InputStackLine { get; set; }

        [DataMember]
        public List<Record> Alternatives { get; set; }
    }


    [DataContract]
    public sealed class StackTraceUnobfuscationResult
    {
        public StackTraceUnobfuscationResult(IEnumerable<StackFrameUnobfuscationResult> frames)
        {
            StackFrames = frames;
        }

        [DataMember]
        public IEnumerable<StackFrameUnobfuscationResult> StackFrames { get; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            ResultWriter.PlainText.Write(this, null, sw);
            return sb.ToString();
        }
    }
}