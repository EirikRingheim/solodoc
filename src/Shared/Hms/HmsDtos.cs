namespace Solodoc.Shared.Hms;

public record SjaFormListItemDto(
    Guid Id,
    string Title,
    string Status,
    string? ProjectName,
    DateOnly Date,
    int ParticipantCount);

public record SjaFormDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string? ProjectName,
    DateOnly Date,
    string? Location,
    List<SjaHazardDto> Hazards,
    List<SjaParticipantDto> Participants);

public record SjaHazardDto(
    Guid Id,
    string Description,
    int Probability,
    int Consequence,
    int RiskScore,
    string? Mitigation);

public record SjaParticipantDto(
    Guid Id,
    string PersonName,
    bool HasSigned);

public record CreateSjaFormRequest(
    string Title,
    string? Description,
    Guid? ProjectId,
    DateOnly Date,
    string? Location);

public record AddSjaHazardRequest(
    string Description,
    int Probability,
    int Consequence,
    string? Mitigation);

public record HmsMeetingListItemDto(
    Guid Id,
    string Title,
    DateOnly Date,
    string? Location);

public record CreateHmsMeetingRequest(
    string Title,
    DateOnly Date,
    string? Location);

public record AddSjaParticipantRequest(Guid PersonId);

public record HmsMeetingDetailDto(
    Guid Id,
    string Title,
    DateOnly Date,
    string? Location,
    string? Agenda,
    string? Minutes,
    List<HmsActionItemDto> ActionItems);

public record HmsActionItemDto(
    Guid Id,
    string Description,
    string? ResponsibleName,
    DateOnly? Deadline,
    string Status);

public record CreateActionItemRequest(
    string Description,
    Guid? ResponsibleId,
    DateOnly? Deadline);

public record UpdateMinutesRequest(string Minutes);

public record SafetyRoundScheduleDto(
    Guid Id,
    string Name,
    Guid? ProjectId,
    int FrequencyWeeks,
    DateOnly NextDue,
    bool IsActive);
