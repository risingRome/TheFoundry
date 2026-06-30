using OpsDashboard.Application.Analytics;

namespace OpsDashboard.Application.Assistant;

public sealed record AssistantPageDto(
    int DatasetId,
    string DatasetName,
    string DomainName,
    AnalyticsFilterStateDto Filters,
    IReadOnlyList<AssistantExchangeDto> Conversations,
    IReadOnlyList<string> SuggestedQuestions);

public sealed record AssistantExchangeDto(
    int Id,
    string UserMessage,
    string AssistantResponse,
    DateTime CreatedAt,
    string? UserId);

public sealed record AssistantQuestionRequest(
    int DatasetId,
    string UserMessage,
    string? UserId,
    AnalyticsFilterDto Filters);
