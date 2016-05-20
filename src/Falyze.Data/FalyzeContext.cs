using System.Collections.Generic;
using System.Threading.Tasks;

using Falyze.Data.Entities;
using Falyze.Data.TypeInitializers;

namespace Falyze.Data
{
    public interface FalyzeContext
    {
        FalyzeDbInitializer DbInitializer { get; set; }
        int QueryTimeout { get; set; }
        Task<IEnumerable<T>> SelectAsync<T>() where T : Entity, new();
        Task<IEnumerable<T>> SelectAsync<T>(dynamic selectors) where T : Entity, new();
        Task<IEnumerable<T>> SelectAsync<T>(string sql, dynamic selectors = null) where T : Entity, new();
        Task<T> SingleAsync<T>(dynamic selector) where T : Entity, new();
        Task<T> SingleAsync<T>(string sql, dynamic selector) where T : Entity, new();
        Task<int> CreateAsync<T>(T entity) where T : Entity, new();
        int BatchCreate<T>(IEnumerable<T> entities) where T : Entity, new();
        Task<int> UpdateAsync<T>(T entity) where T : Entity, new();
        int BatchUpdate<T>(IEnumerable<T> entities) where T : Entity, new();
        Task<int> DeleteAsync<T>(T entity) where T : Entity, new();
        Task<int> DeleteAsync<T>(string sql, dynamic selectors) where T : Entity, new();
        Task<int> DeleteAllAsync<T>() where T : Entity, new();
        Task<int> ExecuteAsync(string sql, dynamic selectors = null);

    }
}
