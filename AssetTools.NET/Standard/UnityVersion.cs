using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace AssetsTools.NET
{
    [StructLayout(LayoutKind.Explicit)]
    public struct UnityVersion : IEquatable<UnityVersion>, IComparable<UnityVersion>
    {
        private static readonly Regex versionRegex = new Regex(@"(?<major>\d+)\.(?<minor>\d+)\.(?<build>\d+)((?<type>[A-Za-z])(?<typeNumber>\d))?", RegexOptions.Compiled);
        public enum VersionType
        {
            Unknown = -1,
            Alpha = 0,
            Beta,
            Final,
            Patch,

            MaxValue = Patch,
        }

        [FieldOffset(0)]
        private readonly ulong m_data;

        [FieldOffset(6)]
        private readonly ushort major;
        [FieldOffset(4)]
        private readonly ushort minor;
        [FieldOffset(2)]
        private readonly ushort build;
        [FieldOffset(1)]
        private readonly byte type;
        [FieldOffset(0)]
        private readonly byte typeNumber;

        public int Major => major;
        public int Minor => minor;
        public int Build => build;
        public VersionType Type => (VersionType)type;
        public int TypeNumber => typeNumber;

        public UnityVersion(string version) : this()
        {
            var result = versionRegex.Match(version);
            major = ushort.Parse(result.Groups["major"].Value);
            minor = ushort.Parse(result.Groups["minor"].Value);
            build = ushort.Parse(result.Groups["build"].Value);
            type = (byte)(result.Groups["type"].Success ? TypeFromLiteral(result.Groups["type"].Value) : VersionType.Unknown);
            typeNumber = (byte)(result.Groups["typeNumber"].Success ? byte.Parse(result.Groups["typeNumber"].Value) : 0);
        }

        public UnityVersion(int major, int minor, int build) : this(major, minor, build, VersionType.Unknown, 0) { }

        public UnityVersion(int major, int minor, int build, VersionType type, int typeNumber) : this()
        {
            this.major = (ushort)major;
            this.minor = (ushort)minor;
            this.build = (ushort)build;
            this.type = (byte)type;
            this.typeNumber = (byte)typeNumber;
        }

        public override bool Equals(object obj)
        {
            return obj is UnityVersion version && Equals(version);
        }

        public bool Equals(UnityVersion other)
        {
            return m_data == other.m_data;
        }

        public override int GetHashCode()
        {
            return 1064174093 + m_data.GetHashCode();
        }

        public static bool operator ==(UnityVersion left, UnityVersion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnityVersion left, UnityVersion right)
        {
            return !(left == right);
        }

        public static bool operator >(UnityVersion left, UnityVersion right)
        {
            return left.CompareTo(right) == 1;
        }

        public static bool operator <(UnityVersion left, UnityVersion right)
        {
            return left.CompareTo(right) == -1;
        }

        public static bool operator >=(UnityVersion left, UnityVersion right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <=(UnityVersion left, UnityVersion right)
        {
            return left.CompareTo(right) <= 0;
        }

        public override string ToString()
        {
            return Type == VersionType.Unknown ? $"{major}.{minor}.{build}" : $"{major}.{minor}.{build}{TypeToLiteral(Type)}{typeNumber}";
        }

        private static string TypeToLiteral(VersionType type)
        {
            switch (type)
            {
                case VersionType.Alpha:
                    return "a";
                case VersionType.Beta:
                    return "b";
                case VersionType.Final:
                    return "f";
                case VersionType.Patch:
                    return "p";
                default:
                    throw new Exception($"Unsupported vertion type {type}");
            }
        }

        private static VersionType TypeFromLiteral(string literal)
        {
            switch (literal)
            {
                case "a":
                    return VersionType.Alpha;
                case "b":
                    return VersionType.Beta;
                case "f":
                    return VersionType.Final;
                case "p":
                    return VersionType.Patch;
                default:
                    throw new Exception($"Unsupported vertion type {literal}");
            }
        }

        public int CompareTo(UnityVersion other)
        {
            return this.m_data == other.m_data ? 0 : this.m_data > other.m_data ? 1 : -1;
        }
    }
}
