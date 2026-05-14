using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinxUniverse.Algo.Common;
using LinxUniverse.CST;
using LinxUniverse.Utils;
using TrayScanStandard.Attritubes;
using TrayScanStandard.Data.Models;
using TrayScanStandard.Models;

namespace TrayScanStandard
{
    public static class MainStorage
    {
        public static SaveManager<WcsSaves> SaveManager = new();
        public static WcsSaves Saves => SaveManager.SaveFile;

        /// <summary>
        /// 启用WCS功能，启用后会显示相关UI并允许使用WCS功能
        /// </summary>
        public static bool IsWcsEnable { get; set; }
        public static Either<string, CodeDetecter> Algo { get; set; }
        public static Either<string, CodeDetecter> AlgoCnn { get; set; }
        public static int[] DefaultExp = [7500, 15000, 22500, 30000];
        public static PowerEnum[] PowerEnums => Enum.GetValues<PowerEnum>().ToArray();
        public static RoleEnum[] RoleEnums => Enum.GetValues<RoleEnum>().SkipLast(1).ToArray();
        public static BatteryTypeInfo? SelectBattery { get; set; }
        public static IEnumerable<LightCST> CST { get; set; } = [];

        public static void Init()
        {
            // 读盘恢复配置，没有文件就用默认配置
            SaveManager.Load();

        }
    }
}
