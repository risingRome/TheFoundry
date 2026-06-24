using Microsoft.EntityFrameworkCore;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Domain.Enums;

namespace OpsDashboard.Application.Datasets;

public sealed class DatasetService(IOpsDashboardDbContext db, IDatasetFileProfiler profiler) : IDatasetService
{
    public async Task<IReadOnlyList<DatasetListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Datasets
            .AsNoTracking()
            .OrderByDescending(x => x.UploadDate)
            .Select(x => new DatasetListItemDto(
                x.Id,
                x.DomainId,
                x.Domain!.Name,
                x.DatasetName,
                x.SourceType,
                x.UploadDate,
                x.Status,
                x.RowCount,
                x.ColumnCount,
                x.DatasetQualityScore))
            .ToListAsync(cancellationToken);
    }

    public async Task<DatasetDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await db.Datasets
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new DatasetDetailsDto(
                x.Id,
                x.DomainId,
                x.Domain!.Name,
                x.DatasetName,
                x.SourceType,
                x.FilePath,
                x.UploadDate,
                x.Status,
                x.RowCount,
                x.ColumnCount,
                x.MissingValues,
                x.DuplicateRows,
                x.NumericColumns,
                x.DateColumns,
                x.TextColumns,
                x.DatasetQualityScore))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> DatasetNameExistsAsync(string datasetName, CancellationToken cancellationToken = default)
    {
        return db.Datasets.AnyAsync(x => x.DatasetName == datasetName, cancellationToken);
    }

    public async Task<int> RegisterUploadAsync(DatasetUploadRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await profiler.ProfileAsync(request.FilePath, request.SourceType, cancellationToken);
        var dataset = new OpsDashboard.Domain.Entities.Dataset
        {
            DomainId = request.DomainId,
            DatasetName = request.DatasetName.Trim(),
            SourceType = request.SourceType,
            FilePath = request.FilePath,
            UploadDate = DateTime.UtcNow,
            Status = DatasetStatus.Profiled,
            RowCount = profile.RowCount,
            ColumnCount = profile.ColumnCount,
            MissingValues = profile.MissingValues,
            DuplicateRows = profile.DuplicateRows,
            NumericColumns = profile.NumericColumns,
            DateColumns = profile.DateColumns,
            TextColumns = profile.TextColumns,
            DatasetQualityScore = profile.DatasetQualityScore
        };

        db.Datasets.Add(dataset);
        await db.SaveChangesAsync(cancellationToken);
        return dataset.Id;
    }
}
