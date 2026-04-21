namespace DotNetModulith.Abstractions.Results;

public static partial class ApiCodes
{
    /// <summary>
    /// 认证域业务码
    /// </summary>
    public static class Auth
    {
        /// <summary>
        /// 用户名或密码错误
        /// </summary>
        public const int InvalidCredentials = 40001;

        /// <summary>
        /// 令牌无效或已失效
        /// </summary>
        public const int InvalidToken = 40002;

        /// <summary>
        /// 用户已被禁用
        /// </summary>
        public const int UserDisabled = 40003;
    }
}
