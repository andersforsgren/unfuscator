using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Dapper;

namespace Unfuscator.Core
{
    internal sealed class MappingDb : IMapping, IDisposable
    {
        private readonly string dbPath;

        public MappingDb()
        {
            dbPath = Path.GetTempFileName();
            CreateDatabase();
        }

        public SQLiteConnection DbConnection()
        {
            return new SQLiteConnection("Data Source=" + dbPath);
        }

        private void CreateDatabase()
        {
            using (var cnn = DbConnection())
            {
                cnn.Open();
                cnn.Execute(
                   @"               
               DROP TABLE IF EXISTS Mapping;
               CREATE TABLE IF NOT EXISTS Mapping (
                 Id         integer PRIMARY KEY,
                 Version    text,
                 Obfuscated text NOT NULL,
                 Unfuscated text NOT NULL                 
              );              
              ");
            }
        }

        public IEnumerable<Record> Get(Signature obfuscated)
        {
            using (var cnn = DbConnection())
            {
                cnn.Open();

                var almost = cnn.Query<Record>(@"SELECT Id, Version, Obfuscated, Unfuscated FROM Mapping WHERE Obfuscated=@Obfuscated ",
                   new { Obfuscated = obfuscated.ToString() }).ToList();

                return almost;
            }
        }

        public void Insert(IEnumerable<Record> records)
        {
            using (var conn = DbConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    foreach (var record in records)
                    {
                        conn.Query<long>(
                           @"INSERT INTO Mapping (version, obfuscated, unfuscated) VALUES  (@Version, @Obfuscated, @Unfuscated)",
                           record, trans);
                    }

                    trans.Commit();
                }
            }
        }

        public IEnumerable<Version> Versions
        {
            get
            {
                using (var cnn = DbConnection())
                {
                    cnn.Open();

                    return cnn.Query<string>(@"SELECT DISTINCT Version FROM Mapping").Select(s => s == null ? null : new Version(s));
                }
            }
        }

        public void Dispose()
        {
            try
            {
                File.Delete(dbPath);
            }
            catch { }
        }
    }
}