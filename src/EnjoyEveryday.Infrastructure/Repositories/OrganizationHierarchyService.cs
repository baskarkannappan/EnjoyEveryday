using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class OrganizationHierarchyService : IOrganizationHierarchyService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public OrganizationHierarchyService(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
    }

    public async Task<OrganizationHierarchyDto> GetOrganizationHierarchyAsync(
        Guid tenantId,
        Guid organizationId,
        Guid requestingUserId,
        bool isOrgAdmin,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
            return new OrganizationHierarchyDto { OrganizationId = organizationId };

        using var conn = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        var json = await conn.ExecuteScalarAsync<string>(
            new CommandDefinition(
                "SELECT fn_get_organization_hierarchy(@tenantId, @organizationId, @requestingUserId, @isOrgAdmin)::text",
                new { tenantId, organizationId, requestingUserId, isOrgAdmin },
                cancellationToken: cancellationToken));

        if (string.IsNullOrWhiteSpace(json))
            return new OrganizationHierarchyDto { OrganizationId = organizationId };

        var dto = JsonSerializer.Deserialize<OrganizationHierarchyDto>(json, JsonOptions);
        return dto ?? new OrganizationHierarchyDto { OrganizationId = organizationId };
    }

    public async Task<List<StudentNodeDto>> GetClassroomStudentsAsync(
        Guid tenantId,
        Guid classroomId,
        CancellationToken cancellationToken = default)
    {
        using var conn = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var children = await conn.QueryAsync<ChildRow>(
            new CommandDefinition(
                @"SELECT ch.id as child_id, ch.classroom_id,
                         TRIM(ch.first_name || ' ' || ch.last_name) as full_name,
                         ch.photo_url,
                         c.name as classroom_name
                  FROM children ch
                  JOIN classrooms c ON ch.classroom_id = c.id
                  WHERE ch.classroom_id = @ClassroomId 
                    AND ch.is_active = true
                    AND (ch.enrollment_status IS NULL OR ch.enrollment_status != 'Withdrawn')
                  ORDER BY ch.first_name, ch.last_name",
                new { ClassroomId = classroomId },
                cancellationToken: cancellationToken));

        return children.Select(ch => new StudentNodeDto
        {
            ChildId = ch.child_id,
            FullName = ch.full_name,
            PhotoUrl = ch.photo_url,
            ClassroomName = ch.classroom_name
        }).ToList();
    }

    private sealed class ChildRow
    {
        public Guid child_id { get; set; }
        public Guid classroom_id { get; set; }
        public string full_name { get; set; } = string.Empty;
        public string? photo_url { get; set; }
        public string classroom_name { get; set; } = string.Empty;
    }
}
