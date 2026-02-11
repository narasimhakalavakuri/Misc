namespace ProjectName.Application.Models.PositionReports
{
    public record CurrencyDetailsDto(
        string CurrCode,
        int Deciml,
        decimal Tts
    );
}