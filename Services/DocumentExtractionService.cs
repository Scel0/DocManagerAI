using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DocManagerAI.Services
{
    public class ExtractionResult
    {
        public string    Vendor        { get; set; } = "Unknown";
        public string    InvoiceNumber { get; set; } = "";
        public decimal   Amount        { get; set; } = 0;
        public decimal   VAT           { get; set; } = 0;
        public DateTime? DocumentDate  { get; set; }
        public string    DocumentType  { get; set; } = "Invoice";
        public string    RawText       { get; set; } = "";
        public string    Error         { get; set; } = "";
    }

    public static class DocumentExtractionService
    {
        // ── Entry point ──────────────────────────────────────────────────────────
        public static ExtractionResult Extract(string filePath, string fileExtension)
        {
            var result = new ExtractionResult();
            try
            {
                result.RawText = fileExtension.ToLower() switch
                {
                    ".pdf"  => ExtractFromPdf(filePath),
                    ".docx" => ExtractFromDocx(filePath),
                    _       => throw new NotSupportedException("Only PDF and DOCX files are supported.")
                };

                ParseFields(result);
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }
            return result;
        }

        // ── PDF text extraction ──────────────────────────────────────────────────
        private static string ExtractFromPdf(string path)
        {
            var sb = new StringBuilder();
            using var doc = PdfDocument.Open(path);
            foreach (Page page in doc.GetPages())
                sb.AppendLine(page.Text);
            return sb.ToString();
        }

        // ── DOCX text extraction ─────────────────────────────────────────────────
        private static string ExtractFromDocx(string path)
        {
            var sb = new StringBuilder();
            using var doc = WordprocessingDocument.Open(path, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return "";
            foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                var line = para.InnerText?.Trim();
                if (!string.IsNullOrEmpty(line))
                    sb.AppendLine(line);
            }
            return sb.ToString();
        }

        // ── Orchestrate field parsing ────────────────────────────────────────────
        private static void ParseFields(ExtractionResult r)
        {
            var text = r.RawText;
            if (string.IsNullOrWhiteSpace(text)) return;

            // Detect document type FIRST so Amount/VAT use the right patterns
            r.DocumentType  = DetectDocumentType(text);
            r.Vendor        = ExtractVendor(text);
            r.InvoiceNumber = ExtractInvoiceNumber(text);
            r.Amount        = ExtractAmount(text, r.DocumentType);
            r.VAT           = ExtractVAT(text, r.Amount);
            r.DocumentDate  = ExtractDate(text);
        }

        // ── Vendor ───────────────────────────────────────────────────────────────
        private static string ExtractVendor(string text)
        {
            // Prefer an explicit label first
            var labelled = Regex.Match(text,
                @"(?:From|Supplier|Vendor|Company|Sold\s*By|Bill\s*(?:From|To))\s*[:\-]\s*(.+)",
                RegexOptions.IgnoreCase);
            if (labelled.Success)
                return labelled.Groups[1].Value.Trim();

            // Fall back: first meaningful line that doesn't look like a field label
            foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var clean = line.Trim();
                if (clean.Length < 3) continue;
                if (Regex.IsMatch(clean, @"^\d")) continue;
                if (Regex.IsMatch(clean,
                    @"^(Invoice|Credit|Date|Tel|Fax|www|VAT|Tax|Reg|P\.O|PO Box|Amount|Total|Due|Balance)",
                    RegexOptions.IgnoreCase)) continue;
                return clean.Length > 60 ? clean[..60] : clean;
            }
            return "Unknown";
        }

        // ── Invoice / Credit Note reference number ───────────────────────────────
        private static string ExtractInvoiceNumber(string text)
        {
            // Priority 1: explicitly labelled
            var labelled = Regex.Match(text,
                @"(?:Invoice\s*(?:No\.?|Number|#|Num|Ref)|Credit\s*Note\s*(?:No\.?|Number|#|Ref)|Reference\s*(?:No\.?|Number|#))\s*[:\-]?\s*([A-Z0-9\-\/]+)",
                RegexOptions.IgnoreCase);
            if (labelled.Success) return labelled.Groups[1].Value.Trim();

            // Priority 2: common prefixes
            var prefixed = Regex.Match(text,
                @"\b((?:INV|CN|CRN|CRD|REF|PO)[-\s]?[0-9]{3,})\b",
                RegexOptions.IgnoreCase);
            if (prefixed.Success) return prefixed.Groups[1].Value.Trim().ToUpper();

            // Priority 3: standalone alphanumeric reference
            var standalone = Regex.Match(text,
                @"(?:^|\n)\s*#?\s*([A-Z]{0,3}\d{4,})\s*(?:\n|$)",
                RegexOptions.Multiline);
            return standalone.Success ? standalone.Groups[1].Value.Trim() : "";
        }

        // ── Amount — type-aware ──────────────────────────────────────────────────
        private static decimal ExtractAmount(string text, string documentType)
        {
            // Patterns ordered from most specific to least specific.
            // Credit notes use different terminology, so we have separate lists.

            string[] invoicePatterns =
            {
                // Explicit invoice total labels
                @"(?:Grand\s+Total|Total\s+(?:Amount\s+)?Due|Amount\s+Due|Total\s+Payable|Invoice\s+Total|Net\s+(?:Total|Amount)|Amount\s+Payable)\s*[:\-]?\s*R?\s*([\d\s,]+\.?\d*)",
                // Generic "Total"
                @"\bTotal\b\s*[:\-]?\s*R?\s*([\d\s,]+\.?\d*)",
                // Rand amount anywhere
                @"R\s*([\d\s,]+\.\d{2})"
            };

            string[] creditNotePatterns =
            {
                // Credit note specific labels
                @"(?:Credit\s+Total|Total\s+Credit|Amount\s+Credited|Credit\s+Amount|Net\s+Credit|Total\s+(?:Credit\s+)?(?:Note\s+)?Amount|Credit\s+(?:Note\s+)?(?:Value|Total))\s*[:\-]?\s*R?\s*([\d\s,]+\.?\d*)",
                // Also accept generic total labels — many credit notes still say "Total"
                @"(?:Grand\s+Total|Total\s+(?:Amount\s+)?Due|Amount\s+Due|Total\s+Payable|Net\s+(?:Total|Amount))\s*[:\-]?\s*R?\s*([\d\s,]+\.?\d*)",
                @"\bTotal\b\s*[:\-]?\s*R?\s*([\d\s,]+\.?\d*)",
                // Rand amount anywhere
                @"R\s*([\d\s,]+\.\d{2})"
            };

            var patterns = documentType == "Credit Note" ? creditNotePatterns : invoicePatterns;

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match m in matches)
                {
                    var raw = m.Groups[1].Value.Replace(" ", "").Replace(",", "");
                    if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var val)
                        && val > 0)
                        return val;
                }
            }
            return 0;
        }

        // ── VAT — multi-strategy ─────────────────────────────────────────────────
        private static decimal ExtractVAT(string text, decimal totalAmount)
        {
            // Strategy 1: explicit VAT/Tax label with amount on same line
            // Handles: "VAT: R150.00", "VAT @ 15%: 150.00", "Tax Amount: 150",
            //          "Output Tax 150.00", "VAT (15%) 150.00", "15% VAT R150"
            var labelPatterns = new[]
            {
                @"(?:VAT|Value\s*Added\s*Tax|Output\s*(?:VAT|Tax)|Input\s*(?:VAT|Tax)|Tax\s*Amount|GST|TVA)\s*(?:[@\(]?\s*\d+\s*%\s*[\):]?)?\s*[:\-]?\s*R?\s*([\d\s,]+\.\d{2})",
                @"\d+\s*%\s*(?:VAT|Tax)\s*[:\-]?\s*R?\s*([\d\s,]+\.\d{2})",
                @"(?:VAT|Tax)\s*@\s*\d+\s*%\s*[:\-]?\s*R?\s*([\d\s,]+\.\d{2})"
            };

            foreach (var pattern in labelPatterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match m in matches)
                {
                    var raw = m.Groups[1].Value.Replace(" ", "").Replace(",", "");
                    if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var val)
                        && val > 0)
                        return val;
                }
            }

            // Strategy 2: VAT label on one line, amount on the NEXT line
            // (common in columnar invoice layouts)
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length - 1; i++)
            {
                var current = lines[i].Trim();
                if (Regex.IsMatch(current, @"^(?:VAT|Value\s*Added\s*Tax|Output\s*Tax|Tax\s*Amount|GST)[\s%@\d\(\)\.]*$",
                    RegexOptions.IgnoreCase))
                {
                    var next = lines[i + 1].Trim().Replace(",", "");
                    // Strip leading R
                    next = Regex.Replace(next, @"^R\s*", "");
                    if (decimal.TryParse(next, NumberStyles.Any, CultureInfo.InvariantCulture, out var val)
                        && val > 0)
                        return val;
                }
            }

            // Strategy 3: Calculate 15% VAT from the total if nothing found
            // Only apply if total > 0 and the total looks VAT-inclusive
            // (i.e., divisible-by-115 heuristic — the standard SA VAT rate)
            if (totalAmount > 0)
            {
                var calculatedVAT = Math.Round(totalAmount * 15m / 115m, 2);
                if (calculatedVAT > 0)
                    return calculatedVAT;
            }

            return 0;
        }

        // ── Date ─────────────────────────────────────────────────────────────────
        private static DateTime? ExtractDate(string text)
        {
            var patterns = new[]
            {
                @"(?:Date|Invoice\s*Date|Issue\s*Date|Credit\s*Note\s*Date|Tax\s*Date)\s*[:\-]?\s*(\d{1,2}[\s\-\/]\w+[\s\-\/]\d{4})",
                @"\b(\d{1,2}[\/\-\.]\d{1,2}[\/\-\.]\d{4})\b",
                @"\b(\d{4}[\/\-]\d{2}[\/\-]\d{2})\b",
                @"\b(\d{1,2}\s+(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\s+\d{4})\b"
            };

            var formats = new[]
            {
                "d MMM yyyy","d MMMM yyyy","dd/MM/yyyy","d/M/yyyy",
                "dd-MM-yyyy","d-M-yyyy","dd.MM.yyyy","yyyy-MM-dd","yyyy/MM/dd"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (!match.Success) continue;
                var raw = match.Groups[1].Value.Trim();
                foreach (var fmt in formats)
                {
                    if (DateTime.TryParseExact(raw, fmt,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                        return dt;
                }
                if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallback))
                    return fallback;
            }
            return null;
        }

        // ── Invoice vs Credit Note ────────────────────────────────────────────────
        private static string DetectDocumentType(string text)
        {
            if (Regex.IsMatch(text,
                @"\bCredit\s*Note\b|\bCredit\s*Memo\b|\bCREDIT\s*NOTE\b",
                RegexOptions.IgnoreCase))
                return "Credit Note";
            return "Invoice";
        }
    }
}
