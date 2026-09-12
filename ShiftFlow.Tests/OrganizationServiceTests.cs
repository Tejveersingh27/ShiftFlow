using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Models.Entities;
using ShiftFlow.Services;

namespace ShiftFlow.Tests;

public class OrganizationServiceTests
{
    // A fresh, throwaway in-memory database for one test. A new Guid name
    // each time means tests never share state or interfere with each other.
    private static ApplicationDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateOrganizationAsync_MakesCreatorAnOwner()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var service = new OrganizationService(db);

        // Act
        var organization = await service.CreateOrganizationAsync("Joe's Coffee", "user-1");

        // Assert
        var member = db.OrganizationMembers.Single();
        Assert.Equal(organization.Id, member.OrganizationId);
        Assert.Equal("user-1", member.UserId);
        Assert.Equal(OrganizationRole.Owner, member.Role);
    }

    [Fact]
    public async Task CreateOrganizationAsync_SetsTheNameAndDraftlikeDefaults()
    {
        var db = CreateInMemoryDb();
        var service = new OrganizationService(db);

        var organization = await service.CreateOrganizationAsync("Joe's Coffee", "user-1");

        Assert.Equal("Joe's Coffee", organization.Name);
        Assert.NotEqual(Guid.Empty, organization.Id);
    }
}
