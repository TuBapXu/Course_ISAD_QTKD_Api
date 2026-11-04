using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;
using QTKD.Api.Exceptions;
using QTKD.Api.Repositories;

namespace QTKD.Api.Services
{
    public class ContractService : IContractService
    {
        private readonly IQtkdRepository _repository;

        public ContractService(IQtkdRepository repository)
        {
            _repository = repository;
        }

        public async Task<Contract> GetByIdAsync(int id)
        {
            var contract = await _repository.GetContractByIdAsync(id);
            if (contract == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy hợp đồng có ID = {id}");
            }
            return contract;
        }

        public async Task<Contract> CreateAsync(CreateContractDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.ContractNo))
            {
                errors.Add("Số hợp đồng không được để trống.");
            }

            if (dto.CustomerId <= 0)
            {
                errors.Add("Mã khách hàng phải lớn hơn 0.");
            }
            else
            {
                var customer = await _repository.GetCustomerByIdAsync(dto.CustomerId);
                if (customer == null || !customer.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Khách hàng được tham chiếu không tồn tại hoặc không ở trạng thái Active.");
                }
            }

            if (dto.StartDate >= dto.EndDate)
            {
                errors.Add("Thời gian hiệu lực không hợp lệ: Ngày kết thúc phải sau ngày bắt đầu.");
            }

            if (errors.Count > 0)
            {
                throw new BusinessRuleException(errors);
            }

            var contract = new Contract
            {
                ContractNo = dto.ContractNo.Trim(),
                CustomerId = dto.CustomerId,
                StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc),
                Status = ContractStatus.Draft, // Luôn khởi tạo ở trạng thái Bản nháp
                FileUrl = dto.FileUrl?.Trim(),
                ContractContent = dto.ContractContent?.Trim()
            };

            await _repository.AddContractAsync(contract);
            return contract;
        }

        public async Task<Contract> UpdateAsync(int id, UpdateContractDto dto)
        {
            var contract = await _repository.GetContractByIdAsync(id);
            if (contract == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy hợp đồng có ID = {id}");
            }

            // CTR-01: Chỉ cho sửa hợp đồng ở Draft hoặc RevisionRequested
            if (contract.Status != ContractStatus.Draft && contract.Status != ContractStatus.RevisionRequested)
            {
                throw new StateConflictException($"Vi phạm CTR-01: Không thể sửa hợp đồng ở trạng thái '{contract.Status}'. Chỉ được sửa khi ở trạng thái Draft hoặc RevisionRequested.");
            }

            if (dto.CustomerId.HasValue)
            {
                var customer = await _repository.GetCustomerByIdAsync(dto.CustomerId.Value);
                if (customer == null || !customer.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessRuleException("Khách hàng được cập nhật không tồn tại hoặc không ở trạng thái Active.");
                }
                contract.CustomerId = dto.CustomerId.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.ContractNo))
            {
                contract.ContractNo = dto.ContractNo.Trim();
            }

            if (dto.StartDate.HasValue)
            {
                contract.StartDate = DateTime.SpecifyKind(dto.StartDate.Value, DateTimeKind.Utc);
            }

            if (dto.EndDate.HasValue)
            {
                contract.EndDate = DateTime.SpecifyKind(dto.EndDate.Value, DateTimeKind.Utc);
            }

            if (contract.StartDate >= contract.EndDate)
            {
                throw new BusinessRuleException("Thời gian hiệu lực không hợp lệ: Ngày kết thúc phải sau ngày bắt đầu.");
            }

            if (dto.FileUrl != null)
            {
                contract.FileUrl = dto.FileUrl.Trim();
            }

            if (dto.ContractContent != null)
            {
                contract.ContractContent = dto.ContractContent.Trim();
            }

            await _repository.UpdateContractAsync(contract);
            return contract;
        }

        public async Task<SubmitContractResponseDto> SubmitContractAsync(int id, string? userRole, string? idempotencyKey, SubmitContractRequestDto? dto)
        {
            // 1. KIỂM TRA QUYỀN NGƯỜI DÙNG (AC01 -> 403 Forbidden)
            if (string.IsNullOrWhiteSpace(userRole) || !userRole.Equals("Sales", StringComparison.OrdinalIgnoreCase))
            {
                throw new ForbiddenException("Vi phạm AC01: Người dùng không có quyền gửi hợp đồng trình duyệt. Yêu cầu quyền Nhân viên Kinh doanh (Sales).");
            }

            // Tìm hợp đồng (nếu không có -> 404 Not Found)
            var contract = await _repository.GetContractByIdAsync(id);
            if (contract == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy hợp đồng có ID = {id}");
            }

            // 2. KIỂM TRA GỬI LẶP & IDEMPOTENCY (AC05, TC05, TC06)
            if (contract.Status == ContractStatus.PendingApproval)
            {
                // Nếu cùng Idempotency-Key -> Trả về kết quả phiên cũ (không tạo workflow thứ 2)
                if (!string.IsNullOrWhiteSpace(idempotencyKey) && idempotencyKey == contract.LastIdempotencyKey)
                {
                    return new SubmitContractResponseDto
                    {
                        ContractId = contract.Id,
                        Status = contract.Status.ToString(),
                        ApprovalId = contract.CurrentApprovalId ?? $"APP-{contract.Id}"
                    };
                }

                // Nếu gửi lại mà không có key hoặc gửi bình thường khi đang PendingApproval -> Báo sai trạng thái (AC02, TC02 -> 409 Conflict)
                throw new StateConflictException("Vi phạm AC02: Hợp đồng đã ở trạng thái Chờ phê duyệt (PendingApproval), không thể gửi lại.");
            }

            // 3. KIỂM TRA TRẠNG THÁI CHO PHÉP GỬI (AC02 -> 409 Conflict)
            if (contract.Status != ContractStatus.Draft && contract.Status != ContractStatus.RevisionRequested)
            {
                throw new StateConflictException($"Vi phạm AC02: Hợp đồng sai trạng thái ({contract.Status}). Chỉ được gửi trình duyệt khi ở trạng thái Draft hoặc RevisionRequested.");
            }

            // 4. KIỂM TRA ĐIỀU KIỆN CTR-02 (AC03 -> 400 Bad Request kèm mảng lỗi)
            var errors = new List<string>();

            if (contract.Customer == null || !contract.Customer.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Khách hàng không tồn tại hoặc không ở trạng thái hoạt động (Active).");
            }

            if (contract.StartDate >= contract.EndDate)
            {
                errors.Add("Thời gian hiệu lực không hợp lệ (Ngày kết thúc phải sau ngày bắt đầu).");
            }

            if (string.IsNullOrWhiteSpace(contract.FileUrl))
            {
                errors.Add("Thiếu tệp đính kèm bắt buộc (FileUrl không được để trống).");
            }

            if (string.IsNullOrWhiteSpace(contract.ContractContent) ||
                (!contract.ContractContent.Contains("Cộng hòa xã hội chủ nghĩa Việt Nam", StringComparison.OrdinalIgnoreCase) &&
                 !contract.ContractContent.Contains("Cong hoa xa hoi chu nghia Viet Nam", StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add("Nội dung hợp đồng bắt buộc chưa đầy đủ (phải có Quốc hiệu 'Cộng hòa xã hội chủ nghĩa Việt Nam').");
            }

            if (errors.Count > 0)
            {
                // AC03: Không đạt CTR-02 -> Không thay đổi dữ liệu, trả danh sách điều kiện chưa đạt
                throw new BusinessRuleException(errors);
            }

            // 5. CTR-03 & AC04: Chuyển sang PendingApproval (tuyệt đối không chuyển sang Approved) & tạo Approval Instance
            var approvalId = $"APP-{contract.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";

            contract.Status = ContractStatus.PendingApproval;
            contract.CurrentApprovalId = approvalId;
            contract.LastIdempotencyKey = idempotencyKey;

            await _repository.UpdateContractAsync(contract);

            return new SubmitContractResponseDto
            {
                ContractId = contract.Id,
                Status = contract.Status.ToString(),
                ApprovalId = approvalId
            };
        }
    }
}
