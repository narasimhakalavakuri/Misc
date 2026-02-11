namespace ProjectName.Infrastructure.Data.Entities
{
    public class MiscSetting
    {
        public Guid Id { get; set; } // Primary key
        public string DataId1 { get; set; } = string.Empty; // e.g., "NOSTRO", "APP_SETTING"
        public string DataId2 { get; set; } = string.Empty; // e.g., "SGD", "MAX_USERS"
        public string? Data01 { get; set; } // Generic data fields
        public string? Data02 { get; set; }
        // Add more generic data fields (Data03, Data04, etc.) as needed based on Delphi usage
    }
}