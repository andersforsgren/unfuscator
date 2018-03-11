using System;

namespace Unfuscator.Core
{
    public static class VersionUtils
    {
        public static int NumEqual(Version a, Version b)
        {
            return 4 - NumDifferences(a, b);
        }

        public static int NumDifferences(Version a, Version b)
        {
            if (a == null || b == null)
                return 0;

            if (a.Major != b.Major)
                return 4;
            if (a.Minor != b.Minor)
                return 3;
            if (a.MajorRevision != b.MajorRevision)
                return 2;
            if (a.MinorRevision != b.MinorRevision)
                return 1;
            return 0;
        }
    }
}