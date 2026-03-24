using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;
namespace Domain.Interfaces
{
    public interface IPatrulleroRepository
    {
        Task SaveAsync(Patrulla patrulla);
        Task<List<Patrulla>> GetUltimasPatrullasAsync();
    }
}
