using OpsDashboard.Application.Reports;

namespace OpsDashboard.Web.Services;

public interface IReportExportService
{
    byte[] ExportPdf(ExecutiveReportDto report);
    byte[] ExportDocx(ExecutiveReportDto report);
}
