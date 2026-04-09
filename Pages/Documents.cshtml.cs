using Microsoft.AspNetCore.Mvc.RazorPages;
using DocManagerAI.Data;
using DocManagerAI.Models;

namespace DocManagerAI.Pages
{
    public class DocumentsModel : PageModel
    {
        private readonly AppDbContext _db;
        public DocumentsModel(AppDbContext db) => _db = db;

        public List<Document> Documents   { get; set; } = new();
        public string         Search      { get; set; }
        public string         StatusFilter  { get; set; }
        public string         DocTypeFilter { get; set; }

        public void OnGet(string search, string status, string doctype)
        {
            Search        = search;
            StatusFilter  = status;
            DocTypeFilter = doctype;

            var query = _db.Documents.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(d =>
                    d.Vendor.Contains(search) ||
                    (d.InvoiceNumber != null && d.InvoiceNumber.Contains(search)) ||
                    d.FileName.Contains(search));

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(d => d.Status == status);

            if (!string.IsNullOrWhiteSpace(doctype))
                query = query.Where(d => d.DocumentType == doctype);

            Documents = query.OrderByDescending(d => d.UploadedAt).ToList();
        }
    }
}
