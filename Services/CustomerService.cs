using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;
using QTKD.Api.Exceptions;
using QTKD.Api.Repositories;

namespace QTKD.Api.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IQtkdRepository _repository;

        public CustomerService(IQtkdRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _repository.GetAllCustomersAsync();
        }

        public async Task<Customer> GetByIdAsync(int id)
        {
            var customer = await _repository.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy khách hàng có ID = {id}");
            }
            return customer;
        }

        public async Task<Customer> CreateAsync(CreateCustomerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new BusinessRuleException("Tên khách hàng không được để trống.");
            }

            var customer = new Customer
            {
                Name = dto.Name.Trim(),
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status.Trim()
            };

            await _repository.AddCustomerAsync(customer);
            return customer;
        }

        public async Task<Customer> UpdateAsync(int id, UpdateCustomerDto dto)
        {
            var customer = await _repository.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy khách hàng có ID = {id}");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new BusinessRuleException("Tên khách hàng không được để trống.");
            }

            customer.Name = dto.Name.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                customer.Status = dto.Status.Trim();
            }

            await _repository.UpdateCustomerAsync(customer);
            return customer;
        }
    }
}
