using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.DTOs;
using Chubb.Claims.Domain.Common;
using Chubb.Claims.Domain.Entities;
using Chubb.Claims.Domain.Enums;

namespace Chubb.Claims.Application.Features.Claims.Commands;

// 1. Submit Claim (FNOL)
public sealed record SubmitClaimCommand(
    string PolicyNumber,
    string Market,
    string ClaimType,
    string ClaimantId,
    string ClaimantName,
    string ClaimantEmail,
    string ClaimantPhone,
    DateTime IncidentDateUtc,
    string IncidentLocation,
    string IncidentDescription,
    string AssetIdentifier,
    decimal EstimatedLoss) : ICommand<ClaimDetailDto>;

public sealed class SubmitClaimHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<SubmitClaimCommand, ClaimDetailDto>
{
    public async Task<ClaimDetailDto> HandleAsync(SubmitClaimCommand command, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<MarketCode>(command.Market, ignoreCase: true, out var marketCode))
            throw new DomainException($"Unsupported APAC market '{command.Market}'. Valid markets: SG, HK, AU, MY, TH, ID.");

        if (!Enum.TryParse<ClaimType>(command.ClaimType, ignoreCase: true, out var claimType))
            throw new DomainException($"Unsupported claim type '{command.ClaimType}'. Valid types: Motor, Property.");

        var seq = await repository.GetNextSequenceNumberAsync(marketCode, cancellationToken);
        var claimNumber = $"CHB-{marketCode}-{DateTime.UtcNow:yyyy}-{seq:D4}";

        var claim = Claim.Submit(
            claimNumber,
            command.PolicyNumber,
            marketCode,
            claimType,
            command.ClaimantId,
            command.ClaimantName,
            command.ClaimantEmail,
            command.ClaimantPhone,
            command.IncidentDateUtc,
            command.IncidentLocation,
            command.IncidentDescription,
            command.AssetIdentifier,
            command.EstimatedLoss);

        await repository.AddAsync(claim, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailDto(claim, fxRateService, includeInternalNotes: false);
    }
}

// 2. Pick Up Claim
public sealed record PickUpClaimCommand(
    Guid ClaimId,
    string OfficerId,
    string OfficerName) : ICommand<ClaimDetailDto>;

public sealed class PickUpClaimHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<PickUpClaimCommand, ClaimDetailDto>
{
    public async Task<ClaimDetailDto> HandleAsync(PickUpClaimCommand command, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(command.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{command.ClaimId}' not found.");

        claim.PickUp(command.OfficerId, command.OfficerName);
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailDto(claim, fxRateService, includeInternalNotes: true);
    }
}

// 3. Update Claim Assessment & Reserve
public sealed record UpdateClaimAssessmentCommand(
    Guid ClaimId,
    decimal? AssessedReserve,
    string? InternalNote,
    string OfficerId,
    string OfficerName) : ICommand<ClaimDetailDto>;

public sealed class UpdateClaimAssessmentHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<UpdateClaimAssessmentCommand, ClaimDetailDto>
{
    public async Task<ClaimDetailDto> HandleAsync(UpdateClaimAssessmentCommand command, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(command.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{command.ClaimId}' not found.");

        claim.UpdateAssessment(command.AssessedReserve, command.InternalNote, command.OfficerId, command.OfficerName);
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailDto(claim, fxRateService, includeInternalNotes: true);
    }
}

// 4. Request Additional Info from Claimant
public sealed record RequestClaimInfoCommand(
    Guid ClaimId,
    string Question,
    string OfficerId,
    string OfficerName) : ICommand<ClaimDetailDto>;

public sealed class RequestClaimInfoHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<RequestClaimInfoCommand, ClaimDetailDto>
{
    public async Task<ClaimDetailDto> HandleAsync(RequestClaimInfoCommand command, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(command.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{command.ClaimId}' not found.");

        claim.RequestAdditionalInfo(command.Question, command.OfficerId, command.OfficerName);
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailDto(claim, fxRateService, includeInternalNotes: true);
    }
}

// 5. Claimant Responds to Info Request
public sealed record RespondToClaimInfoCommand(
    Guid ClaimId,
    string Response,
    string? SupportingDocumentRef,
    string ClaimantId,
    string ClaimantName) : ICommand<ClaimDetailDto>;

public sealed class RespondToClaimInfoHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<RespondToClaimInfoCommand, ClaimDetailDto>
{
    public async Task<ClaimDetailDto> HandleAsync(RespondToClaimInfoCommand command, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(command.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{command.ClaimId}' not found.");

        claim.RespondToInformationRequest(command.Response, command.SupportingDocumentRef, command.ClaimantId, command.ClaimantName);
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailDto(claim, fxRateService, includeInternalNotes: false);
    }
}

// 6. Settle Claim
public sealed record SettleClaimCommand(
    Guid ClaimId,
    decimal SettledAmount,
    string DecisionNotes,
    string OfficerId,
    string OfficerName) : ICommand<ClaimDetailDto>;

public sealed class SettleClaimHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<SettleClaimCommand, ClaimDetailDto>
{
    public async Task<ClaimDetailDto> HandleAsync(SettleClaimCommand command, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(command.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{command.ClaimId}' not found.");

        claim.Settle(command.SettledAmount, command.DecisionNotes, command.OfficerId, command.OfficerName);
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailDto(claim, fxRateService, includeInternalNotes: true);
    }
}

// 7. Reject Claim
public sealed record RejectClaimCommand(
    Guid ClaimId,
    string RejectionReason,
    string OfficerId,
    string OfficerName) : ICommand<ClaimDetailDto>;

public sealed class RejectClaimHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<RejectClaimCommand, ClaimDetailDto>
{
    public async Task<ClaimDetailDto> HandleAsync(RejectClaimCommand command, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(command.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{command.ClaimId}' not found.");

        claim.Reject(command.RejectionReason, command.OfficerId, command.OfficerName);
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailDto(claim, fxRateService, includeInternalNotes: true);
    }
}
