using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ExperienceMarketplaceRepository : IExperienceMarketplaceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ExperienceMarketplaceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ExperienceLicense> CreateLicenseAsync(ExperienceLicense license, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO experience_licenses (
                id, organization_id, experience_id, license_type, grant_scope, granted_by_user_id, granted_at, expires_at, terms_document_hash, is_active
            ) VALUES (
                @Id, @OrganizationId, @ExperienceId, @LicenseType, @GrantScope, @GrantedByUserId, @GrantedAt, @ExpiresAt, @TermsDocumentHash, @IsActive
            )
            RETURNING id AS Id, organization_id AS OrganizationId, experience_id AS ExperienceId, license_type AS LicenseType,
                      grant_scope AS GrantScope, granted_by_user_id AS GrantedByUserId, granted_at AS GrantedAt,
                      expires_at AS ExpiresAt, terms_document_hash AS TermsDocumentHash, is_active AS IsActive";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleAsync<ExperienceLicense>(sql, license);
    }

    public async Task<ExperienceLicense?> GetActiveLicenseAsync(Guid experienceId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, organization_id AS OrganizationId, experience_id AS ExperienceId, license_type AS LicenseType,
                   grant_scope AS GrantScope, granted_by_user_id AS GrantedByUserId, granted_at AS GrantedAt,
                   expires_at AS ExpiresAt, terms_document_hash AS TermsDocumentHash, is_active AS IsActive
            FROM experience_licenses
            WHERE experience_id = @ExperienceId AND is_active = TRUE
              AND (expires_at IS NULL OR expires_at > NOW())
            ORDER BY granted_at DESC
            LIMIT 1";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<ExperienceLicense>(sql, new { ExperienceId = experienceId });
    }

    public async Task<ExperienceHarvestRequest> SubmitHarvestRequestAsync(ExperienceHarvestRequest request, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO experience_harvest_requests (
                id, tenant_id, organization_id, source_experience_id, source_version_number, license_id,
                submitted_by_user_id, submitted_at, status, review_notes, reviewed_by_user_id, reviewed_at,
                safety_suitability_verified, learning_objectives_verified, instructions_verified,
                materials_verified, media_rights_verified, commercial_rights_verified
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @SourceExperienceId, @SourceVersionNumber, @LicenseId,
                @SubmittedByUserId, @SubmittedAt, @Status, @ReviewNotes, @ReviewedByUserId, @ReviewedAt,
                @SafetySuitabilityVerified, @LearningObjectivesVerified, @InstructionsVerified,
                @MaterialsVerified, @MediaRightsVerified, @CommercialRightsVerified
            )
            RETURNING id AS Id, tenant_id AS TenantId, organization_id AS OrganizationId, source_experience_id AS SourceExperienceId,
                      source_version_number AS SourceVersionNumber, license_id AS LicenseId, submitted_by_user_id AS SubmittedByUserId,
                      submitted_at AS SubmittedAt, status AS Status, review_notes AS ReviewNotes, reviewed_by_user_id AS ReviewedByUserId,
                      reviewed_at AS ReviewedAt, safety_suitability_verified AS SafetySuitabilityVerified,
                      learning_objectives_verified AS LearningObjectivesVerified, instructions_verified AS InstructionsVerified,
                      materials_verified AS MaterialsVerified, media_rights_verified AS MediaRightsVerified,
                      commercial_rights_verified AS CommercialRightsVerified";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleAsync<ExperienceHarvestRequest>(sql, request);
    }

    public async Task<ExperienceHarvestRequest?> GetHarvestRequestByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT r.id AS Id, r.tenant_id AS TenantId, r.organization_id AS OrganizationId, r.source_experience_id AS SourceExperienceId,
                   r.source_version_number AS SourceVersionNumber, r.license_id AS LicenseId, r.submitted_by_user_id AS SubmittedByUserId,
                   r.submitted_at AS SubmittedAt, r.status AS Status, r.review_notes AS ReviewNotes, r.reviewed_by_user_id AS ReviewedByUserId,
                   r.reviewed_at AS ReviewedAt, r.safety_suitability_verified AS SafetySuitabilityVerified,
                   r.learning_objectives_verified AS LearningObjectivesVerified, r.instructions_verified AS InstructionsVerified,
                   r.materials_verified AS MaterialsVerified, r.media_rights_verified AS MediaRightsVerified,
                   r.commercial_rights_verified AS CommercialRightsVerified,
                   e.title AS SourceExperienceTitle, o.name AS OrganizationName, u.email AS SubmitterEmail
            FROM experience_harvest_requests r
            LEFT JOIN experiences e ON r.source_experience_id = e.id
            LEFT JOIN organizations o ON r.organization_id = o.id
            LEFT JOIN users u ON r.submitted_by_user_id = u.id
            WHERE r.id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<ExperienceHarvestRequest>(sql, new { Id = id });
    }

    public async Task<IEnumerable<ExperienceHarvestRequest>> GetHarvestRequestsAsync(Guid? organizationId = null, string? status = null, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT r.id AS Id, r.tenant_id AS TenantId, r.organization_id AS OrganizationId, r.source_experience_id AS SourceExperienceId,
                   r.source_version_number AS SourceVersionNumber, r.license_id AS LicenseId, r.submitted_by_user_id AS SubmittedByUserId,
                   r.submitted_at AS SubmittedAt, r.status AS Status, r.review_notes AS ReviewNotes, r.reviewed_by_user_id AS ReviewedByUserId,
                   r.reviewed_at AS ReviewedAt, r.safety_suitability_verified AS SafetySuitabilityVerified,
                   r.learning_objectives_verified AS LearningObjectivesVerified, r.instructions_verified AS InstructionsVerified,
                   r.materials_verified AS MaterialsVerified, r.media_rights_verified AS MediaRightsVerified,
                   r.commercial_rights_verified AS CommercialRightsVerified,
                   e.title AS SourceExperienceTitle, o.name AS OrganizationName, u.email AS SubmitterEmail
            FROM experience_harvest_requests r
            LEFT JOIN experiences e ON r.source_experience_id = e.id
            LEFT JOIN organizations o ON r.organization_id = o.id
            LEFT JOIN users u ON r.submitted_by_user_id = u.id
            WHERE (@OrganizationId IS NULL OR r.organization_id = @OrganizationId)
              AND (@Status IS NULL OR r.status = @Status)
            ORDER BY r.submitted_at DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QueryAsync<ExperienceHarvestRequest>(sql, new { OrganizationId = organizationId, Status = status });
    }

    public async Task UpdateHarvestRequestAsync(ExperienceHarvestRequest request, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE experience_harvest_requests SET
                status = @Status,
                review_notes = @ReviewNotes,
                reviewed_by_user_id = @ReviewedByUserId,
                reviewed_at = @ReviewedAt,
                safety_suitability_verified = @SafetySuitabilityVerified,
                learning_objectives_verified = @LearningObjectivesVerified,
                instructions_verified = @InstructionsVerified,
                materials_verified = @MaterialsVerified,
                media_rights_verified = @MediaRightsVerified,
                commercial_rights_verified = @CommercialRightsVerified
            WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await connection.ExecuteAsync(sql, request);
    }

    public async Task<PlatformExperience> CreatePlatformExperienceAsync(PlatformExperience experience, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO platform_experiences (
                id, title, description, category, age_range_min_months, age_range_max_months,
                duration_minutes, learning_objectives, required_materials, instructions, safety_notes,
                dna_payload, pricing_model, price_amount, currency, publication_status, ownership_type,
                current_version, license_id, source_harvest_request_id, source_experience_id, source_organization_id,
                creator_attribution, created_at, updated_at
            ) VALUES (
                @Id, @Title, @Description, @Category, @AgeRangeMinMonths, @AgeRangeMaxMonths,
                @DurationMinutes, @LearningObjectives::jsonb, @RequiredMaterials::jsonb, @Instructions, @SafetyNotes,
                @DnaPayload, @PricingModel, @PriceAmount, @Currency, @PublicationStatus, @OwnershipType,
                @CurrentVersion, @LicenseId, @SourceHarvestRequestId, @SourceExperienceId, @SourceOrganizationId,
                @CreatorAttribution, @CreatedAt, @UpdatedAt
            )
            RETURNING id AS Id, title AS Title, description AS Description, category AS Category,
                      age_range_min_months AS AgeRangeMinMonths, age_range_max_months AS AgeRangeMaxMonths,
                      duration_minutes AS DurationMinutes, instructions AS Instructions, safety_notes AS SafetyNotes,
                      dna_payload AS DnaPayload, pricing_model AS PricingModel, price_amount AS PriceAmount,
                      currency AS Currency, publication_status AS PublicationStatus, ownership_type AS OwnershipType,
                      current_version AS CurrentVersion, license_id AS LicenseId, source_harvest_request_id AS SourceHarvestRequestId,
                      source_experience_id AS SourceExperienceId, source_organization_id AS SourceOrganizationId,
                      creator_attribution AS CreatorAttribution, created_at AS CreatedAt, updated_at AS UpdatedAt";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleAsync<PlatformExperience>(sql, experience);
    }

    public async Task<PlatformExperience?> GetPlatformExperienceByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, title AS Title, description AS Description, category AS Category,
                   age_range_min_months AS AgeRangeMinMonths, age_range_max_months AS AgeRangeMaxMonths,
                   duration_minutes AS DurationMinutes, instructions AS Instructions, safety_notes AS SafetyNotes,
                   dna_payload AS DnaPayload, pricing_model AS PricingModel, price_amount AS PriceAmount,
                   currency AS Currency, publication_status AS PublicationStatus, ownership_type AS OwnershipType,
                   current_version AS CurrentVersion, license_id AS LicenseId, source_harvest_request_id AS SourceHarvestRequestId,
                   source_experience_id AS SourceExperienceId, source_organization_id AS SourceOrganizationId,
                   creator_attribution AS CreatorAttribution, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM platform_experiences
            WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<PlatformExperience>(sql, new { Id = id });
    }

    public async Task<IEnumerable<PlatformExperience>> GetMarketplaceCatalogAsync(string? category = null, string? pricingModel = null, string? searchQuery = null, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, title AS Title, description AS Description, category AS Category,
                   age_range_min_months AS AgeRangeMinMonths, age_range_max_months AS AgeRangeMaxMonths,
                   duration_minutes AS DurationMinutes, instructions AS Instructions, safety_notes AS SafetyNotes,
                   dna_payload AS DnaPayload, pricing_model AS PricingModel, price_amount AS PriceAmount,
                   currency AS Currency, publication_status AS PublicationStatus, ownership_type AS OwnershipType,
                   current_version AS CurrentVersion, license_id AS LicenseId,
                   creator_attribution AS CreatorAttribution, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM platform_experiences
            WHERE publication_status = 'Published'
              AND (@Category IS NULL OR category = @Category)
              AND (@PricingModel IS NULL OR pricing_model = @PricingModel)
              AND (@SearchQuery IS NULL OR title ILIKE @SearchPattern OR description ILIKE @SearchPattern)
            ORDER BY created_at DESC";

        var searchPattern = string.IsNullOrWhiteSpace(searchQuery) ? null : $"%{searchQuery.Trim()}%";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QueryAsync<PlatformExperience>(sql, new { Category = category, PricingModel = pricingModel, SearchQuery = searchQuery, SearchPattern = searchPattern });
    }

    public async Task<IEnumerable<PlatformExperience>> GetAllPlatformExperiencesAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, title AS Title, description AS Description, category AS Category,
                   age_range_min_months AS AgeRangeMinMonths, age_range_max_months AS AgeRangeMaxMonths,
                   duration_minutes AS DurationMinutes, instructions AS Instructions, safety_notes AS SafetyNotes,
                   dna_payload AS DnaPayload, pricing_model AS PricingModel, price_amount AS PriceAmount,
                   currency AS Currency, publication_status AS PublicationStatus, ownership_type AS OwnershipType,
                   current_version AS CurrentVersion, license_id AS LicenseId, source_harvest_request_id AS SourceHarvestRequestId,
                   source_experience_id AS SourceExperienceId, source_organization_id AS SourceOrganizationId,
                   creator_attribution AS CreatorAttribution, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM platform_experiences
            ORDER BY created_at DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QueryAsync<PlatformExperience>(sql);
    }

    public async Task UpdatePlatformExperienceAsync(PlatformExperience experience, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE platform_experiences SET
                title = @Title,
                description = @Description,
                category = @Category,
                age_range_min_months = @AgeRangeMinMonths,
                age_range_max_months = @AgeRangeMaxMonths,
                duration_minutes = @DurationMinutes,
                instructions = @Instructions,
                safety_notes = @SafetyNotes,
                dna_payload = @DnaPayload,
                pricing_model = @PricingModel,
                price_amount = @PriceAmount,
                currency = @Currency,
                publication_status = @PublicationStatus,
                ownership_type = @OwnershipType,
                current_version = @CurrentVersion,
                creator_attribution = @CreatorAttribution,
                updated_at = NOW()
            WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await connection.ExecuteAsync(sql, experience);
    }

    public async Task<PlatformExperienceVersion> AddPlatformVersionAsync(PlatformExperienceVersion version, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO platform_experience_versions (
                id, platform_experience_id, version_number, title, description, dna_payload, change_summary, created_by_user_id, created_at, is_active
            ) VALUES (
                @Id, @PlatformExperienceId, @VersionNumber, @Title, @Description, @DnaPayload, @ChangeSummary, @CreatedByUserId, @CreatedAt, @IsActive
            )
            RETURNING id AS Id, platform_experience_id AS PlatformExperienceId, version_number AS VersionNumber,
                      title AS Title, description AS Description, dna_payload AS DnaPayload, change_summary AS ChangeSummary,
                      created_by_user_id AS CreatedByUserId, created_at AS CreatedAt, is_active AS IsActive";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleAsync<PlatformExperienceVersion>(sql, version);
    }

    public async Task<IEnumerable<PlatformExperienceVersion>> GetPlatformVersionsAsync(Guid platformExperienceId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, platform_experience_id AS PlatformExperienceId, version_number AS VersionNumber,
                   title AS Title, description AS Description, dna_payload AS DnaPayload, change_summary AS ChangeSummary,
                   created_by_user_id AS CreatedByUserId, created_at AS CreatedAt, is_active AS IsActive
            FROM platform_experience_versions
            WHERE platform_experience_id = @PlatformExperienceId
            ORDER BY version_number DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QueryAsync<PlatformExperienceVersion>(sql, new { PlatformExperienceId = platformExperienceId });
    }

    public async Task<MarketplaceOrder> CreateOrderAsync(MarketplaceOrder order, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO marketplace_orders (
                id, tenant_id, organization_id, platform_experience_id, purchased_by_user_id,
                pricing_model, amount_paid, currency, payment_status, transaction_reference, purchased_at, refunded_at, refund_reason
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @PlatformExperienceId, @PurchasedByUserId,
                @PricingModel, @AmountPaid, @Currency, @PaymentStatus, @TransactionReference, @PurchasedAt, @RefundedAt, @RefundReason
            )
            ON CONFLICT (organization_id, platform_experience_id, transaction_reference) DO UPDATE
            SET payment_status = EXCLUDED.payment_status
            RETURNING id AS Id, tenant_id AS TenantId, organization_id AS OrganizationId, platform_experience_id AS PlatformExperienceId,
                      purchased_by_user_id AS PurchasedByUserId, pricing_model AS PricingModel, amount_paid AS AmountPaid,
                      currency AS Currency, payment_status AS PaymentStatus, transaction_reference AS TransactionReference,
                      purchased_at AS PurchasedAt, refunded_at AS RefundedAt, refund_reason AS RefundReason";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleAsync<MarketplaceOrder>(sql, order);
    }

    public async Task<MarketplaceOrder?> GetOrderByTransactionRefAsync(Guid organizationId, string transactionReference, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, tenant_id AS TenantId, organization_id AS OrganizationId, platform_experience_id AS PlatformExperienceId,
                   purchased_by_user_id AS PurchasedByUserId, pricing_model AS PricingModel, amount_paid AS AmountPaid,
                   currency AS Currency, payment_status AS PaymentStatus, transaction_reference AS TransactionReference,
                   purchased_at AS PurchasedAt, refunded_at AS RefundedAt, refund_reason AS RefundReason
            FROM marketplace_orders
            WHERE organization_id = @OrganizationId AND transaction_reference = @TransactionReference";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<MarketplaceOrder>(sql, new { OrganizationId = organizationId, TransactionReference = transactionReference });
    }

    public async Task<IEnumerable<MarketplaceOrder>> GetOrdersAsync(Guid? organizationId = null, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, tenant_id AS TenantId, organization_id AS OrganizationId, platform_experience_id AS PlatformExperienceId,
                   purchased_by_user_id AS PurchasedByUserId, pricing_model AS PricingModel, amount_paid AS AmountPaid,
                   currency AS Currency, payment_status AS PaymentStatus, transaction_reference AS TransactionReference,
                   purchased_at AS PurchasedAt, refunded_at AS RefundedAt, refund_reason AS RefundReason
            FROM marketplace_orders
            WHERE (@OrganizationId IS NULL OR organization_id = @OrganizationId)
            ORDER BY purchased_at DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QueryAsync<MarketplaceOrder>(sql, new { OrganizationId = organizationId });
    }

    public async Task<OrganizationEntitlement> CreateOrUpdateEntitlementAsync(OrganizationEntitlement entitlement, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO organization_entitlements (
                id, tenant_id, organization_id, platform_experience_id, order_id,
                acquired_version, entitlement_type, status, granted_at, expires_at, scope
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @PlatformExperienceId, @OrderId,
                @AcquiredVersion, @EntitlementType, @Status, @GrantedAt, @ExpiresAt, @Scope
            )
            ON CONFLICT (organization_id, platform_experience_id) DO UPDATE
            SET acquired_version = EXCLUDED.acquired_version,
                status = EXCLUDED.status,
                expires_at = EXCLUDED.expires_at
            RETURNING id AS Id, tenant_id AS TenantId, organization_id AS OrganizationId, platform_experience_id AS PlatformExperienceId,
                      order_id AS OrderId, acquired_version AS AcquiredVersion, entitlement_type AS EntitlementType,
                      status AS Status, granted_at AS GrantedAt, expires_at AS ExpiresAt, scope AS Scope";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleAsync<OrganizationEntitlement>(sql, entitlement);
    }

    public async Task<OrganizationEntitlement?> GetEntitlementAsync(Guid organizationId, Guid platformExperienceId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, tenant_id AS TenantId, organization_id AS OrganizationId, platform_experience_id AS PlatformExperienceId,
                   order_id AS OrderId, acquired_version AS AcquiredVersion, entitlement_type AS EntitlementType,
                   status AS Status, granted_at AS GrantedAt, expires_at AS ExpiresAt, scope AS Scope
            FROM organization_entitlements
            WHERE organization_id = @OrganizationId AND platform_experience_id = @PlatformExperienceId
              AND status = 'Active' AND (expires_at IS NULL OR expires_at > NOW())";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<OrganizationEntitlement>(sql, new { OrganizationId = organizationId, PlatformExperienceId = platformExperienceId });
    }

    public async Task<IEnumerable<OrganizationEntitlement>> GetOrganizationEntitlementsAsync(Guid organizationId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, tenant_id AS TenantId, organization_id AS OrganizationId, platform_experience_id AS PlatformExperienceId,
                   order_id AS OrderId, acquired_version AS AcquiredVersion, entitlement_type AS EntitlementType,
                   status AS Status, granted_at AS GrantedAt, expires_at AS ExpiresAt, scope AS Scope
            FROM organization_entitlements
            WHERE organization_id = @OrganizationId AND status = 'Active'
              AND (expires_at IS NULL OR expires_at > NOW())
            ORDER BY granted_at DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QueryAsync<OrganizationEntitlement>(sql, new { OrganizationId = organizationId });
    }

    public async Task LogAuditEventAsync(ExperienceAuditEvent auditEvent, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO experience_audit_events (
                id, tenant_id, organization_id, platform_experience_id, action, performed_by_user_id, details, created_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @PlatformExperienceId, @Action, @PerformedByUserId, @Details::jsonb, @CreatedAt
            )";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await connection.ExecuteAsync(sql, auditEvent);
    }

    public async Task<IEnumerable<ExperienceAuditEvent>> GetAuditEventsAsync(Guid? organizationId = null, Guid? platformExperienceId = null, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, tenant_id AS TenantId, organization_id AS OrganizationId,
                   platform_experience_id AS PlatformExperienceId, action AS Action,
                   performed_by_user_id AS PerformedByUserId, details AS Details, created_at AS CreatedAt
            FROM experience_audit_events
            WHERE (@OrganizationId IS NULL OR organization_id = @OrganizationId)
              AND (@PlatformExperienceId IS NULL OR platform_experience_id = @PlatformExperienceId)
            ORDER BY created_at DESC
            LIMIT 200";

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QueryAsync<ExperienceAuditEvent>(sql, new { OrganizationId = organizationId, PlatformExperienceId = platformExperienceId });
    }
}
