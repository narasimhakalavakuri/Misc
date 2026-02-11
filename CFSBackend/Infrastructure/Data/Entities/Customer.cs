namespace ProjectName.Infrastructure.Data.Entities
{
    public class Customer
    {
        public Guid Id { get; set; }
        public string AcctNo { get; set; } = string.Empty;
        public string? AbbrvName { get; set; } // Abbreviated Name
        public string CustName1 { get; set; } = string.Empty;
        public string? HomeCurrency { get; set; } // Assuming customer's home currency might be stored here
    }
}