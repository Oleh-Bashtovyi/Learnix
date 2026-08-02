using System.Reflection;
using Learnix.Infrastructure.Storage;

namespace Learnix.Infrastructure.UnitTests.Storage;

/// <summary>
/// <see cref="BlobContainers.Access"/> is load-bearing, not documentation (see the class remarks): a
/// container missing from it, or wrongly marked, is the gap between "this SAS has an expiry" and "this
/// container answers anonymous reads anyway".
/// </summary>
public class BlobContainersTests
{
    [Theory]
    [InlineData(BlobContainers.Avatars)]
    [InlineData(BlobContainers.CourseCovers)]
    [InlineData(BlobContainers.CategoryImages)]
    public void IsPublic_ReturnsTrue_ForContainersProvisionedForAnonymousRead(string container)
        => BlobContainers.IsPublic(container).Should().BeTrue();

    [Theory]
    [InlineData(BlobContainers.CourseVideos)]
    [InlineData(BlobContainers.Certificates)]
    [InlineData(BlobContainers.Temp)]
    public void IsPublic_ReturnsFalse_ForContainersThatRequireASas(string container)
        => BlobContainers.IsPublic(container).Should().BeFalse();

    [Fact]
    public void IsPublic_ReturnsFalse_ForAContainerThatIsNotDeclaredAtAll()
        // A typo'd or renamed container must fail closed to a SAS requirement, not fail open to a public URL.
        => BlobContainers.IsPublic("not-a-real-container").Should().BeFalse();

    [Fact]
    public void Access_HasExactlyOneEntryPerDeclaredContainerConstant()
    {
        // Guards the C# side of what npm run check:containers guards against Terraform: every container
        // constant must be in Access, and Access must not carry an entry for a container nobody declared.
        var declaredContainers = typeof(BlobContainers)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        BlobContainers.All.Should().BeEquivalentTo(declaredContainers);
    }
}
