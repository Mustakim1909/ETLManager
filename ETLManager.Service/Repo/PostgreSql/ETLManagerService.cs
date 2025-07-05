using Common.Config;
using Common.DataAccess.PostgreSql;
using Common.Security;
using ETL.Service.Model;
using ETL.Service.Repo.Interface;
using ETLManager.Service.Model;
using Microsoft.Extensions.Options;
using NpgsqlTypes;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETL.Service.Repo.PostgreSql
{
    public class ETLManagerService : IETLManagerService
    {
        private QueryHelper _queryHelper = null;
        private string _connectionString = null;
        public ETLManagerService(DbConfig dbConfig)
        {

            //_queryHelper = new QueryHelper(databaseConfig.ConnectionString);
            var initialConnectionString = ConnectionStringManager.IsConnectionStringCached()
                                    ? ConnectionStringManager.GetConnectionString()
                                    : dbConfig.ConnectionString;
            _connectionString = initialConnectionString;
            _queryHelper = new QueryHelper(initialConnectionString);
        }
        public async Task<List<FileNamePrefix>> GetPath(string CompanyName)
        {
            var sql = $"SELECT * FROM FileNamePrefix Where SourceName=@SourceName";
            try
            {
                var parameters = new List<IDataParameter>
                {
                     QueryHelper.CreateSqlParameter("@SourceName", CompanyName, NpgsqlDbType.Text)
                };
                var data = (await _queryHelper.Read(sql, parameters, FilePathList))?.ToList();

                return data;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<TenantDetails> GetConnectionString(string Domain)
        {
            string sql = $"SELECT * FROM TenantDetails Where Domain = @Domain";
            var parameters = new List<IDataParameter>
        {
            QueryHelper.CreateSqlParameter("@Domain", Domain, NpgsqlDbType.Varchar)
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


        public async Task SetConnectionString(string connectionstring)
        {
            var encPassword = connectionstring.Split(';')[3].Substring(9);
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

    public static class ConnectionStringManager
    {
        private static string _cachedConnectionString;

        public static string GetConnectionString()
        {
            return _cachedConnectionString;
        }
        public static void SetConnectionString(string connectionString)
        {
            var encPassword = connectionString.Split(';')[3].Substring(9);
            var password = SecurityHelper.DecryptWithEmbedKey(encPassword);
            connectionString = connectionString.Replace(encPassword, password);
            _cachedConnectionString = connectionString;
        }
        public static bool IsConnectionStringCached()
        {
            return !string.IsNullOrEmpty(_cachedConnectionString);
        }
    }
}
