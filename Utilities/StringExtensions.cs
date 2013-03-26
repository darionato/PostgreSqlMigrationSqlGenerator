using System;
using System.Data.Entity.Migrations;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;

namespace Badlydone.Utilities
{
    

    internal static class StringExtensions
    {
        private static readonly Regex MigrationIdPattern = new Regex(@"\d{15}_.+");

        public static DatabaseName ToDatabaseName(this string s)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(s));

            return DatabaseName.Parse(s);
        }

        public static bool EqualsIgnoreCase(this string s1, string s2)
        {
            return string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
        }

        public static string MigrationName(this string migrationId)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(migrationId));
            Contract.Assert(migrationId.IsValidMigrationId());

            return migrationId.Substring(16);
        }

        public static string RestrictTo(this string s, int size)
        {
            if (string.IsNullOrEmpty(s)
                || s.Length <= size)
            {
                return s;
            }

            return s.Substring(0, size);
        }

        public static bool IsValidMigrationId(this string migrationId)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(migrationId));

            return MigrationIdPattern.IsMatch(migrationId)
                   || migrationId == DbMigrator.InitialDatabase;
        }

    }
}
