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

namespace ETL.Service.Repo.MSSQL
{ 
    public class ETLManagerService : IETLManagerService
    {

        public QueryHelper _queryHelper = null;
        public string conectionstring = null;
        public ETLManagerService(DbConfig dbConfig)
        {

            //_queryHelper = new QueryHelper(databaseConfig.ConnectionString);
            var initialConnectionString = ConnectionStringManager.IsConnectionStringCached()
                                    ? ConnectionStringManager.GetConnectionString()
                                    : dbConfig.ConnectionString;
            conectionstring = initialConnectionString;
            _queryHelper = new QueryHelper(initialConnectionString);
        }
        public Task<TenantDetails> GetConnectionString(string domain)
        {
            throw new NotImplementedException();
        }

        public Task<List<FileNamePrefix>> GetPath()
        {
            throw new NotImplementedException();
        }

        public Task SetConnectionString(string connectionstring)
        {
            throw new NotImplementedException();
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
}


