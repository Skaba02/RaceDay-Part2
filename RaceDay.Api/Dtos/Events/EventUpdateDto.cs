namespace RaceDay.Api.Dtos.Events;

// Same shape as creating an event - kept as a separate class so the two
// operations can diverge later without a breaking change.
public class EventUpdateDto : EventCreateDto
{
}
