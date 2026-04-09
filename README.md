# DocManager AI
**Secure AI-powered Invoice & Credit Note Management System**

---

## WHAT THIS SYSTEM DOES

| Feature              | Detail                                                        |
|----------------------|---------------------------------------------------------------|
| Authentication       | 5 roles: Admin, Reviewer, Manager, Finance, Viewer            |
| Uploads              | PDF and DOCX only — Invoices & Credit Notes                   |
| AI Extraction        | Auto-reads vendor, date, amount, VAT, invoice number          |
| Duplicate Detection  | Checks invoice number AND vendor+amount combination           |
| Approval Workflow    | Exactly 3 mandatory steps: Reviewer → Manager → Finance       |
| Reports              | Filter by date, vendor, status, amount range                  |
| AI Insights          | Trends, anomalies, spending patterns, vendor concentration    |

---

## HOW TO OPEN IN VISUAL STUDIO

1. Extract this zip anywhere on your computer (e.g. Desktop)
2. Open Visual Studio 2022
3. Click "Open a project or solution"
4. Navigate into the extracted folder
5. Select DocManagerAI.csproj and click Open
6. Wait for NuGet packages to restore (bottom status bar)
7. Press F5 or click the green Run button
8. Browser opens at http://localhost:5000

The SQLite database (app.db) is created automatically on first run.
No migrations needed. No tessdata folder needed.

---

## LOGIN ACCOUNTS

| Username  | Password      | Role     | Can Approve At    |
|-----------|---------------|----------|-------------------|
| admin     | Admin@1234    | Admin    | Any step          |
| reviewer  | Review@1234   | Reviewer | Step 1            |
| manager   | Manage@1234   | Manager  | Step 2            |
| finance   | Finance@1234  | Finance  | Step 3 (final)    |
| viewer    | View@1234     | Viewer   | View only         |

---

## HOW DUPLICATE DETECTION WORKS

Check 1 - Invoice Number: exact match against all existing records.
Check 2 - Vendor + Amount: if same vendor AND same amount exists, also rejected.

Both checks run on every upload. The file is deleted if either check fails.

---

## PACKAGES USED (auto-installed by NuGet)

- PdfPig              — reads text from PDF files
- DocumentFormat.OpenXml — reads text from DOCX files
- BCrypt.Net-Next     — secure password hashing
- EF Core + SQLite    — database
