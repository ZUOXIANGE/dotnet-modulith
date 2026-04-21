namespace DotNetModulith.Abstractions.Results;

public static partial class ApiCodes
{
    /// <summary>
    /// 订单域业务码
    /// </summary>
    public static class Order
    {
        /// <summary>
        /// 订单状态不允许当前操作
        /// </summary>
        public const int InvalidState = 10001;
    }
}
