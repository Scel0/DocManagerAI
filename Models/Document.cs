namespace DocManagerAI.Models
{
    public class Document
    {
        public int      Id            { get; set; }
        public string   FileName      { get; set; }
        public string   FilePath      { get; set; }
        public string   FileType      { get; set; }  // PDF | DOCX

        // Extracted by AI
        public string   Vendor        { get; set; }
        public DateTime? DocumentDate { get; set; }
        public decimal  Amount        { get; set; }
        public decimal  VAT           { get; set; }
        public string   InvoiceNumber { get; set; }
        public string   DocumentType  { get; set; }  // Invoice | Credit Note

        // Workflow
        public string   Status        { get; set; }  // Pending | Approved | Rejected
        public int      ApprovalStep  { get; set; }  // 1, 2, 3
        public string   RejectedReason { get; set; }

        // Audit
        public string   UploadedBy    { get; set; }
        public DateTime UploadedAt    { get; set; } = DateTime.Now;
        public string   ExtractedText { get; set; }  // raw OCR text stored for reference

        public ICollection<ApprovalHistory> ApprovalHistories { get; set; }
    }
}
