namespace OpsDashboard.Application.Kpis;

public static class KpiThresholds
{
    public static KpiStatus AverageTat(decimal hours) => hours < 20m ? KpiStatus.Green : hours <= 24m ? KpiStatus.Amber : KpiStatus.Red;
    public static KpiStatus SlaBreachRate(decimal percent) => percent < 5m ? KpiStatus.Green : percent <= 10m ? KpiStatus.Amber : KpiStatus.Red;
    public static KpiStatus Productivity(decimal casesPerDay) => casesPerDay >= 45m ? KpiStatus.Green : casesPerDay >= 35m ? KpiStatus.Amber : KpiStatus.Red;
    public static KpiStatus BacklogGrowth(decimal weeklyPercent) => weeklyPercent <= 0m ? KpiStatus.Green : weeklyPercent < 5m ? KpiStatus.Amber : KpiStatus.Red;
    public static KpiStatus Fcr(decimal percent) => percent >= 80m ? KpiStatus.Green : percent >= 65m ? KpiStatus.Amber : KpiStatus.Red;
}
