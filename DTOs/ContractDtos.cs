using System;
using System.ComponentModel.DataAnnotations;
using QTKD.Api.Entities;

namespace QTKD.Api.DTOs
{
    public class CreateContractDto
    {
        public string ContractNo { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? FileUrl { get; set; }
        public string? ContractContent { get; set; }
    }

    public class UpdateContractDto
    {
        public string? ContractNo { get; set; }
        public int? CustomerId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? FileUrl { get; set; }
        public string? ContractContent { get; set; }
    }

    public class SubmitContractRequestDto
    {
        public string? Comment { get; set; }
    }

    public class SubmitContractResponseDto
    {
        public int ContractId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ApprovalId { get; set; } = string.Empty;
    }
}
