using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocManagerAI.Data;
using DocManagerAI.Models;
using DocManagerAI.Services;

namespace DocManagerAI.Pages
{
    public class UploadModel : PageModel
    {
        private readonly AppDbContext _db;
        public UploadModel(AppDbContext db) => _db = db;

        [BindProperty]
        public IFormFile UploadedFile { get; set; }

        public string         ErrorMessage  { get; set; }
        public ExtractionResult SuccessResult { get; set; }

        // Allowed file types - PDF and DOCX only
        private static readonly string[] AllowedExtensions = { ".pdf", ".docx" };
        private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

        public async Task<IActionResult> OnPostAsync()
        {
            // ── Auth check ───────────────────────────────────────────────────────
            var role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role))
                return RedirectToPage("/Login");

            if (!AuthService.CanUpload(role))
            {
                ErrorMessage = "Your role does not have permission to upload documents.";
                return Page();
            }

            // ── File validation ──────────────────────────────────────────────────
            if (UploadedFile == null || UploadedFile.Length == 0)
            {
                ErrorMessage = "Please select a file before uploading.";
                return Page();
            }

            var ext = Path.GetExtension(UploadedFile.FileName).ToLower();

            if (!AllowedExtensions.Contains(ext))
            {
                ErrorMessage = $"Invalid file type '{ext}'. Only PDF (.pdf) and Word (.docx) files are accepted.";
                return Page();
            }

            if (UploadedFile.Length > MaxFileSizeBytes)
            {
                ErrorMessage = $"File is too large ({UploadedFile.Length / 1024 / 1024} MB). Maximum allowed size is 20 MB.";
                return Page();
            }

            // ── Save file ────────────────────────────────────────────────────────
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);

            // Unique filename to prevent overwrite collisions
            var safeFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(UploadedFile.FileName)}";
            var filePath     = Path.Combine(uploadsFolder, safeFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await UploadedFile.CopyToAsync(stream);
            }

            // ── AI Extraction ────────────────────────────────────────────────────
            var extraction = DocumentExtractionService.Extract(filePath, ext);

            if (!string.IsNullOrEmpty(extraction.Error))
            {
                // Delete file if extraction fails so it doesn't litter the uploads folder
                System.IO.File.Delete(filePath);
                ErrorMessage = $"Document extraction failed: {extraction.Error}";
                return Page();
            }

            // ── Duplicate Detection (both checks) ────────────────────────────────
            var duplicateCheck = DuplicateDetectionService.Check(
                _db,
                extraction.InvoiceNumber,
                extraction.Vendor,
                extraction.Amount
            );

            if (duplicateCheck.IsDuplicate)
            {
                System.IO.File.Delete(filePath);
                ErrorMessage = $"Duplicate detected — upload rejected. {duplicateCheck.Reason}";
                return Page();
            }

            // ── Save to database ─────────────────────────────────────────────────
            var document = new Document
            {
                FileName      = safeFileName,
                FilePath      = filePath,
                FileType      = ext.TrimStart('.').ToUpper(),
                Vendor        = extraction.Vendor,
                DocumentDate  = extraction.DocumentDate,
                Amount        = extraction.Amount,
                VAT           = extraction.VAT,
                InvoiceNumber = extraction.InvoiceNumber,
                DocumentType  = extraction.DocumentType,
                Status        = "Pending",
                ApprovalStep  = 1,
                UploadedBy    = HttpContext.Session.GetString("User"),
                UploadedAt    = DateTime.Now,
                ExtractedText = extraction.RawText?.Length > 2000
                                    ? extraction.RawText[..2000]
                                    : extraction.RawText
            };

            _db.Documents.Add(document);
            await _db.SaveChangesAsync();

            // ── Log audit entry ──────────────────────────────────────────────────
            _db.ApprovalHistories.Add(new ApprovalHistory
            {
                DocumentId = document.Id,
                ActionBy   = document.UploadedBy,
                Role       = role,
                Step       = 0,
                Action     = "Uploaded",
                Comment    = $"Document uploaded. Extracted: {document.Vendor}, {document.InvoiceNumber}, R{document.Amount:N2}",
                Timestamp  = DateTime.Now
            });
            await _db.SaveChangesAsync();

            SuccessResult = extraction;
            return Page();
        }
    }
}
