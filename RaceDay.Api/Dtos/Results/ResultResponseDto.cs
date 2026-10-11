namespace RaceDay.Api.Dtos.Results;

public class ResultResponseDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
}
