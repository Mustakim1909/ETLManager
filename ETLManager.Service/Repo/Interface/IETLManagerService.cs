using ETL.Service.Model;
using ETLManager.Service.Model;
using System.Data;

namespace ETL.Service.Repo.Interface
{
    public interface IETLManagerService
    {
        Task<List<FileNamePrefix>> GetPath(string CompanyName);
        Task<TenantDetails> GetConnectionString(string domain);
        Task SetConnectionString(string connectionstring);
    }
}
 