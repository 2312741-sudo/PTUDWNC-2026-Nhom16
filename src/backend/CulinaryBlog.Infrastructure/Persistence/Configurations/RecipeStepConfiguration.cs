using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public sealed class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStep>
{
    public void Configure(EntityTypeBuilder<RecipeStep> b)
    {
        b.ToTable("RecipeSteps");
        b.HasKey(s => s.Id);

        b.Property(s => s.StepNumber).IsRequired();
        b.Property(s => s.Title).HasMaxLength(200).IsRequired();        // D16
        b.Property(s => s.Description).HasMaxLength(2000).IsRequired();
        b.Property(s => s.TimerMinutes);                               // nullable, >=0
        b.Property(s => s.ImageUrl).HasMaxLength(500);
        b.Property(s => s.RowVersion).HasColumnType("bytea").IsConcurrencyToken().IsRequired();

        // Unique (RecipeId, StepNumber) - đảm bảo liên tục 1..N + chống race khi renumber
        b.HasIndex(s => new { s.RecipeId, s.StepNumber }).IsUnique();
    }
}
