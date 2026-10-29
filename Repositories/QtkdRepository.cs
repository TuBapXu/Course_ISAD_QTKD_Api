using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.Entities;

namespace QTKD.Api.Repositories
{
    public class QtkdRepository : IQtkdRepository
    {
        private readonly QtkdDbContext _context;

        public QtkdRepository(QtkdDbContext context)
        {
            _context = context;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _context.Customers.ToListAsync();
        }

        public async Task<Contract?> GetContractByIdAsync(int id){
            return await _context.Contracts.Include(c => c.Customer).FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}