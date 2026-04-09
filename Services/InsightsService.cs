using DocManagerAI.Data;
using DocManagerAI.Models;

namespace DocManagerAI.Services
{
    public class InsightItem
    {
        public string Type    { get; set; }  // "info" | "warning" | "success"
        public string Title   { get; set; }
        public string Detail  { get; set; }
    }

    public class VendorSpend
    {
        public string  Vendor { get; set; }
        public decimal Total  { get; set; }
        public int     Count  { get; set; }
    }

    public class MonthlySpend
    {
        public string  Month  { get; set; }
        public decimal Total  { get; set; }
        public int     Count  { get; set; }
    }

    public class InsightsSummary
    {
        public decimal            TotalSpend       { get; set; }
        public decimal            TotalVAT         { get; set; }
        public decimal            AverageInvoice   { get; set; }
        public string             TopVendor        { get; set; }
        public int                ApprovedCount    { get; set; }
        public int                PendingCount     { get; set; }
        public int                RejectedCount    { get; set; }
        public List<InsightItem>  Insights         { get; set; } = new();
        public List<VendorSpend>  TopVendors       { get; set; } = new();
        public List<MonthlySpend> MonthlyTrend     { get; set; } = new();
    }

    public static class InsightsService
    {
        public static InsightsSummary Generate(List<Document> docs)
        {
            var summary = new InsightsSummary();
            if (!docs.Any()) return summary;

            var approved = docs.Where(d => d.Status == "Approved").ToList();
            var pending  = docs.Where(d => d.Status == "Pending").ToList();
            var rejected = docs.Where(d => d.Status == "Rejected").ToList();

            summary.TotalSpend     = docs.Sum(d => d.Amount);
            summary.TotalVAT       = docs.Sum(d => d.VAT);
            summary.AverageInvoice = docs.Any() ? docs.Average(d => d.Amount) : 0;
            summary.ApprovedCount  = approved.Count;
            summary.PendingCount   = pending.Count;
            summary.RejectedCount  = rejected.Count;

            // Top vendors by total spend
            summary.TopVendors = docs
                .GroupBy(d => d.Vendor)
                .Select(g => new VendorSpend
                {
                    Vendor = g.Key,
                    Total  = g.Sum(x => x.Amount),
                    Count  = g.Count()
                })
                .OrderByDescending(v => v.Total)
                .Take(5)
                .ToList();

            summary.TopVendor = summary.TopVendors.FirstOrDefault()?.Vendor ?? "N/A";

            // Monthly spend trend (last 6 months)
            summary.MonthlyTrend = docs
                .Where(d => d.DocumentDate.HasValue)
                .GroupBy(d => d.DocumentDate.Value.ToString("MMM yyyy"))
                .Select(g => new MonthlySpend
                {
                    Month = g.Key,
                    Total = g.Sum(x => x.Amount),
                    Count = g.Count()
                })
                .TakeLast(6)
                .ToList();

            // ── Generate insight cards ────────────────────────────────────────────

            // 1. Approval rate
            if (docs.Count > 0)
            {
                double approvalRate = (double)approved.Count / docs.Count * 100;
                summary.Insights.Add(new InsightItem
                {
                    Type   = approvalRate >= 70 ? "success" : "warning",
                    Title  = $"Approval Rate: {approvalRate:F0}%",
                    Detail = $"{approved.Count} approved out of {docs.Count} total documents."
                });
            }

            // 2. Anomaly detection: invoices more than 2x the average
            if (summary.AverageInvoice > 0)
            {
                var anomalies = docs
                    .Where(d => d.Amount > summary.AverageInvoice * 2 && d.Amount > 0)
                    .ToList();

                if (anomalies.Any())
                {
                    summary.Insights.Add(new InsightItem
                    {
                        Type   = "warning",
                        Title  = $"{anomalies.Count} High-Value Invoice{(anomalies.Count > 1 ? "s" : "")} Detected",
                        Detail = $"Documents from: {string.Join(", ", anomalies.Select(a => a.Vendor).Distinct())} " +
                                 $"exceed 2× the average invoice value of R{summary.AverageInvoice:N2}."
                    });
                }
            }

            // 3. Pending bottleneck warning
            if (pending.Count > 5)
            {
                summary.Insights.Add(new InsightItem
                {
                    Type   = "warning",
                    Title  = $"Approval Backlog: {pending.Count} Pending Documents",
                    Detail = $"There are {pending.Count} documents waiting for approval. " +
                             $"Oldest pending: {pending.Min(d => d.UploadedAt):dd MMM yyyy}."
                });
            }

            // 4. Top vendor spend concentration
            if (summary.TopVendors.Any() && summary.TotalSpend > 0)
            {
                var top      = summary.TopVendors.First();
                double share = (double)top.Total / (double)summary.TotalSpend * 100;
                if (share > 50)
                {
                    summary.Insights.Add(new InsightItem
                    {
                        Type   = "info",
                        Title  = $"High Vendor Concentration: {top.Vendor}",
                        Detail = $"{top.Vendor} accounts for {share:F0}% of total spend " +
                                 $"(R{top.Total:N2} across {top.Count} invoices)."
                    });
                }
            }

            // 5. Rejected invoice insight
            if (rejected.Count > 0)
            {
                summary.Insights.Add(new InsightItem
                {
                    Type   = "warning",
                    Title  = $"{rejected.Count} Rejected Document{(rejected.Count > 1 ? "s" : "")}",
                    Detail = $"Vendors with rejected invoices: " +
                             $"{string.Join(", ", rejected.Select(r => r.Vendor).Distinct())}."
                });
            }

            // 6. VAT insight
            if (summary.TotalVAT > 0 && summary.TotalSpend > 0)
            {
                double vatRate = (double)summary.TotalVAT / (double)summary.TotalSpend * 100;
                summary.Insights.Add(new InsightItem
                {
                    Type   = "info",
                    Title  = $"Total VAT: R{summary.TotalVAT:N2}",
                    Detail = $"VAT represents {vatRate:F1}% of total spend. " +
                             $"Net spend (excl. VAT): R{(summary.TotalSpend - summary.TotalVAT):N2}."
                });
            }

            // 7. Good standing message if all clear
            if (!summary.Insights.Any(i => i.Type == "warning"))
            {
                summary.Insights.Add(new InsightItem
                {
                    Type   = "success",
                    Title  = "All Clear",
                    Detail = "No anomalies or bottlenecks detected in the current dataset."
                });
            }

            return summary;
        }
    }
}
