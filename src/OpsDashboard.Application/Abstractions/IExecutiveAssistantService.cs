using OpsDashboard.Application.Analytics;
using OpsDashboard.Application.Assistant;

namespace OpsDashboard.Application.Abstractions;

public interface IExecutiveAssistantService
{
    Task<AssistantPageDto?> GetAssistantAsync(int datasetId, AnalyticsFilterDto? filters = null, CancellationToken cancellationToken = default);
    Task<AssistantExchangeDto?> AskAsync(AssistantQuestionRequest request, CancellationToken cancellationToken = default);
}
