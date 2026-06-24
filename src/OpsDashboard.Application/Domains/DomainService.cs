using Microsoft.EntityFrameworkCore;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Domain.Enums;

namespace OpsDashboard.Application.Domains;

public sealed class DomainService(IOpsDashboardDbContext db) : IDomainService
{
    public async Task<IReadOnlyList<DomainListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Domains
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new DomainListItemDto(
                x.Id,
                x.Name,
                x.Description,
                x.Status,
                x.CreatedDate,
                x.Datasets.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<DomainDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await db.Domains
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new DomainDetailsDto(
                x.Id,
                x.Name,
                x.Description,
                x.Status,
                x.CreatedDate,
                x.Datasets
                    .OrderByDescending(d => d.UploadDate)
                    .Select(d => new DomainDatasetDto(
                        d.Id,
                        d.DatasetName,
                        d.SourceType,
                        d.UploadDate,
                        d.RowCount,
                        d.ColumnCount,
                        d.DatasetQualityScore))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(DomainFormDto input, CancellationToken cancellationToken = default)
    {
        var domain = new OpsDashboard.Domain.Entities.Domain
        {
            Name = input.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            Status = DomainStatus.Active,
            CreatedDate = DateTime.UtcNow
        };

        db.Domains.Add(domain);
        await db.SaveChangesAsync(cancellationToken);
        return domain.Id;
    }

    public async Task<bool> UpdateAsync(int id, DomainFormDto input, CancellationToken cancellationToken = default)
    {
        var domain = await db.Domains.FindAsync([id], cancellationToken);
        if (domain is null)
        {
            return false;
        }

        domain.Name = input.Name.Trim();
        domain.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> ArchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        return SetStatusAsync(id, DomainStatus.Archived, cancellationToken);
    }

    public Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        return SetStatusAsync(id, DomainStatus.Active, cancellationToken);
    }

    private async Task<bool> SetStatusAsync(int id, DomainStatus status, CancellationToken cancellationToken)
    {
        var domain = await db.Domains.FindAsync([id], cancellationToken);
        if (domain is null)
        {
            return false;
        }

        domain.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
