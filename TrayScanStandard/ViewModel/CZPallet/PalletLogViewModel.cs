using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LinxUniverse.Utils;
using Microsoft.EntityFrameworkCore;
using TrayScanStandard.Data;
using TrayScanStandard.Data.Models;
using TrayScanStandard.Models.CZPallet;

namespace TrayScanStandard.ViewModel.CZPallet
{
    public partial class PalletLogViewModel(LinxContext context) : ObservableRecipient
    {
        //public ObservableCollection<PalletLogViewModel> Log { get; set; } = [];
        [ObservableProperty] private ObservableCollection<PalletLogExt> _palletLogs = [];
        private IEnumerable<PalletLog> _logs = [];
        public DateTime StartTime { get; set; } = DateTime.Today.AddDays(-30);
        public DateTime EndTime { get; set; } = DateTime.Today.AddDays(1);

        public string Code { get; set; } = string.Empty;
        public int LogNum { get; set; } = 100;


        public void RefreshContext()
        {
            _logs = context
                .PalletLogs.AsNoTracking()
                .OrderByDescending(s => s.Id)
                .Where(s => s.PalletType == PalletType.组盘).Take(LogNum).ToArray();
            Search();
        }

        public bool IsNowLog(WarningLog log)
        {

            return log.WarningTime >= StartTime && log.WarningTime <= EndTime;
        }

        [RelayCommand]
        public void ExportAll()
        {
            StringBuilder sb = new(1000);
            string[] titles = ["托盘编号", "组盘时间", "电池数量", "电池条码"];

            sb.AppendLine(string.Join(",", titles));

            foreach (var log in PalletLogs)
            {
                //sb.AppendLine($"{log.PalletCode},{log.ZuPanTime},{log.ChannelCount},[{string.Join("|", log.BatteryInfo.Select(s => s.BatteryCode))}]");
                sb.AppendLine($"{log.PalletCode},{log.ZuPanTime.ToString("yyyy-MM-dd HH:mm:ss")},{log.ChannelCount},[{string.Join("|", log.BatteryInfo.Select(s => s.BatteryCode))}]");

            }

            string fileName = $"InsertLog/exportall-{FilenameHelper.FileName}.csv";
            System.IO.File.WriteAllBytes(fileName, Encoding.GetEncoding("gb2312").GetBytes(sb.ToString()));
            MessageBox.Show($"export to {fileName}");

        }
        [RelayCommand]
        public void Export()
        {
            StringBuilder sb = new(1000);
            string[] titles = ["托盘编号", "组盘时间", "电池数量", "电池条码"];

            sb.AppendLine(string.Join(",", titles));

            foreach (var log in PalletLogs.Where(s => s.IsSelect))
            {
                //sb.AppendLine($"{log.PalletCode},{log.ZuPanTime},{log.ChannelCount},[{string.Join("|", log.BatteryInfo.Select(s => s.BatteryCode))}]");
                sb.AppendLine($"{log.PalletCode},{log.ZuPanTime.ToString("yyyy-MM-dd HH:mm:ss")},{log.ChannelCount},[{string.Join("|", log.BatteryInfo.Select(s => s.BatteryCode))}]");

            }

            string fileName = $"InsertLog/export-{FilenameHelper.FileName}.csv";
            System.IO.File.WriteAllBytes(fileName, Encoding.GetEncoding("gb2312").GetBytes(sb.ToString()));
            MessageBox.Show($"export to {fileName}");

        }
        [RelayCommand]
        public void Search()
        {
            lock (context)
            {
                _logs = context.PalletLogs.AsNoTracking().Where(s => s.PalletType == PalletType.组盘).OrderByDescending(s => s.Id);

            }
            var afterFilter = _logs.Where(s => s.ZuPanTime >= StartTime && s.ZuPanTime <= EndTime);

            if (!string.IsNullOrEmpty(Code))
            {
                afterFilter = afterFilter.Where(s => s.PalletCode.Contains(Code, StringComparison.InvariantCultureIgnoreCase));
            }

            PalletLogs = new(
                 afterFilter.Take(LogNum).Select(s => new PalletLogExt(s))
             );
        }
    }
}
