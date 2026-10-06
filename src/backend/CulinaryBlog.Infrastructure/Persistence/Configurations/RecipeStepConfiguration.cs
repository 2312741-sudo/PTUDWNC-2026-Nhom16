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
        // Id sinh o domain (Guid.NewGuid) -> EF phai INSERT entity con moi, khong phai UPDATE
        b.Property(s => s.Id).ValueGeneratedNever();
        b.Property(s => s.RowVersion).HasColumnType("bytea").IsConcurrencyToken().IsRequired();

        // Unique (RecipeId, StepNumber) - đảm bảo liên tục 1..N + chống race khi renumber.
        // Partial: bước đã xoá mềm vẫn giữ StepNumber cũ nên không được chiếm chỗ trong index.
        b.HasIndex(s => new { s.RecipeId, s.StepNumber }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}
