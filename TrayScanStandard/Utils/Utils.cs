using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using TrayScanStandard.Attritubes;


namespace TrayScanStandard.Utils
{
    public static class Utils
    {

        public static string GetRoleName(RoleEnum role)
        {
            return role switch
            {

                RoleEnum.ME工程师 => Properties.Resources.MEEngineer,
                RoleEnum.系统管理员 => Properties.Resources.Administrator,
                _ => role.ToString()
            };
        }

        /// <summary>
        /// 获取权限名称
        /// </summary>
        /// <param name="power"></param>
        /// <returns></returns>
        public static string GetPowerName(PowerEnum power)
        {
            return power switch
            {
                //PowerEnum.报警日志 => Properties.Resources.AlarmLog,
                //PowerEnum.WCS交互日志 => Properties.Resources.InteractionLog,
                PowerEnum.用户管理界面 => Properties.Resources.AccessControl,





                _ => power.ToString()
            };
        }

    }

}
