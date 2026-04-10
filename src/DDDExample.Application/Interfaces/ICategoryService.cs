using DDDExample.Application.DTOs;

namespace DDDExample.Application.Interfaces;

public interface ICategoryService
{
    Task<CategoryDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(string id, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
    Task ToggleStatusAsync(string id, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
