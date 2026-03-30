namespace AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;

public class BulkItemDtos
{
    public List<CreateBulkItemRequest> Items { get; set; } = new List<CreateBulkItemRequest>();
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int totalSuccessfulUploadedCount { get; set; }
    public int totalErrorsUploadedCount { get; set; }
}

public class CreateBulkItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public CreateBulkItemServiceCodeRequest Service { get; set; } = new();
    public CreateBulkItemCategoryRequest Category { get; set; } = new();
}

public class CreateBulkItemServiceCodeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class CreateBulkItemCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Percent { get; set; }
}

public record BulkItemResult(bool IsSuccess, string Message);


