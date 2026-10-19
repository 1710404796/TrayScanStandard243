using Humanizer;
using LinxUniverse.Auth;
using MediatR;
using System.Windows;
using TrayScanStandard;
using TrayScanStandard.Attritubes;
using TrayScanStandard.Mediator.Queries;


namespace TrayScanStandard.Mediator.Handlers
{
    /// <summary>
    /// 检查权限查询处理程序 // 根据需要去查询权限
    /// </summary>
    /// <param name="authenticationStateProvider"></param>
    public class CheckPowerQueryHandler(LinxAuthenticationStateProvider authenticationStateProvider) : IRequestHandler<CheckPowerQuery, bool>
    {
        public async Task<bool> Handle(CheckPowerQuery request, CancellationToken cancellationToken)
        {
            string? roleName = (await authenticationStateProvider.GetAuthenticationStateAsync()).User.GetUserRole();

            if (string.Equals(roleName, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!Enum.TryParse(roleName, ignoreCase: true, out RoleEnum roleEnum)
                || !Enum.IsDefined(typeof(RoleEnum), roleEnum))
            {
                return false;
            }

            if (roleEnum == RoleEnum.SuperAdmin)
            {
                return true;
            }

            if (!MainStorage.Saves.PowerTable.TryGetValue(request.PowerEnum, out var roleMap))
            {
                return false;
            }

            return roleMap.TryGetValue(roleEnum, out var granted) && granted;
        }
    }
}
