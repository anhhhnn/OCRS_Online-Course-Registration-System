namespace OCRS.DTOs.SOA;

public sealed record NotificationDto(
    int NotificationId,
    string Title,
    string Content,
    string Type,
    bool IsRead,
    DateTime CreatedAt,
    int? RelatedEntityId);

public sealed record UnreadCountDto(int Count);

public sealed record ApiMessageDto(bool Success, string Message);
