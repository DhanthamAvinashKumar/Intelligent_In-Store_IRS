using System.ComponentModel.DataAnnotations;

public class SalesHistoryCreateRequest
{
    [Required]
    public long StoreId { get; set; }

    [Required]
    public long ProductId { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public DateTime SaleTime { get; set; }
}

public class SalesHistoryResponse
{
    public long SaleId { get; set; }
    public long StoreId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime SaleTime { get; set; }
}
