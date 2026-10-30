namespace RaceDay.Web.Models.Api;

// These classes only describe the JSON the RaceDay API sends back.
// They hold no rules and no database code.

public class AuthResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class CreatedEvent
{
    public int EventId { get; set; }
}

public class EventListItem
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string OrganiserName { get; set; } = string.Empty;
    public int CategoryCount { get; set; }
    public bool HasBanner { get; set; }
}

public class CategoryInfo
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public int EnrolledCount { get; set; }
    public bool IsFull { get; set; }
}

public class EventDetails : EventListItem
{
    public int OrganiserId { get; set; }
    public List<CategoryInfo> Categories { get; set; } = new();
}

public class OrganiserEventSummary
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int CategoryCount { get; set; }
    public int TotalEnrolments { get; set; }
    public bool HasBanner { get; set; }
}

public class OrganiserDashboard
{
    public int TotalEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public DateTime? NextEventDate { get; set; }
    public List<OrganiserEventSummary> Events { get; set; } = new();
}

public class EnrolmentRow
{
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public bool HasResult { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
}

public class MyEnrolment
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public bool HasResult { get; set; }
}

public class MyResult
{
    public int ResultId { get; set; }
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public int TotalFinishers { get; set; }
}

public class ProfileInfo
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool HasProfilePicture { get; set; }
}
