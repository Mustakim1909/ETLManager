using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using System.Formats.Asn1;
using Common.DataAccess.MsSql;
using Common.Config;
using System.Data;
using Serilog;
using Common.Security;
using Microsoft.Extensions.Options;
using ETL.Service.Repo.Interface;
using ETLManager.Service.Model;
using ETL.Service.Model;
using NpgsqlTypes;

namespace ETL.Service.Repo.MSSQL
{ 
    public class ETLManagerService : IETLManagerService
    {

        public QueryHelper _queryHelper = null;
        public string conectionstring = null;
        private readonly Appsettings _appsettings;
        public ETLManagerService(DbConfig dbConfig)
        {

            //_queryHelper = new QueryHelper(databaseConfig.ConnectionString);
            var initialConnectionString = ConnectionStringManager.IsConnectionStringCached()
                                    ? ConnectionStringManager.GetConnectionString()
                                    : dbConfig.ConnectionString;
            conectionstring = initialConnectionString;
            _queryHelper = new QueryHelper(initialConnectionString);
        }
        public async Task<TenantDetails> GetConnectionString(string Domain)
        {
            string sql = $"SELECT * FROM TenantDetails Where Domain = @Domain";
            var parameters = new List<IDataParameter>
        {
            QueryHelper.CreateSqlParameter("@Domain", Domain, SqlDbType.NVarChar)
        };

            var value = (await _queryHelper.Read(sql, parameters, ConnectionStringMake)).FirstOrDefault();
            if (value != null)
            {
                // Cache the connection string
                ConnectionStringManager.SetConnectionString(value.ConnectionString);

                var initialConnectionString = ConnectionStringManager.IsConnectionStringCached()
                                    ? ConnectionStringManager.GetConnectionString()
                                    : null;
                _queryHelper = new QueryHelper(initialConnectionString);
                // return connection string
                return value;
            }
            else
            {
                // return connection string
                return value;
            }
        }

        public async Task<List<FileNamePrefix>> GetPath(string CompanyName)
        {
            var sql = $"SELECT * FROM FileNamePrefix WHERE SourceName =@SourceName";
            try
            {
                var parameters = new List<IDataParameter>
                {
                     QueryHelper.CreateSqlParameter("@SourceName", CompanyName, SqlDbType.NVarChar)
                };
                var data = (await _queryHelper.Read(sql, parameters, FilePathList))?.ToList();

                return data;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task SetConnectionString(string connectionstring)
        {
            var encPassword = connectionstring.Split(';')[3].Substring(10);
            var password = SecurityHelper.DecryptWithEmbedKey(encPassword);
            connectionstring = connectionstring.Replace(encPassword, password);

            //// Cache the connection string
            //ConnectionStringManager.SetConnectionString(connectionString);

            // Update QueryHelper with the new connection string
            _queryHelper = new QueryHelper(connectionstring);
        }
        private readonly Func<IDataReader, TenantDetails> ConnectionStringMake = reader =>
               new TenantDetails
               {
                   ConnectionString = reader["ConnectionString"].ToString(),
               };
        private readonly Func<IDataReader, FileNamePrefix> FilePathList = reader =>
               new FileNamePrefix
               {
                   SourceName = reader["SourceName"].ToString(),
                   FinPutSource = reader["FinPutSource"].ToString(),
                   FinPutProcessed = reader["FinPutProcessed"].ToString(),
               };
    }
}
    public static class ConnectionStringManager
    {
        private static string _cachedConnectionString;

        public static string GetConnectionString()
        {
            return _cachedConnectionString;
        }
        public static void SetConnectionString(string connectionString)
        {
            var encPassword = connectionString.Split(';')[3].Substring(10);
            var password = SecurityHelper.DecryptWithEmbedKey(encPassword);
            connectionString = connectionString.Replace(encPassword, password);
            _cachedConnectionString = connectionString;
        }
        public static bool IsConnectionStringCached()
        {
            return !string.IsNullOrEmpty(_cachedConnectionString);
        }
    }



