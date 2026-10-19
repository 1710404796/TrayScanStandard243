namespace TrayScanStandard.Attritubes
{
    /// <summary>
    /// 权限枚举
    /// </summary>
    public enum PowerEnum
    {
        // WCS交互日志,
        // 报警日志,
        用户管理界面,
        // 电芯计数,
        // 拆盘日志,
        扫码日志,
        关闭软件,
        相机列表,
        相机参数设置,
        电芯种类管理,
        程序参数设定,
        电芯条码显示,
        光源管理,
    }
    /// <summary>
    /// 角色枚举
    /// </summary>
    public enum RoleEnum
    {
        操作员 = 1,
        技师 = 2,
        me技术员 = 3,
        ME工程师 = 4,
        系统管理员 = 5,

        SuperAdmin = 999
    }
}
