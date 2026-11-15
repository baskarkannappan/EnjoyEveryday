using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Services;

public class FacilityProfileService : IFacilityProfileService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly ILogger<FacilityProfileService> _logger;

    public FacilityProfileService(
        IDbConnectionFactory connectionFactory,
        ITenantContext tenantContext,
        IUserContext userContext,
        ILogger<FacilityProfileService> logger)
    {
        _connectionFactory = connectionFactory;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<FacilityProfileDto?> GetProfileAsync(Guid organizationId, Guid? branchId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        // 1. If branchId is specified, try to find the branch-specific profile first
        if (branchId.HasValue && branchId.Value != Guid.Empty)
        {
            const string branchSql = """
                SELECT 
                    p.id, p.tenant_id AS TenantId, p.organization_id AS OrganizationId, p.branch_id AS BranchId,
                    o.name AS OrganizationName, b.name AS BranchName,
                    p.title, p.motto, p.about_story AS AboutStory,
                    p.logo_media_url AS LogoMediaUrl, p.cover_media_url AS CoverMediaUrl,
                    p.gallery_media_urls::text AS GalleryMediaUrlsRaw,
                    p.phone, p.secondary_phone AS SecondaryPhone, p.emergency_phone AS EmergencyPhone,
                    p.email, p.inquiry_email AS InquiryEmail, p.operating_hours AS OperatingHours,
                    p.street_address AS StreetAddress, p.city, p.state, p.postal_code AS PostalCode, p.country,
                    p.map_url AS MapUrl, p.directions_notes AS DirectionsNotes,
                    p.core_values::text AS CoreValuesRaw,
                    p.is_active AS IsActive, p.created_at AS CreatedAt, p.updated_at AS UpdatedAt
                FROM facility_profiles p
                INNER JOIN organizations o ON p.organization_id = o.id
                LEFT JOIN branches b ON p.branch_id = b.id
                WHERE p.tenant_id = @TenantId 
                  AND p.organization_id = @OrganizationId 
                  AND p.branch_id = @BranchId 
                  AND p.is_active = true
                LIMIT 1;
            """;

            var branchRow = await connection.QueryFirstOrDefaultAsync<FacilityProfileRaw>(
                branchSql,
                new { TenantId = tenantId, OrganizationId = organizationId, BranchId = branchId.Value });

            if (branchRow != null)
            {
                return MapRawToDto(branchRow);
            }
        }

        // 2. Fallback to organization-level profile
        const string orgSql = """
            SELECT 
                p.id, p.tenant_id AS TenantId, p.organization_id AS OrganizationId, p.branch_id AS BranchId,
                o.name AS OrganizationName, NULL AS BranchName,
                p.title, p.motto, p.about_story AS AboutStory,
                p.logo_media_url AS LogoMediaUrl, p.cover_media_url AS CoverMediaUrl,
                p.gallery_media_urls::text AS GalleryMediaUrlsRaw,
                p.phone, p.secondary_phone AS SecondaryPhone, p.emergency_phone AS EmergencyPhone,
                p.email, p.inquiry_email AS InquiryEmail, p.operating_hours AS OperatingHours,
                p.street_address AS StreetAddress, p.city, p.state, p.postal_code AS PostalCode, p.country,
                p.map_url AS MapUrl, p.directions_notes AS DirectionsNotes,
                p.core_values::text AS CoreValuesRaw,
                p.is_active AS IsActive, p.created_at AS CreatedAt, p.updated_at AS UpdatedAt
            FROM facility_profiles p
            INNER JOIN organizations o ON p.organization_id = o.id
            WHERE p.tenant_id = @TenantId 
              AND p.organization_id = @OrganizationId 
              AND p.branch_id IS NULL
              AND p.is_active = true
            LIMIT 1;
        """;

        var orgRow = await connection.QueryFirstOrDefaultAsync<FacilityProfileRaw>(
            orgSql,
            new { TenantId = tenantId, OrganizationId = organizationId });

        return orgRow != null ? MapRawToDto(orgRow) : null;
    }

    public async Task<FacilityProfileDto?> GetExactProfileAsync(Guid organizationId, Guid? branchId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = """
            SELECT 
                p.id, p.tenant_id AS TenantId, p.organization_id AS OrganizationId, p.branch_id AS BranchId,
                o.name AS OrganizationName, b.name AS BranchName,
                p.title, p.motto, p.about_story AS AboutStory,
                p.logo_media_url AS LogoMediaUrl, p.cover_media_url AS CoverMediaUrl,
                p.gallery_media_urls::text AS GalleryMediaUrlsRaw,
                p.phone, p.secondary_phone AS SecondaryPhone, p.emergency_phone AS EmergencyPhone,
                p.email, p.inquiry_email AS InquiryEmail, p.operating_hours AS OperatingHours,
                p.street_address AS StreetAddress, p.city, p.state, p.postal_code AS PostalCode, p.country,
                p.map_url AS MapUrl, p.directions_notes AS DirectionsNotes,
                p.core_values::text AS CoreValuesRaw,
                p.is_active AS IsActive, p.created_at AS CreatedAt, p.updated_at AS UpdatedAt
            FROM facility_profiles p
            INNER JOIN organizations o ON p.organization_id = o.id
            LEFT JOIN branches b ON p.branch_id = b.id
            WHERE p.tenant_id = @TenantId 
              AND p.organization_id = @OrganizationId 
              AND ((@BranchId IS NULL AND p.branch_id IS NULL) OR (p.branch_id = @BranchId))
            LIMIT 1;
        """;

        var row = await connection.QueryFirstOrDefaultAsync<FacilityProfileRaw>(
            sql,
            new { TenantId = tenantId, OrganizationId = organizationId, BranchId = branchId });

        return row != null ? MapRawToDto(row) : null;
    }

    public async Task<FacilityProfileDto> SaveProfileAsync(SaveFacilityProfileRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        ValidateAccess(request.OrganizationId, request.BranchId);

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var galleryJson = JsonSerializer.Serialize(request.GalleryMediaUrls ?? new List<string>());
        var coreValuesJson = JsonSerializer.Serialize(request.CoreValues ?? new List<string>());

        if (request.BranchId.HasValue)
        {
            // Branch-level upsert
            const string upsertBranchSql = """
                INSERT INTO facility_profiles (
                    tenant_id, organization_id, branch_id, title, motto, about_story,
                    logo_media_url, cover_media_url, gallery_media_urls,
                    phone, secondary_phone, emergency_phone, email, inquiry_email, operating_hours,
                    street_address, city, state, postal_code, country, map_url, directions_notes,
                    core_values, updated_at, updated_by_user_id
                ) VALUES (
                    @TenantId, @OrganizationId, @BranchId, @Title, @Motto, @AboutStory,
                    @LogoMediaUrl, @CoverMediaUrl, @GalleryJson::jsonb,
                    @Phone, @SecondaryPhone, @EmergencyPhone, @Email, @InquiryEmail, @OperatingHours,
                    @StreetAddress, @City, @State, @PostalCode, @Country, @MapUrl, @DirectionsNotes,
                    @CoreValuesJson::jsonb, NOW(), @UserId
                )
                ON CONFLICT (tenant_id, organization_id, branch_id) WHERE branch_id IS NOT NULL
                DO UPDATE SET
                    title = EXCLUDED.title,
                    motto = EXCLUDED.motto,
                    about_story = EXCLUDED.about_story,
                    logo_media_url = EXCLUDED.logo_media_url,
                    cover_media_url = EXCLUDED.cover_media_url,
                    gallery_media_urls = EXCLUDED.gallery_media_urls,
                    phone = EXCLUDED.phone,
                    secondary_phone = EXCLUDED.secondary_phone,
                    emergency_phone = EXCLUDED.emergency_phone,
                    email = EXCLUDED.email,
                    inquiry_email = EXCLUDED.inquiry_email,
                    operating_hours = EXCLUDED.operating_hours,
                    street_address = EXCLUDED.street_address,
                    city = EXCLUDED.city,
                    state = EXCLUDED.state,
                    postal_code = EXCLUDED.postal_code,
                    country = EXCLUDED.country,
                    map_url = EXCLUDED.map_url,
                    directions_notes = EXCLUDED.directions_notes,
                    core_values = EXCLUDED.core_values,
                    updated_at = NOW(),
                    updated_by_user_id = EXCLUDED.updated_by_user_id
                RETURNING id;
            """;

            var profileId = await connection.ExecuteScalarAsync<Guid>(
                upsertBranchSql,
                new
                {
                    TenantId = tenantId,
                    request.OrganizationId,
                    BranchId = request.BranchId.Value,
                    request.Title,
                    request.Motto,
                    request.AboutStory,
                    request.LogoMediaUrl,
                    request.CoverMediaUrl,
                    GalleryJson = galleryJson,
                    request.Phone,
                    request.SecondaryPhone,
                    request.EmergencyPhone,
                    request.Email,
                    request.InquiryEmail,
                    request.OperatingHours,
                    request.StreetAddress,
                    request.City,
                    request.State,
                    request.PostalCode,
                    Country = request.Country ?? "Canada",
                    request.MapUrl,
                    request.DirectionsNotes,
                    CoreValuesJson = coreValuesJson,
                    UserId = _userContext.UserId == Guid.Empty ? (Guid?)null : _userContext.UserId
                });

            var result = await GetExactProfileAsync(request.OrganizationId, request.BranchId, ct);
            return result!;
        }
        else
        {
            // Organization-level upsert
            const string upsertOrgSql = """
                INSERT INTO facility_profiles (
                    tenant_id, organization_id, branch_id, title, motto, about_story,
                    logo_media_url, cover_media_url, gallery_media_urls,
                    phone, secondary_phone, emergency_phone, email, inquiry_email, operating_hours,
                    street_address, city, state, postal_code, country, map_url, directions_notes,
                    core_values, updated_at, updated_by_user_id
                ) VALUES (
                    @TenantId, @OrganizationId, NULL, @Title, @Motto, @AboutStory,
                    @LogoMediaUrl, @CoverMediaUrl, @GalleryJson::jsonb,
                    @Phone, @SecondaryPhone, @EmergencyPhone, @Email, @InquiryEmail, @OperatingHours,
                    @StreetAddress, @City, @State, @PostalCode, @Country, @MapUrl, @DirectionsNotes,
                    @CoreValuesJson::jsonb, NOW(), @UserId
                )
                ON CONFLICT (tenant_id, organization_id) WHERE branch_id IS NULL
                DO UPDATE SET
                    title = EXCLUDED.title,
                    motto = EXCLUDED.motto,
                    about_story = EXCLUDED.about_story,
                    logo_media_url = EXCLUDED.logo_media_url,
                    cover_media_url = EXCLUDED.cover_media_url,
                    gallery_media_urls = EXCLUDED.gallery_media_urls,
                    phone = EXCLUDED.phone,
                    secondary_phone = EXCLUDED.secondary_phone,
                    emergency_phone = EXCLUDED.emergency_phone,
                    email = EXCLUDED.email,
                    inquiry_email = EXCLUDED.inquiry_email,
                    operating_hours = EXCLUDED.operating_hours,
                    street_address = EXCLUDED.street_address,
                    city = EXCLUDED.city,
                    state = EXCLUDED.state,
                    postal_code = EXCLUDED.postal_code,
                    country = EXCLUDED.country,
                    map_url = EXCLUDED.map_url,
                    directions_notes = EXCLUDED.directions_notes,
                    core_values = EXCLUDED.core_values,
                    updated_at = NOW(),
                    updated_by_user_id = EXCLUDED.updated_by_user_id
                RETURNING id;
            """;

            var profileId = await connection.ExecuteScalarAsync<Guid>(
                upsertOrgSql,
                new
                {
                    TenantId = tenantId,
                    request.OrganizationId,
                    request.Title,
                    request.Motto,
                    request.AboutStory,
                    request.LogoMediaUrl,
                    request.CoverMediaUrl,
                    GalleryJson = galleryJson,
                    request.Phone,
                    request.SecondaryPhone,
                    request.EmergencyPhone,
                    request.Email,
                    request.InquiryEmail,
                    request.OperatingHours,
                    request.StreetAddress,
                    request.City,
                    request.State,
                    request.PostalCode,
                    Country = request.Country ?? "Canada",
                    request.MapUrl,
                    request.DirectionsNotes,
                    CoreValuesJson = coreValuesJson,
                    UserId = _userContext.UserId == Guid.Empty ? (Guid?)null : _userContext.UserId
                });

            var result = await GetExactProfileAsync(request.OrganizationId, null, ct);
            return result!;
        }
    }

    public async Task<IEnumerable<BranchOptionDto>> GetEditableBranchesAsync(Guid organizationId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = """
            SELECT id, name, location 
            FROM branches 
            WHERE tenant_id = @TenantId 
              AND organization_id = @OrganizationId 
              AND is_active = true
            ORDER BY name;
        """;

        var branches = await connection.QueryAsync<BranchOptionDto>(sql, new { TenantId = tenantId, OrganizationId = organizationId });

        // If user is a branch admin scoped to a specific branch, restrict to only that branch
        if (_userContext.BranchId.HasValue && !IsOrgAdmin())
        {
            return branches.Where(b => b.Id == _userContext.BranchId.Value);
        }

        return branches;
    }

    public async Task<IEnumerable<BranchOptionDto>> GetPublicBranchesAsync(Guid organizationId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = """
            SELECT id, name, location 
            FROM branches 
            WHERE tenant_id = @TenantId 
              AND organization_id = @OrganizationId 
              AND is_active = true
            ORDER BY name;
        """;

        return await connection.QueryAsync<BranchOptionDto>(sql, new { TenantId = tenantId, OrganizationId = organizationId });
    }

    public async Task<ContactInquiryDto> SubmitInquiryAsync(CreateContactInquiryRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = """
            INSERT INTO contact_inquiries (
                tenant_id, organization_id, branch_id, sender_user_id,
                sender_name, sender_email, sender_phone, category, subject, message, status
            ) VALUES (
                @TenantId, @OrganizationId, @BranchId, @SenderUserId,
                @SenderName, @SenderEmail, @SenderPhone, @Category, @Subject, @Message, 'New'
            )
            RETURNING id, created_at;
        """;

        var inserted = await connection.QuerySingleAsync<(Guid Id, DateTime CreatedAt)>(
            sql,
            new
            {
                TenantId = tenantId,
                request.OrganizationId,
                request.BranchId,
                SenderUserId = request.SenderUserId ?? (_userContext.UserId == Guid.Empty ? (Guid?)null : _userContext.UserId),
                request.SenderName,
                request.SenderEmail,
                request.SenderPhone,
                Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category,
                request.Subject,
                request.Message
            });

        return new ContactInquiryDto
        {
            Id = inserted.Id,
            TenantId = tenantId,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            SenderUserId = request.SenderUserId,
            SenderName = request.SenderName,
            SenderEmail = request.SenderEmail,
            SenderPhone = request.SenderPhone,
            Category = request.Category,
            Subject = request.Subject,
            Message = request.Message,
            Status = "New",
            CreatedAt = inserted.CreatedAt
        };
    }

    public async Task<IEnumerable<ContactInquiryDto>> GetInquiriesAsync(Guid organizationId, Guid? branchId = null, string? status = null, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = """
            SELECT 
                ci.id, ci.tenant_id AS TenantId, ci.organization_id AS OrganizationId, ci.branch_id AS BranchId,
                b.name AS BranchName, ci.sender_user_id AS SenderUserId,
                ci.sender_name AS SenderName, ci.sender_email AS SenderEmail, ci.sender_phone AS SenderPhone,
                ci.category, ci.subject, ci.message, ci.status, ci.admin_notes AS AdminNotes,
                ci.created_at AS CreatedAt, ci.resolved_at AS ResolvedAt
            FROM contact_inquiries ci
            LEFT JOIN branches b ON ci.branch_id = b.id
            WHERE ci.tenant_id = @TenantId
              AND ci.organization_id = @OrganizationId
              AND (@BranchId IS NULL OR ci.branch_id = @BranchId)
              AND (@Status IS NULL OR ci.status = @Status)
            ORDER BY ci.created_at DESC;
        """;

        return await connection.QueryAsync<ContactInquiryDto>(
            sql,
            new { TenantId = tenantId, OrganizationId = organizationId, BranchId = branchId, Status = status });
    }

    public async Task<IEnumerable<ContactInquiryDto>> GetAllPlatformInquiriesAsync(Guid? organizationId = null, string? status = null, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = """
            SELECT 
                ci.id, ci.tenant_id AS TenantId, ci.organization_id AS OrganizationId, ci.branch_id AS BranchId,
                o.name AS OrganizationName, b.name AS BranchName, ci.sender_user_id AS SenderUserId,
                ci.sender_name AS SenderName, ci.sender_email AS SenderEmail, ci.sender_phone AS SenderPhone,
                ci.category, ci.subject, ci.message, ci.status, ci.admin_notes AS AdminNotes,
                ci.created_at AS CreatedAt, ci.resolved_at AS ResolvedAt
            FROM contact_inquiries ci
            LEFT JOIN organizations o ON ci.organization_id = o.id
            LEFT JOIN branches b ON ci.branch_id = b.id
            WHERE (@OrganizationId IS NULL OR ci.organization_id = @OrganizationId)
              AND (@Status IS NULL OR ci.status = @Status)
            ORDER BY ci.created_at DESC;
        """;

        return await connection.QueryAsync<ContactInquiryDto>(
            sql,
            new { OrganizationId = organizationId, Status = status });
    }

    public async Task UpdateInquiryStatusAsync(UpdateInquiryStatusRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = """
            UPDATE contact_inquiries
            SET 
                status = @Status,
                admin_notes = COALESCE(@AdminNotes, admin_notes),
                resolved_at = CASE WHEN @Status = 'Resolved' THEN NOW() ELSE resolved_at END,
                resolved_by_user_id = CASE WHEN @Status = 'Resolved' THEN @UserId ELSE resolved_by_user_id END
            WHERE id = @InquiryId AND tenant_id = @TenantId;
        """;

        await connection.ExecuteAsync(
            sql,
            new
            {
                request.InquiryId,
                TenantId = tenantId,
                request.Status,
                request.AdminNotes,
                UserId = _userContext.UserId == Guid.Empty ? (Guid?)null : _userContext.UserId
            });
    }

    private bool IsOrgAdmin()
    {
        return _userContext.Roles.Any(r =>
            r.Equals("OrgAdmin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Superadmin", StringComparison.OrdinalIgnoreCase)) ||
            _userContext.HasPermission("organization.manage");
    }

    private void ValidateAccess(Guid organizationId, Guid? branchId)
    {
        if (_userContext.OrganizationId.HasValue && _userContext.OrganizationId.Value != organizationId)
        {
            throw new UnauthorizedAccessException("Cannot manage facility profile for another organization.");
        }

        if (branchId.HasValue)
        {
            // Branch level: either Org Admin or assigned Branch Admin
            if (!IsOrgAdmin() && _userContext.BranchId.HasValue && _userContext.BranchId.Value != branchId.Value)
            {
                throw new UnauthorizedAccessException("Cannot manage facility profile for another branch.");
            }
        }
        else
        {
            // Org level: only Org Admin can configure org-wide profile
            if (!IsOrgAdmin())
            {
                throw new UnauthorizedAccessException("Only Organization Administrators can manage organization-wide profiles.");
            }
        }
    }

    private static FacilityProfileDto MapRawToDto(FacilityProfileRaw raw)
    {
        List<string> gallery = new();
        if (!string.IsNullOrWhiteSpace(raw.GalleryMediaUrlsRaw))
        {
            try { gallery = JsonSerializer.Deserialize<List<string>>(raw.GalleryMediaUrlsRaw) ?? new(); } catch { }
        }

        List<string> values = new();
        if (!string.IsNullOrWhiteSpace(raw.CoreValuesRaw))
        {
            try { values = JsonSerializer.Deserialize<List<string>>(raw.CoreValuesRaw) ?? new(); } catch { }
        }

        return new FacilityProfileDto
        {
            Id = raw.Id,
            TenantId = raw.TenantId,
            OrganizationId = raw.OrganizationId,
            BranchId = raw.BranchId,
            OrganizationName = raw.OrganizationName,
            BranchName = raw.BranchName,
            Title = raw.Title,
            Motto = raw.Motto,
            AboutStory = raw.AboutStory,
            LogoMediaUrl = raw.LogoMediaUrl,
            CoverMediaUrl = raw.CoverMediaUrl,
            GalleryMediaUrls = gallery,
            Phone = raw.Phone,
            SecondaryPhone = raw.SecondaryPhone,
            EmergencyPhone = raw.EmergencyPhone,
            Email = raw.Email,
            InquiryEmail = raw.InquiryEmail,
            OperatingHours = raw.OperatingHours,
            StreetAddress = raw.StreetAddress,
            City = raw.City,
            State = raw.State,
            PostalCode = raw.PostalCode,
            Country = raw.Country,
            MapUrl = raw.MapUrl,
            DirectionsNotes = raw.DirectionsNotes,
            CoreValues = values,
            IsActive = raw.IsActive,
            CreatedAt = raw.CreatedAt,
            UpdatedAt = raw.UpdatedAt
        };
    }

    private class FacilityProfileRaw
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? BranchId { get; set; }
        public string? OrganizationName { get; set; }
        public string? BranchName { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Motto { get; set; }
        public string? AboutStory { get; set; }
        public string? LogoMediaUrl { get; set; }
        public string? CoverMediaUrl { get; set; }
        public string? GalleryMediaUrlsRaw { get; set; }
        public string? Phone { get; set; }
        public string? SecondaryPhone { get; set; }
        public string? EmergencyPhone { get; set; }
        public string? Email { get; set; }
        public string? InquiryEmail { get; set; }
        public string? OperatingHours { get; set; }
        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? MapUrl { get; set; }
        public string? DirectionsNotes { get; set; }
        public string? CoreValuesRaw { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

