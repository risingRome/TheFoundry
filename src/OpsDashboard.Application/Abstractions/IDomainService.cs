using OpsDashboard.Application.Domains;

namespace OpsDashboard.Application.Abstractions;

public interface IDomainService
{
    Task<IReadOnlyList<DomainListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DomainDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(DomainFormDto input, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, DomainFormDto input, CancellationToken cancellationToken = default);
    Task<bool> ArchiveAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
}
