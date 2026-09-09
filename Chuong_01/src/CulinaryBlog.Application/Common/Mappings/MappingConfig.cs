using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using Mapster;
namespace CulinaryBlog.Application.Common.Mappings;
public sealed class MappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Category, CategoryDto>();
        config.NewConfig<Recipe, RecipeDto>();
        config.NewConfig<Recipe, RecipeDetailDto>().Map(d => d.Category, s => s.Category);
    }
}
