namespace DotNetModulith.Abstractions.Results;

public static partial class ApiCodes
{
    /// <summary>
    /// 用户与角色域业务码
    /// </summary>
    public static class User
    {
        /// <summary>
        /// 用户名已存在
        /// </summary>
        public const int UserNameAlreadyExists = 50001;

        /// <summary>
        /// 邮箱已存在
        /// </summary>
        public const int EmailAlreadyExists = 50002;

        /// <summary>
        /// 角色不存在
        /// </summary>
        public const int RoleNotFound = 50003;

        /// <summary>
        /// 权限编码不合法
        /// </summary>
        public const int InvalidPermission = 50004;

        /// <summary>
        /// 角色名已存在
        /// </summary>
        public const int RoleNameAlreadyExists = 50005;

        /// <summary>
        /// 当前密码不正确
        /// </summary>
        public const int InvalidCurrentPassword = 50006;
    }
}
