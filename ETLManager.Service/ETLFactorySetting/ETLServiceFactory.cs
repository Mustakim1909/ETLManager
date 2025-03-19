using Common.Config;
using ETL.Service.Repo.Interface;


namespace ETL.Service.ETLDemoFactorySetting
{
    public class ETLDemoServiceFactory
    {
        public static IETLManagerService GetETLDemoService(DbConfig dbConfig)
        {
            switch (dbConfig.DataProvider.ToLower().Trim())
            {
                case "sqlserver": return new ETL.Service.Repo.MSSQL.ETLManagerService(dbConfig);
                case "oracle": return new ETL.Service.Repo.Oracle.ETLManagerService(dbConfig);
                case "postgresql": return new ETL.Service.Repo.PostgreSql.ETLManagerService(dbConfig);
                default: return new ETL.Service.Repo.MSSQL.ETLManagerService(dbConfig);
            }
        }
    }
}
