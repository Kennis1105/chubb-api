namespace Chubb.Claims.Application.Contracts.Responses;

public sealed record ClaimNoteResponse(
    Guid Id,
    string AuthorId,
    string AuthorName,
    string Content,
    bool IsInternalOnly,
    DateTime CreatedAtUtc);
