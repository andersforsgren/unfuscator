using System;
using System.Runtime.Serialization;

namespace Unfuscator.Core
{
    [DataContract]
    public class Record
    {
        public Record(string version, string obfuscated, string unfuscated)
           : this(-1, version, obfuscated, unfuscated)
        {
            this.Version = version;
            this.Obfuscated = obfuscated;
            this.Unfuscated = unfuscated;
            this.Id = -1;
        }

        public Record(long id, string version, string obfuscated, string unfuscated)
        {
            this.Version = version;
            this.Obfuscated = obfuscated;
            this.Unfuscated = unfuscated;
            this.Id = id;
        }

        public long Id { get; set; }

        [DataMember]
        public string Version { get; set; }
        [DataMember]
        public string Obfuscated { get; set; }
        [DataMember]
        public string Unfuscated { get; set; }

        public Version VersionNumber => string.IsNullOrEmpty(Version) ? null : new Version(Version);
    }
}