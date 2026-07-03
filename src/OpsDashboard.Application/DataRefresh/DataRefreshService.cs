using Microsoft.EntityFrameworkCore;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Domain.Entities;
using OpsDashboard.Domain.Enums;

namespace OpsDashboard.Application.DataRefresh;

public sealed class DataRefreshService(
    IOpsDashboardDbContext db,
    IDatasetFileProfiler profiler,
    IBusinessIntelligenceService intelligence,
    IGeneratedDashboardService dashboards) : IDataRefreshService
{
    public async Task<DataRefreshJobDto?> RefreshDatasetAsync(int datasetId, string? userId, string? userEmail, CancellationToken cancellationToken = default)
    {
        var dataset = await db.Datasets.FirstOrDefaultAsync(x => x.Id == datasetId, cancellationToken);
        if (dataset is null)
        {
            return null;
        }

        var job = new DataRefreshJob
        {
            DatasetId = datasetId,
            StartedAtUtc = DateTime.UtcNow,
            Status = DataRefreshStatus.Running,
            TriggeredByUserId = userId,
            TriggeredByEmail = userEmail,
            Message = "Refresh started."
        };

        db.DataRefreshJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            if (!File.Exists(dataset.FilePath))
            {
                throw new FileNotFoundException("Uploaded dataset file was not found.", dataset.FilePath);
            }

            var profile = await profiler.ProfileAsync(dataset.FilePath, dataset.SourceType, cancellationToken);
            dataset.RowCount = profile.RowCount;
            dataset.ColumnCount = profile.ColumnCount;
            dataset.MissingValues = profile.MissingValues;
            dataset.DuplicateRows = profile.DuplicateRows;
            dataset.NumericColumns = profile.NumericColumns;
            dataset.DateColumns = profile.DateColumns;
            dataset.TextColumns = profile.TextColumns;
            dataset.DatasetQualityScore = profile.DatasetQualityScore;
            dataset.Status = DatasetStatus.Profiled;

            await intelligence.RefreshInsightAsync(datasetId, cancellationToken);
            await dashboards.GenerateAsync(datasetId, cancellationToken: cancellationToken);

            job.CompletedAtUtc = DateTime.UtcNow;
            job.Status = DataRefreshStatus.Success;
            job.RecordsProcessed = profile.RowCount;
            job.Message = $"Refresh completed successfully. Processed {profile.RowCount:N0} records.";
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            job.CompletedAtUtc = DateTime.UtcNow;
            job.Status = DataRefreshStatus.Failed;
            job.Message = ex.Message;
            await db.SaveChangesAsync(cancellationToken);
        }

        return ToJobDto(job);
    }

    public async Task<IReadOnlyList<RefreshCenterDatasetDto>> GetRefreshCenterAsync(CancellationToken cancellationToken = default)
    {
        var datasets = await db.Datasets
            .AsNoTracking()
            .Include(x => x.Domain)
            .OrderBy(x => x.DatasetName)
            .ToListAsync(cancellationToken);

        var latestJobs = await db.DataRefreshJobs
            .AsNoTracking()
            .GroupBy(x => x.DatasetId)
            .Select(x => x.OrderByDescending(j => j.StartedAtUtc).First())
            .ToListAsync(cancellationToken);
        var latestByDataset = latestJobs.ToDictionary(x => x.DatasetId);

        return datasets.Select(dataset =>
        {
            latestByDataset.TryGetValue(dataset.Id, out var job);
            return new RefreshCenterDatasetDto(
                dataset.Id,
                dataset.DatasetName,
                dataset.Domain?.Name ?? "Unassigned",
                job?.CompletedAtUtc ?? job?.StartedAtUtc,
                job?.Status,
                job?.RecordsProcessed,
                job?.Message);
        }).ToList();
    }

    public async Task<DatasetRefreshHistoryDto?> GetDatasetRefreshHistoryAsync(int datasetId, CancellationToken cancellationToken = default)
    {
        var dataset = await db.Datasets
            .AsNoTracking()
            .Include(x => x.Domain)
            .FirstOrDefaultAsync(x => x.Id == datasetId, cancellationToken);
        if (dataset is null)
        {
            return null;
        }

        var jobs = await db.DataRefreshJobs
            .AsNoTracking()
            .Where(x => x.DatasetId == datasetId)
            .OrderByDescending(x => x.StartedAtUtc)
            .Select(x => ToJobDto(x))
            .ToListAsync(cancellationToken);

        return new DatasetRefreshHistoryDto(dataset.Id, dataset.DatasetName, dataset.Domain?.Name ?? "Unassigned", jobs);
    }

    private static DataRefreshJobDto ToJobDto(DataRefreshJob job)
    {
        return new DataRefreshJobDto(
            job.Id,
            job.DatasetId,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            job.Status,
            job.RecordsProcessed,
            job.TriggeredByUserId,
            job.TriggeredByEmail,
            job.Message);
    }
}
