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
using TrayScanStandard.Attritubes;
using TrayScanStandard.Mediator.Queries;
using TrayScanStandard.View.CZPallet;
using TrayScanStandard.ViewModel;

namespace TrayScanStandard.View
{
    /// <summary>
    /// BcrBorder.xaml 的交互逻辑
    /// </summary>
    public partial class BcrBorder : UserControl
    {
        public BcrBorder(BcrBorderViewModel bcrBorderViewModel)
        {
            DataContext = this;
            ViewModel = bcrBorderViewModel;

            InitializeComponent();
            camview.Child = new Image2DView(bcrBorderViewModel.Image2DViewModel, true);
        }

        public BcrBorderViewModel ViewModel { get; }

        

        /// <summary>
        /// 设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            if ( new BcrSettingWindow(ViewModel.BcrInfo).ShowDialog()??false) MainStorage.SaveManager.Save();

        }

        /// <summary>
        /// 重连
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            { 
                button.IsEnabled = false;
            }

            try 
            { 
                var imageVm = ViewModel.Image2DViewModel;
                int cameraIdx = imageVm.CameraIdx;

                // 调用重连方法并处理结果
                var result = imageVm.Service.ReconnectCamera(cameraIdx);
                result.Match(
                    success =>
                    {
                        MessageBox.Show($"重连{cameraIdx}成功");
                        return 0;
                    },
                    failure =>
                    {
                        MessageBox.Show($"重连{cameraIdx}失败: {failure}");
                        return 0;
                    });
            }
            finally
            {
                if (button != null)
                {
                    button.IsEnabled = true;
                }
            }

        }

        public void Dispose()
        {
            // 如果 Image2DView 实现了 IDisposable，则移除子元素并调用 disposal 方法。
            if (camview.Child is IDisposable disposableView)
            {
                disposableView.Dispose();
            }
            camview.Child = null;
        }
    }
}
