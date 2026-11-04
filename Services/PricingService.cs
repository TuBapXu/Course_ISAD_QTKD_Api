using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;
using QTKD.Api.Exceptions;
using QTKD.Api.Repositories;

namespace QTKD.Api.Services
{
    public class PricingService : IPricingService
    {
        private readonly IQtkdRepository _repository;

        public PricingService(IQtkdRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<PriceList>> GetAllPriceListsAsync()
        {
            return await _repository.GetAllPriceListsAsync();
        }

        public async Task<PriceList> GetPriceListByIdAsync(int id)
        {
            var priceList = await _repository.GetPriceListByIdAsync(id);
            if (priceList == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy bảng giá có ID = {id}");
            }
            return priceList;
        }

        public async Task<PriceList> CreatePriceListAsync(CreatePriceListDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new BusinessRuleException("Tên bảng giá không được để trống.");
            }

            var priceList = new PriceList
            {
                Name = dto.Name.Trim(),
                EffectiveDate = dto.EffectiveDate == default ? DateTime.UtcNow : DateTime.SpecifyKind(dto.EffectiveDate, DateTimeKind.Utc)
            };

            await _repository.AddPriceListAsync(priceList);
            return priceList;
        }

        public async Task<PriceItem> AddPriceItemAsync(int priceListId, CreatePriceItemDto dto)
        {
            var priceList = await _repository.GetPriceListByIdAsync(priceListId);
            if (priceList == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy bảng giá có ID = {priceListId}");
            }

            if (string.IsNullOrWhiteSpace(dto.ServiceCode))
            {
                throw new BusinessRuleException("Mã dịch vụ không được để trống.");
            }

            if (dto.UnitPrice <= 0)
            {
                throw new BusinessRuleException("Đơn giá dịch vụ phải lớn hơn 0.");
            }

            var priceItem = new PriceItem
            {
                PriceListId = priceListId,
                ServiceCode = dto.ServiceCode.Trim(),
                UnitPrice = dto.UnitPrice
            };

            await _repository.AddPriceItemAsync(priceItem);
            return priceItem;
        }
    }
}
