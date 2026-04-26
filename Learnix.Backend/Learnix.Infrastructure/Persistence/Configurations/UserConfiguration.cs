using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FirstName)
            .HasMaxLength(UserConstants.FirstNameMaxLength)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasMaxLength(UserConstants.LastNameMaxLength)
            .IsRequired();

        builder.Property(u => u.Bio)
            .HasMaxLength(UserConstants.BioMaxLength);

        builder.Property(u => u.AvatarBlobPath)
            .HasMaxLength(UserConstants.AvatarUrlMaxLength);

        builder.Property(u => u.GoogleId)
            .HasMaxLength(UserConstants.GoogleIdMaxLength);

        builder.HasIndex(u => u.GoogleId)
            .IsUnique()
            .HasFilter($"\"{nameof(User.GoogleId)}\" IS NOT NULL");
    }
}
