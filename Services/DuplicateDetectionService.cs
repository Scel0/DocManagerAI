using DocManagerAI.Data;

namespace DocManagerAI.Services
{
    public class DuplicateCheckResult
    {
        public bool   IsDuplicate { get; set; }
        public string Reason      { get; set; } = "";
        public int    MatchedId   { get; set; }
    }

    public static class DuplicateDetectionService
    {
        /// <summary>
        /// Runs two duplicate checks:
        /// 1. Exact invoice number match
        /// 2. Same vendor + same amount (secondary validation)
        /// </summary>
        public static DuplicateCheckResult Check(
            AppDbContext db,
            string invoiceNumber,
            string vendor,
            decimal amount,
            int? excludeId = null)
        {
            // ── Check 1: Invoice number match ────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(invoiceNumber))
            {
                var existing = db.Documents.FirstOrDefault(d =>
                    d.InvoiceNumber == invoiceNumber &&
                    (excludeId == null || d.Id != excludeId));

                if (existing != null)
                {
                    return new DuplicateCheckResult
                    {
                        IsDuplicate = true,
                        MatchedId   = existing.Id,
                        Reason      = $"Invoice number '{invoiceNumber}' already exists " +
                                      $"(Document #{existing.Id} — {existing.Vendor}, " +
                                      $"uploaded {existing.UploadedAt:dd MMM yyyy})."
                    };
                }
            }

            // ── Check 2: Vendor + Amount match ───────────────────────────────────
            if (!string.IsNullOrWhiteSpace(vendor) &&
                vendor != "Unknown" &&
                amount > 0)
            {
                var existing = db.Documents.FirstOrDefault(d =>
                    d.Vendor == vendor &&
                    d.Amount == amount &&
                    (excludeId == null || d.Id != excludeId));

                if (existing != null)
                {
                    return new DuplicateCheckResult
                    {
                        IsDuplicate = true,
                        MatchedId   = existing.Id,
                        Reason      = $"A document from '{vendor}' for R{amount:N2} already exists " +
                                      $"(Document #{existing.Id} — invoice {existing.InvoiceNumber}, " +
                                      $"uploaded {existing.UploadedAt:dd MMM yyyy}). " +
                                      "Vendor + Amount combination matches an existing record."
                    };
                }
            }

            return new DuplicateCheckResult { IsDuplicate = false };
        }
    }
}
