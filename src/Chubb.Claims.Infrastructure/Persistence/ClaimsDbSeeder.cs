using Chubb.Claims.Domain.Entities;
using Chubb.Claims.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Chubb.Claims.Infrastructure.Persistence;

public static class ClaimsDbSeeder
{
    public static async Task SeedAsync(ClaimsDbContext dbContext)
    {
        await dbContext.Database.EnsureCreatedAsync();

        if (await dbContext.Claims.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        // 1. SG Motor Claim - UnderReview (Claimant: Tan Wei Ming, Officer: Sarah Jenkins)
        var sgMotor = Claim.Submit(
            claimNumber: "CHB-SG-2026-1001",
            policyNumber: "SG-MTR-884920",
            market: MarketCode.SG,
            claimType: ClaimType.Motor,
            claimantId: "clm-tan",
            claimantName: "Tan Wei Ming",
            claimantEmail: "weiming.tan@example.sg",
            claimantPhone: "+65 9123 4567",
            incidentDateUtc: now.AddDays(-5),
            incidentLocation: "Pan Island Expressway (PIE) Exit 15, Singapore",
            incidentDescription: "Rear-end collision during heavy rain on PIE. Rear bumper, boot lid, and parking sensors damaged.",
            assetIdentifier: "SGP 8821 M (Mercedes-Benz C200)",
            estimatedLoss: 14500m,
            nowUtc: now.AddDays(-5));
        sgMotor.PickUp("off-sarah", "Sarah Jenkins", now.AddDays(-4));
        sgMotor.UpdateAssessment(
            newAssessedReserve: 16200m,
            internalNote: "Authorized workshop quotation received from Cycle & Carriage. Hidden rear reinforcement bar damage adds SGD 1,700 to initial estimate.",
            officerId: "off-sarah",
            officerName: "Sarah Jenkins",
            nowUtc: now.AddDays(-3));

        // 2.SG Property Claim - InfoRequested (Claimant: Tan Wei Ming, Officer: Sarah Jenkins)
        // Great for demoing Claimant responding to Info Request right away!
        var sgPropInfo = Claim.Submit(
            claimNumber: "CHB-SG-2026-1002",
            policyNumber: "SG-PRP-510293",
            market: MarketCode.SG,
            claimType: ClaimType.Property,
            claimantId: "clm-tan",
            claimantName: "Tan Wei Ming",
            claimantEmail: "weiming.tan@example.sg",
            claimantPhone: "+65 9123 4567",
            incidentDateUtc: now.AddDays(-4),
            incidentLocation: "18 Marina Boulevard #24-08, Singapore 018980",
            incidentDescription: "Burst chilled-water pipe in utility ceiling caused flooding across hardwood living room floor and built-in cabinetry.",
            assetIdentifier: "18 Marina Blvd #24-08 (Condominium)",
            estimatedLoss: 28000m,
            nowUtc: now.AddDays(-4));
        sgPropInfo.PickUp("off-sarah", "Sarah Jenkins", now.AddDays(-3));
        sgPropInfo.RequestAdditionalInfo(
            question: "Please provide the MCST plumber's incident verification report and itemized contractor quote for the hardwood parquet replacement.",
            officerId: "off-sarah",
            officerName: "Sarah Jenkins",
            nowUtc: now.AddDays(-2));

        // 3. HK Property Claim - Submitted / Unassigned (Claimant: Chloe Chan)
        // Great for demoing Officer picking up an unassigned high-value claim!
        var hkProp = Claim.Submit(
            claimNumber: "CHB-HK-2026-1001",
            policyNumber: "HK-PRP-339104",
            market: MarketCode.HK,
            claimType: ClaimType.Property,
            claimantId: "clm-chloe",
            claimantName: "Chloe Chan",
            claimantEmail: "chloe.chan@example.hk",
            claimantPhone: "+852 9388 1120",
            incidentDateUtc: now.AddDays(-1),
            incidentLocation: "88 Queensway, Admiralty, Hong Kong",
            incidentDescription: "Typhoon Signal No. 8 wind-driven debris cracked curtain wall glass panel and damaged server room UPS unit.",
            assetIdentifier: "Unit 1902, 88 Queensway, HK",
            estimatedLoss: 420000m, // HKD (~53,760 USD)
            nowUtc: now.AddHours(-18));

        // 4. AU Motor Claim - UnderReview (Claimant: Liam O'Connor, Officer: Rajesh Patel)
        var auMotor = Claim.Submit(
            claimNumber: "CHB-AU-2026-1001",
            policyNumber: "AU-MTR-772190",
            market: MarketCode.AU,
            claimType: ClaimType.Motor,
            claimantId: "clm-liam",
            claimantName: "Liam O'Connor",
            claimantEmail: "liam.oconnor@example.com.au",
            claimantPhone: "+61 412 889 301",
            incidentDateUtc: now.AddDays(-6),
            incidentLocation: "M1 Pacific Motorway, Sydney NSW, Australia",
            incidentDescription: "Severe hailstorm damage across bonnet, roof panel, and front windscreen while parked near Hornsby.",
            assetIdentifier: "NSW-CXR-94A (BMW X5 xDrive40i)",
            estimatedLoss: 24500m, // AUD
            nowUtc: now.AddDays(-6));
        auMotor.PickUp("off-raj", "Rajesh Patel", now.AddDays(-5));
        auMotor.UpdateAssessment(
            newAssessedReserve: 21800m,
            internalNote: "Paintless dent removal (PDR) feasible for side panels; only aluminum bonnet and OEM acoustic windscreen require replacement.",
            officerId: "off-raj",
            officerName: "Rajesh Patel",
            nowUtc: now.AddDays(-3));

        // 5. AU Property Claim - Settled (Claimant: Liam O'Connor, Officer: Rajesh Patel)
        var auPropSettled = Claim.Submit(
            claimNumber: "CHB-AU-2026-1002",
            policyNumber: "AU-PRP-901244",
            market: MarketCode.AU,
            claimType: ClaimType.Property,
            claimantId: "clm-liam",
            claimantName: "Liam O'Connor",
            claimantEmail: "liam.oconnor@example.com.au",
            claimantPhone: "+61 412 889 301",
            incidentDateUtc: now.AddDays(-10),
            incidentLocation: "42 Collins Street, Melbourne VIC 3000",
            incidentDescription: "Electrical surge damaged commercial HVAC compressor and rooftop solar inverter.",
            assetIdentifier: "42 Collins St, Melbourne",
            estimatedLoss: 35000m,
            nowUtc: now.AddDays(-9));
        auPropSettled.PickUp("off-raj", "Rajesh Patel", now.AddDays(-8));
        auPropSettled.Settle(
            settledAmount: 33500m,
            decisionNotes: "Approved full replacement cost of AUD 35,000 less policy excess of AUD 1,500. EFT payout scheduled.",
            officerId: "off-raj",
            officerName: "Rajesh Patel",
            nowUtc: now.AddDays(-7));

        // 6. MY Motor Claim - Submitted / Unassigned (Claimant: Aisha Rahman)
        var myMotor = Claim.Submit(
            claimNumber: "CHB-MY-2026-1001",
            policyNumber: "MY-MTR-440192",
            market: MarketCode.MY,
            claimType: ClaimType.Motor,
            claimantId: "clm-aisha",
            claimantName: "Aisha Rahman",
            claimantEmail: "aisha.rahman@example.my",
            claimantPhone: "+60 12-345 6789",
            incidentDateUtc: now.AddDays(-2),
            incidentLocation: "Federal Highway near Petaling Jaya, Selangor, Malaysia",
            incidentDescription: "Multi-vehicle lane merge collision damaging front left fender, LED headlamp assembly, and control arm.",
            assetIdentifier: "WYY 4012 (Honda Civic RS)",
            estimatedLoss: 38000m, // MYR
            nowUtc: now.AddDays(-2));

        // 7. TH Property Claim - InfoRequested (Claimant: Somchai Charoen, Officer: Narong Srisai)
        var thProp = Claim.Submit(
            claimNumber: "CHB-TH-2026-1001",
            policyNumber: "TH-PRP-610284",
            market: MarketCode.TH,
            claimType: ClaimType.Property,
            claimantId: "clm-somchai",
            claimantName: "Somchai Charoen",
            claimantEmail: "somchai@example.co.th",
            claimantPhone: "+66 81 234 5678",
            incidentDateUtc: now.AddDays(-5),
            incidentLocation: "Sukhumvit Soi 24, Khlong Toei, Bangkok, Thailand",
            incidentDescription: "Monsoon flash flood ingress into ground-floor retail storage damaging inventory and electrical distribution board.",
            assetIdentifier: "89 Sukhumvit Soi 24, Bangkok",
            estimatedLoss: 950000m, // THB (~27,550 USD)
            nowUtc: now.AddDays(-5));
        thProp.PickUp("off-narong", "Narong Srisai", now.AddDays(-4));
        thProp.RequestAdditionalInfo(
            question: "Please upload the audited stock inventory ledger prior to the flood event and photographs of the water line mark.",
            officerId: "off-narong",
            officerName: "Narong Srisai",
            nowUtc: now.AddDays(-2));

        // 8. ID Motor Claim - Rejected (Claimant: Budi Santoso, Officer: Narong Srisai)
        var idMotorRejected = Claim.Submit(
            claimNumber: "CHB-ID-2026-1001",
            policyNumber: "ID-MTR-219004",
            market: MarketCode.ID,
            claimType: ClaimType.Motor,
            claimantId: "clm-budi",
            claimantName: "Budi Santoso",
            claimantEmail: "budi.santoso@example.co.id",
            claimantPhone: "+62 811 987 654",
            incidentDateUtc: now.AddDays(-8),
            incidentLocation: "Jl. Jend. Sudirman, Jakarta Selatan, Indonesia",
            incidentDescription: "Engine hydro-lock after driving through submerged underpass during commercial ride-hailing trip.",
            assetIdentifier: "B 1928 KZX (Toyota Innova Zenix)",
            estimatedLoss: 185000000m, // IDR (~11,840 USD)
            nowUtc: now.AddDays(-8));
        idMotorRejected.PickUp("off-narong", "Narong Srisai", now.AddDays(-7));
        idMotorRejected.Reject(
            rejectionReason: "Policy ID-MTR-219004 covers Private Passenger use only; vehicle was operating for commercial ride-hailing without flood endorsement.",
            officerId: "off-narong",
            officerName: "Narong Srisai",
            nowUtc: now.AddDays(-6));

        // 9. ID Property Claim - UnderReview (Claimant: Budi Santoso, Officer: Narong Srisai)
        var idPropOpen = Claim.Submit(
            claimNumber: "CHB-ID-2026-1002",
            policyNumber: "ID-PRP-774120",
            market: MarketCode.ID,
            claimType: ClaimType.Property,
            claimantId: "clm-budi",
            claimantName: "Budi Santoso",
            claimantEmail: "budi.santoso@example.co.id",
            claimantPhone: "+62 811 987 654",
            incidentDateUtc: now.AddDays(-3),
            incidentLocation: "Kawasan Industri Jababeka, Cikarang, Indonesia",
            incidentDescription: "Warehouse roof truss partial collapse due to localized microburst windstorm.",
            assetIdentifier: "Blok K-14 Jababeka Warehouse",
            estimatedLoss: 450000000m, // IDR (~28,800 USD)
            nowUtc: now.AddDays(-3));
        idPropOpen.PickUp("off-narong", "Narong Srisai", now.AddDays(-2));

        dbContext.Claims.AddRange(
            sgMotor,
            sgPropInfo,
            hkProp,
            auMotor,
            auPropSettled,
            myMotor,
            thProp,
            idMotorRejected,
            idPropOpen);

        await dbContext.SaveChangesAsync();
    }
}
