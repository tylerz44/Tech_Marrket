namespace TechMarket.ViewModels
{
    public class DemoPaymentViewModel
    {
        public int OrderId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentMethodName { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string TransactionRef { get; set; } = string.Empty;
        public string QrCodeBase64 { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
