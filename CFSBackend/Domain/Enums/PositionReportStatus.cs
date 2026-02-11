namespace ProjectName.Domain.Enums
{
    public enum PositionReportStatus
    {
        // Corresponding to Delphi INI [STATUS.TYPES]
        Unchecked = 'M',    // 'M' - Unchecked
        Cancelled = 'K',    // 'K' - Cancel
        Upload = 'U',       // 'U' - Upload (Approved)
        Error = 'E',        // 'E' - Correction (Error)
        Deleted = 'X'       // 'X' - Logical Delete (as per Delphi 1.4.0 change)
    }
}