namespace DocManagerAI.Models
{
    public class ApprovalHistory
    {
        public int      Id         { get; set; }
        public int      DocumentId { get; set; }
        public string   ActionBy   { get; set; }
        public string   Role       { get; set; }
        public int      Step       { get; set; }
        public string   Action     { get; set; }  // Approved | Rejected
        public string   Comment    { get; set; }
        public DateTime Timestamp  { get; set; } = DateTime.Now;

        public Document Document   { get; set; }
    }
}
