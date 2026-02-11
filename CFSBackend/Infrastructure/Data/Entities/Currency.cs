namespace ProjectName.Infrastructure.Data.Entities
{
    public class Currency
    {
        public string CurrCode { get; set; } = string.Empty; // Primary Key, e.g., "SGD"
        public string CurrDesc { get; set; } = string.Empty;
        public int Deciml { get; set; } // Number of decimal places
        public decimal Tts { get; set; } // Telegraphic Transfer Selling Rate
        public decimal Sts { get; set; } // Selling rate (could be same as TTS or different)
        public decimal Bts { get; set; } // Buying rate (could be same as TTS or different)
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}