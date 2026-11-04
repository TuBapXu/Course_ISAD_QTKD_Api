using System.Collections.Generic;
using System.Threading.Tasks;
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

        // Customer
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _context.Customers.ToListAsync();
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            return await _context.Customers.FindAsync(id);
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }

        // Contract
        public async Task<Contract?> GetContractByIdAsync(int id)
        {
            return await _context.Contracts
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddContractAsync(Contract contract)
        {
            await _context.Contracts.AddAsync(contract);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateContractAsync(Contract contract)
        {
            _context.Contracts.Update(contract);
            await _context.SaveChangesAsync();
        }

        // Pricing
        public async Task<List<PriceList>> GetAllPriceListsAsync()
        {
            return await _context.PriceLists
                .Include(pl => pl.PriceItems)
                .ToListAsync();
        }

        public async Task<PriceList?> GetPriceListByIdAsync(int id)
        {
            return await _context.PriceLists
                .Include(pl => pl.PriceItems)
                .FirstOrDefaultAsync(pl => pl.Id == id);
        }

        public async Task AddPriceListAsync(PriceList priceList)
        {
            await _context.PriceLists.AddAsync(priceList);
            await _context.SaveChangesAsync();
        }

        public async Task AddPriceItemAsync(PriceItem priceItem)
        {
            await _context.PriceItems.AddAsync(priceItem);
            await _context.SaveChangesAsync();
        }
    }
}
