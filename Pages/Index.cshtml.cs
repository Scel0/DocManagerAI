using Microsoft.AspNetCore.Mvc.RazorPages;
using DocManagerAI.Data;
using DocManagerAI.Models;

namespace DocManagerAI.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        public IndexModel(AppDbContext db) => _db = db;

        public int            TotalDocs    { get; set; }
        public int            PendingDocs  { get; set; }
        public int            ApprovedDocs { get; set; }
        public int            RejectedDocs { get; set; }
        public decimal        TotalSpend   { get; set; }
        public string         LoggedInUser { get; set; }
        public string         UserRole     { get; set; }
        public List<Document> RecentDocs   { get; set; } = new();

        public void OnGet()
        {
            TotalDocs    = _db.Documents.Count();
            PendingDocs  = _db.Documents.Count(d => d.Status == "Pending");
            ApprovedDocs = _db.Documents.Count(d => d.Status == "Approved");
            RejectedDocs = _db.Documents.Count(d => d.Status == "Rejected");

            // SQLite cannot apply Sum() on decimal columns via EF translation.
            // AsEnumerable() pulls the filtered rows into memory first so
            // the Sum runs as C# (LINQ to Objects) instead of as SQL.
            TotalSpend = _db.Documents
                .Where(d => d.Status == "Approved")
                .AsEnumerable()
                .Sum(d => d.Amount);

            LoggedInUser = HttpContext.Session.GetString("User");
            UserRole     = HttpContext.Session.GetString("Role");
            RecentDocs   = _db.Documents.OrderByDescending(d => d.UploadedAt).Take(5).ToList();
        }
    }
}
