namespace ProjectName.Domain.Constants
{
    public static class AppConstants
    {
        public const string NoCustomerName = "[NO CUSTOMER]";

        // Access Mask Indexes (0-based)
        public const int AccQuery = 0;
        public const int AccInput = 1;
        public const int AccCheck = 2;
        public const int AccAdmin = 3;
        public const int AccUserAdmin = 4;
        public const int AccReport = 5;
        public const int AccSystemControl = 6;

        public const string LogicalDeletedDeptPrefix = "\u0001"; // char(1) from Delphi
    }

    public static class AuditConstants
    {
        public const string ActionLogin = "LOGIN";
        public const string ActionLogout = "LOGOUT";
        public const string ActionCreatePositionReport = "CREATE_POS_RPT";
        public const string ActionUpdatePositionReport = "UPDATE_POS_RPT";
        public const string ActionDeletePositionReport = "DELETE_POS_RPT"; // Logical delete
        public const string ActionUpdateStatus = "UPDATE_STATUS";
        public const string ActionCheckout = "CHECKOUT_POS_RPT";
        public const string ActionCheckin = "CHECKIN_POS_RPT";
        public const string ActionCloseSystem = "CLOSE_SYSTEM";
        public const string ActionOpenSystem = "OPEN_SYSTEM";
        public const string ActionCreateUser = "CREATE_USER";
        public const string ActionUpdateUser = "UPDATE_USER";
        public const string ActionDeleteUser = "DELETE_USER";
        public const string ActionChangePassword = "CHANGE_PWD";
        public const string ActionAddDepartment = "ADD_DEPT";
        public const string ActionUpdateDepartment = "UPDATE_DEPT";
        public const string ActionDeleteDepartment = "DEL_DEPT";
        public const string ActionGenerateReport = "GENERATE_REPORT";
        public const string ActionVerifyOutstanding = "VERIFIED_OUTSTANDING";
    }
}