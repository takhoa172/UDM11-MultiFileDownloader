namespace Shared
{
    public static class AuthProtocol
    {
        // Client -> Server
        public const string Register = "REGISTER";
        public const string Login = "LOGIN";
        public const string Logout = "LOGOUT";
        public const string ChangePassword = "CHANGE_PASSWORD";

        // Server -> Client
        public const string RegisterOk = "REGISTER_OK";
        public const string RegisterFail = "REGISTER_FAIL";

        public const string LoginOk = "LOGIN_OK";
        public const string LoginFail = "LOGIN_FAIL";

        public const string LogoutOk = "LOGOUT_OK";

        public const string ChangePasswordOk = "CHANGE_PASSWORD_OK";
        public const string ChangePasswordFail = "CHANGE_PASSWORD_FAIL";

        // Authentication / Authorization
        public const string InvalidToken = "INVALID_TOKEN";
        public const string AccessDenied = "ACCESS_DENIED";
    }
}