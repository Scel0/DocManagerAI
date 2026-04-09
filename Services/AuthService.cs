namespace DocManagerAI.Services
{
    public static class AuthService
    {
        public static string Hash(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password);

        public static bool Verify(string password, string hash) =>
            BCrypt.Net.BCrypt.Verify(password, hash);

        // Returns true if the role is allowed to approve at the given step
        public static bool CanApproveAtStep(string role, int step)
        {
            if (role == "Admin") return true;
            return step switch
            {
                1 => role == "Reviewer",
                2 => role == "Manager",
                3 => role == "Finance",
                _ => false
            };
        }

        // Returns true if the role can upload documents
        public static bool CanUpload(string role) =>
            role is "Admin" or "Reviewer" or "Manager" or "Finance";

        // Returns true if the role can view reports
        public static bool CanViewReports(string role) =>
            role != null;

        public static string GetStepLabel(int step) => step switch
        {
            1 => "Awaiting Reviewer",
            2 => "Awaiting Manager",
            3 => "Awaiting Finance",
            _ => "Unknown"
        };
    }
}
