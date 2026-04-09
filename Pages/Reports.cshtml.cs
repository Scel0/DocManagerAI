using Microsoft.AspNetCore.Mvc.RazorPages;
using DocManagerAI.Data;
using DocManagerAI.Models;
using DocManagerAI.Services;

namespace DocManagerAI.Pages
{
    public class ReportsModel : PageModel
    {
        private readonly AppDbContext _db;
        public ReportsModel(AppDbContext db) => _db = db;

        public List<Document>  Documents    { get; set; } = new();
        public InsightsSummary Summary      { get; set; } = new();

        // Filter values (retained in form after submit)
        public string FromDate     { get; set; }
        public string ToDate       { get; set; }
        public string VendorFilter { get; set; }
        public string StatusFilter { get; set; }
        public string MinAmount    { get; set; }
        public string MaxAmount    { get; set; }

        public void OnGet(
            DateTime? from,
            DateTime? to,
            string    vendor,
            string    status,
            decimal?  minAmount,
            decimal?  maxAmount)
        {
            // Retain filter values for the form
            FromDate     = from?.ToString("yyyy-MM-dd");
            ToDate       = to?.ToString("yyyy-MM-dd");
            VendorFilter = vendor;
            StatusFilter = status;
            MinAmount    = minAmount?.ToString();
            MaxAmount    = maxAmount?.ToString();

            var query = _db.Documents.AsQueryable();

            // Filter: date range
            if (from.HasValue)
                query = query.Where(d => d.UploadedAt >= from.Value || (d.DocumentDate.HasValue && d.DocumentDate.Value >= from.Value));

            if (to.HasValue)
                query = query.Where(d => d.UploadedAt <= to.Value || (d.DocumentDate.HasValue && d.DocumentDate.Value <= to.Value));

            // Filter: vendor (partial match)
            if (!string.IsNullOrWhiteSpace(vendor))
                query = query.Where(d => d.Vendor.Contains(vendor));

            // Filter: status
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(d => d.Status == status);

            // Filter: amount range
            if (minAmount.HasValue)
                query = query.Where(d => d.Amount >= minAmount.Value);

            if (maxAmount.HasValue)
                query = query.Where(d => d.Amount <= maxAmount.Value);

            Documents = query.OrderByDescending(d => d.UploadedAt).ToList();

            // Generate AI insights from the filtered set
            Summary = InsightsService.Generate(Documents);
        }
    }
}
