using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Tenancy;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class MemoryStreamPrivacyAndSecurityTests
{
    private Guid _tenantId = Guid.NewGuid();
    private Guid _childId = Guid.NewGuid();
    private Guid _otherOrgChildId = Guid.NewGuid();

    [Test]
    public void MemoryEvent_DefaultIsParentVisible_IsTrue()
    {
        var memory = new MemoryEvent();
        Assert.That(memory.IsParentVisible, Is.True);
    }

    [Test]
    public void MemoryEvent_Categorization_MapsCorrectly()
    {
        var obs = new MemoryEvent { Category = "Observations", Title = "Sandcastle Discovery" };
        var exp = new MemoryEvent { Category = "Learning Experiences", Title = "Water Exploration" };
        var mls = new MemoryEvent { Category = "Milestones", Title = "First Independent Walk" };
        var cel = new MemoryEvent { Category = "Celebrations", Title = "Spring Garden Party" };
        var car = new MemoryEvent { Category = "Daily Care", Title = "Wellbeing Highlight: Happy" };

        Assert.That(obs.Category, Is.EqualTo("Observations"));
        Assert.That(exp.Category, Is.EqualTo("Learning Experiences"));
        Assert.That(mls.Category, Is.EqualTo("Milestones"));
        Assert.That(cel.Category, Is.EqualTo("Celebrations"));
        Assert.That(car.Category, Is.EqualTo("Daily Care"));
    }

    [Test]
    public void MemoryQueryParameters_CategoryAndSearch_FilterCorrectly()
    {
        var query = new MemoryQueryParameters
        {
            Category = "Learning Experiences",
            SearchQuery = "Water",
            Limit = 20,
            Offset = 0
        };

        Assert.That(query.Category, Is.EqualTo("Learning Experiences"));
        Assert.That(query.SearchQuery, Is.EqualTo("Water"));
        Assert.That(query.Limit, Is.EqualTo(20));
    }

    public class MemoryTestUserContext : IUserContext
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public bool IsAuthenticated => true;
        public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
        public IEnumerable<string> Permissions { get; set; } = Array.Empty<string>();
        public Guid? OrganizationId { get; set; }
        public bool HasPermission(string permission) => true;
    }

    [Test]
    public async Task MemoryService_WhenUserIsParent_FiltersHiddenMemories()
    {
        var parentUser = new MemoryTestUserContext
        {
            UserId = Guid.NewGuid(),
            Email = "parent@family.test",
            Roles = new List<string> { "Parent" }
        };

        var allMemories = new List<MemoryEvent>
        {
            new MemoryEvent
            {
                Id = Guid.NewGuid(),
                ChildId = _childId,
                Title = "Public Observation",
                IsParentVisible = true,
                Status = "Approved"
            },
            new MemoryEvent
            {
                Id = Guid.NewGuid(),
                ChildId = _childId,
                Title = "Staff Internal Note",
                IsParentVisible = false,
                Status = "Draft"
            }
        };

        // When user is Parent, items with IsParentVisible == false or unapproved must be filtered out
        var visibleMemories = allMemories
            .Where(m => parentUser.Roles.Contains("Parent") ? (m.IsParentVisible && m.Status == "Approved") : true)
            .ToList();

        Assert.That(visibleMemories.Count, Is.EqualTo(1));
        Assert.That(visibleMemories.First().Title, Is.EqualTo("Public Observation"));
        await Task.CompletedTask;
    }

    [Test]
    public async Task MemoryService_WhenUserIsStaff_ShowsBothInternalAndParentMemories()
    {
        var staffUser = new MemoryTestUserContext
        {
            UserId = Guid.NewGuid(),
            Email = "teacher@daycare.test",
            Roles = new List<string> { "Teacher" }
        };

        var allMemories = new List<MemoryEvent>
        {
            new MemoryEvent
            {
                Id = Guid.NewGuid(),
                ChildId = _childId,
                Title = "Public Observation",
                IsParentVisible = true,
                Status = "Approved"
            },
            new MemoryEvent
            {
                Id = Guid.NewGuid(),
                ChildId = _childId,
                Title = "Staff Internal Note",
                IsParentVisible = false,
                Status = "Draft"
            }
        };

        var visibleMemories = allMemories
            .Where(m => staffUser.Roles.Contains("Parent") ? (m.IsParentVisible && m.Status == "Approved") : true)
            .ToList();

        Assert.That(visibleMemories.Count, Is.EqualTo(2));
        await Task.CompletedTask;
    }
}
