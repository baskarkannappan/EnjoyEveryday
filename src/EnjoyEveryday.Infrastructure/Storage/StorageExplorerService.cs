using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Application.Services.Storage;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Storage;

public class StorageExplorerService : IStorageExplorerService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StorageExplorerService> _logger;

    public StorageExplorerService(
        IDbConnectionFactory connectionFactory,
        IConfiguration configuration,
        ILogger<StorageExplorerService> logger)
    {
        _connectionFactory = connectionFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<StorageExplorerModel> GetStorageExplorerDataAsync(Guid? tenantId = null, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = """
            SELECT 
                m.id,
                m.entity_type AS EntityType,
                m.entity_id AS EntityId,
                m.child_id AS ChildId,
                m.display_key AS DisplayKey,
                m.display_container AS DisplayContainer,
                m.display_content_type AS ContentType,
                m.display_size_bytes AS SizeBytes,
                m.created_at AS CreatedAt,
                COALESCE(cl.name, child_cl.name, teacher_cl.name, 'Center-wide') AS ClassroomName,
                COALESCE(cl_b.name, child_b.name, ev_b.name, teacher_b.name, 'Main Campus') AS BranchName
            FROM media_assets m
            LEFT JOIN classrooms cl ON m.entity_type = 'Classroom' AND m.entity_id = cl.id
            LEFT JOIN branches cl_b ON cl.branch_id = cl_b.id
            LEFT JOIN children ch ON m.entity_type = 'Child' AND (m.entity_id = ch.id OR m.child_id = ch.id)
            LEFT JOIN classrooms child_cl ON ch.classroom_id = child_cl.id
            LEFT JOIN branches child_b ON child_cl.branch_id = child_b.id
            LEFT JOIN events ev ON m.entity_type = 'Event' AND m.entity_id = ev.id
            LEFT JOIN branches ev_b ON ev.branch_id = ev_b.id
            LEFT JOIN classroom_teachers ct ON m.entity_type = 'Teacher' AND m.entity_id = ct.teacher_id AND ct.is_active = true
            LEFT JOIN classrooms teacher_cl ON ct.classroom_id = teacher_cl.id
            LEFT JOIN branches teacher_b ON teacher_cl.branch_id = teacher_b.id
            WHERE (@TenantId IS NULL OR m.tenant_id = @TenantId)
            ORDER BY m.created_at DESC;
            """;

        var rawRows = (await connection.QueryAsync<RawStorageRecord>(sql, new { TenantId = tenantId })).ToList();

        // Determine current storage environment
        var azureConn = _configuration["AzureBlob:ConnectionString"] ?? Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        var isCloud = !string.IsNullOrWhiteSpace(azureConn);
        var providerName = isCloud ? "Azure Blob Storage" : "Local Disk";
        var basePath = _configuration["Storage:BasePath"] ?? "storage";
        var absoluteBasePath = isCloud ? "azure://enjoyeveryday-media" : Path.GetFullPath(basePath);

        var model = new StorageExplorerModel();
        model.Summary.ActiveProvider = providerName;
        model.Summary.StorageLocation = absoluteBasePath;
        model.Summary.TotalFiles = rawRows.Count;
        model.Summary.TotalSizeBytes = rawRows.Sum(r => r.SizeBytes);

        if (isCloud)
        {
            model.Summary.CloudFilesCount = rawRows.Count;
            model.Summary.LocalFilesCount = 0;
        }
        else
        {
            model.Summary.LocalFilesCount = rawRows.Count;
            model.Summary.CloudFilesCount = 0;
        }

        // Entity breakdown
        foreach (var group in rawRows.GroupBy(r => string.IsNullOrWhiteSpace(r.EntityType) ? "General" : r.EntityType))
        {
            model.Summary.EntityCounts[group.Key] = group.Count();
            model.Summary.EntitySizeMb[group.Key] = Math.Round((double)group.Sum(r => r.SizeBytes) / (1024 * 1024), 2);
        }

        // Build file records
        foreach (var r in rawRows)
        {
            var physicalOrCloud = isCloud
                ? $"https://enjoyeverydaystorage.blob.core.windows.net/{r.DisplayContainer}/{r.DisplayKey}"
                : Path.Combine(absoluteBasePath, r.DisplayContainer, r.DisplayKey.Replace('/', Path.DirectorySeparatorChar));

            model.Files.Add(new StorageFileRecordDto
            {
                Id = r.Id,
                EntityType = r.EntityType ?? "General",
                BranchName = r.BranchName ?? "Main Campus",
                ClassroomName = r.ClassroomName ?? "Center-wide",
                DisplayKey = r.DisplayKey,
                DisplayContainer = r.DisplayContainer,
                ContentType = r.ContentType,
                SizeBytes = r.SizeBytes,
                StorageProvider = providerName,
                PhysicalOrCloudPath = physicalOrCloud,
                DisplayUrl = $"/api/media/{r.Id}/display",
                CreatedAt = r.CreatedAt
            });
        }

        // Build Branch -> Classroom Hierarchy Rollups
        var branchGroups = model.Files.GroupBy(f => f.BranchName);
        foreach (var bGroup in branchGroups)
        {
            var branchRollup = new BranchStorageRollupDto
            {
                BranchName = bGroup.Key,
                TotalFiles = bGroup.Count(),
                TotalSizeBytes = bGroup.Sum(f => f.SizeBytes)
            };

            var classGroups = bGroup.GroupBy(f => f.ClassroomName);
            foreach (var cGroup in classGroups)
            {
                var classRollup = new ClassroomStorageRollupDto
                {
                    ClassroomName = cGroup.Key,
                    TotalFiles = cGroup.Count(),
                    TotalSizeBytes = cGroup.Sum(f => f.SizeBytes)
                };

                foreach (var typeGroup in cGroup.GroupBy(f => f.EntityType))
                {
                    classRollup.TypeCounts[typeGroup.Key] = typeGroup.Count();
                }

                branchRollup.Classrooms.Add(classRollup);
            }

            model.Hierarchy.Add(branchRollup);
        }

        return model;
    }

    private class RawStorageRecord
    {
        public Guid Id { get; set; }
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public Guid? ChildId { get; set; }
        public string DisplayKey { get; set; } = string.Empty;
        public string DisplayContainer { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? ClassroomName { get; set; }
        public string? BranchName { get; set; }
    }
}

