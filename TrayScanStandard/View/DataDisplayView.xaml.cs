using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LinxUniverse.Utils;
using TrayScanStandard.Attritubes;
using TrayScanStandard.ViewModel;


namespace TrayScanStandard.View
{
    /// <summary>
    /// DataDisplayView.xaml 的交互逻辑
    /// </summary>
    // [PowerView(PowerEnum.电芯计数)]
    public partial class DataDisplayView : Page
    {

        public DataDisplayViewModel ViewModel { get; }

        public DataDisplayView()
        {
            DataContext = this;
            ViewModel = App.GetService<DataDisplayViewModel>();
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.DoYouWantToSaveThisDataAndReset, Properties.Resources.DataRecordConfirmation, button: MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                ViewModel.SaveOkNGCnt();

            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshContext();
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new();
            sb.AppendLine("OK,NG1,NG2,时间");
            foreach (var item in ViewModel.OkNGLogs)
            {
                sb.AppendLine($"{item.OKCnt},{item.NG1Cnt},{item.NG2Cnt},{item.EndTime:f}");
            }
            var name = $"CellCount/{FilenameHelper.FileName}.csv";

            File.WriteAllText(name, sb.ToString());

            MessageBox.Show($"Export to {name}!");



        }
    }
}
