using Common.Config;
using Common.DataAccess.Oracle;
using Common.Security;
using ETL.Service.Model;
using ETL.Service.Repo.Interface;
using ETLManager.Service.Model;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETL.Service.Repo.Oracle
{
    public class ETLManagerService : IETLManagerService
    {
        public QueryHelper _queryHelper = null;
        public string _connectionString = null;
        public ETLManagerService(DbConfig dbConfig)
        {

            var encPassword = string.Empty;
            encPassword = dbConfig.ConnectionString.Split(';')[3].Substring(10);
            var password = SecurityHelper.DecryptWithEmbedKey(encPassword);
            dbConfig.ConnectionString = dbConfig.ConnectionString.Replace(encPassword, password);
            _connectionString = dbConfig.ConnectionString;
            _queryHelper = new QueryHelper(dbConfig.ConnectionString);
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
}
