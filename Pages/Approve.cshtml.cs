using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocManagerAI.Data;
using DocManagerAI.Models;
using DocManagerAI.Services;

namespace DocManagerAI.Pages
{
    public class ApproveModel : PageModel
    {
        private readonly AppDbContext _db;
        public ApproveModel(AppDbContext db) => _db = db;

        public List<Document> PendingDocuments   { get; set; } = new();
        public List<Document> CompletedDocuments { get; set; } = new();
        public string         StatusMessage      { get; set; }
        public bool           IsError            { get; set; }

        public void OnGet()
        {
            LoadDocuments();
        }

        public IActionResult OnPost(int Id, string action)
        {
            var role = HttpContext.Session.GetString("Role");
            var user = HttpContext.Session.GetString("User");

            if (string.IsNullOrEmpty(role))
                return RedirectToPage("/Login");

            var doc = _db.Documents.Find(Id);
            if (doc == null)
            {
                StatusMessage = "Document not found.";
                IsError = true;
                LoadDocuments();
                return Page();
            }

            if (action == "reject")
            {
                // Any role can reject at any step
                doc.Status = "Rejected";
                StatusMessage = $"Document #{doc.Id} ({doc.Vendor} — {doc.InvoiceNumber}) has been rejected.";
                IsError = false;

                _db.ApprovalHistories.Add(new ApprovalHistory
                {
                    DocumentId = doc.Id,
                    ActionBy   = user,
                    Role       = role,
                    Step       = doc.ApprovalStep,
                    Action     = "Rejected",
                    Comment    = $"Rejected at Step {doc.ApprovalStep} by {role}.",
                    Timestamp  = DateTime.Now
                });
            }
            else if (action == "approve")
            {
                // Check if this role is allowed to act at this step
                if (!AuthService.CanApproveAtStep(role, doc.ApprovalStep))
                {
                    StatusMessage = $"Your role ({role}) cannot approve at Step {doc.ApprovalStep}. " +
                                    $"This step requires: {AuthService.GetStepLabel(doc.ApprovalStep)}.";
                    IsError = true;
                    LoadDocuments();
                    return Page();
                }

                var previousStep = doc.ApprovalStep;

                if (doc.ApprovalStep < 3)
                {
                    // Move to next step
                    doc.ApprovalStep++;
                    StatusMessage = $"Document #{doc.Id} approved at Step {previousStep}. " +
                                    $"Now awaiting: {AuthService.GetStepLabel(doc.ApprovalStep)}.";
                }
                else
                {
                    // Step 3 approved = fully approved
                    doc.Status = "Approved";
                    StatusMessage = $"Document #{doc.Id} ({doc.Vendor} — {doc.InvoiceNumber}) " +
                                    $"has been fully approved through all 3 steps!";
                }

                IsError = false;

                _db.ApprovalHistories.Add(new ApprovalHistory
                {
                    DocumentId = doc.Id,
                    ActionBy   = user,
                    Role       = role,
                    Step       = previousStep,
                    Action     = "Approved",
                    Comment    = $"Approved at Step {previousStep} by {role}.",
                    Timestamp  = DateTime.Now
                });
            }

            _db.SaveChanges();
            LoadDocuments();
            return Page();
        }

        private void LoadDocuments()
        {
            PendingDocuments = _db.Documents
                .Where(d => d.Status == "Pending")
                .OrderBy(d => d.UploadedAt)
                .ToList();

            CompletedDocuments = _db.Documents
                .Where(d => d.Status == "Approved" || d.Status == "Rejected")
                .OrderByDescending(d => d.UploadedAt)
                .ToList();
        }
    }
}
