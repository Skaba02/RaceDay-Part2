namespace RaceDay.Api.Dtos.Results;

public class ResultCreateDto
{
    /// <summary>Format: hh:mm:ss, e.g. 00:45:12</summary>
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
}
