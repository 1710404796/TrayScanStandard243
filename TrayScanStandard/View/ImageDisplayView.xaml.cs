
using MediatR;
using System;
using System.Collections.Generic;
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
using TrayScanStandard.Mediator.Commands;
using TrayScanStandard.ViewModel;

namespace TrayScanStandard.View
{
    /// <summary>
    /// ImageDisplayView.xaml 的交互逻辑
    /// </summary>
    public partial class ImageDisplayView : Page
    {
        private IMediator _meditor;

        public ImageDisplayViewModel ViewModel { get; }
        public ImageDisplayView(ImageDisplayViewModel viewModel)
        {
            _meditor = App.GetService<IMediator>();
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
            viewModel.XYLStation.ChannelNum = viewModel.SelectBatteryInfo.Count;
            palletv.Station = viewModel.XYLStation;
        }

        private async void DelectBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null)
            {
                return;
            }

            btn.IsEnabled = false;
            var res = await _meditor.Send(new DetectCCDCommand(ViewModel.SelectBatteryInfo));

            await res.MatchAsync(

                LeftAsync: async l =>
                {
                    await _meditor.Send(new WarningBoxCommand(l));
                    return LanguageExt.Unit.Default;
                },
                RightAsync: async r =>
                {
                    await _meditor.Send(new InformationBoxCommand("检测完成"));
                    return LanguageExt.Unit.Default;

                }
                
                

                );

            ////Dispatcher.Invoke(() => btn.IsEnabled = true);
            btn.IsEnabled = true;

        }

        private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await Task.Delay(100);
            palletv.Refesh();
        }
        
    }
}
