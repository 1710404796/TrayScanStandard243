// filepath: d:\wcsrepo\TrayScanStandard\TrayScanStandard\TrayScanStandard\View\BcrSettingWindow.xaml.cs
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
using System.Windows.Shapes;
using TrayScanStandard.Mediator.Commands;
using TrayScanStandard.Models;
using TrayScanStandard.Service;
using Camera.Fs.Common;
using MugenCamera;

namespace TrayScanStandard.View
{
    /// <summary>
    /// 相机设置窗口 的交互逻辑
    /// </summary>
    public partial class BcrSettingWindow : Window
    {
        public CameraSetting Setting { get; set; }
        public readonly IMediator mediator;
        
        public BcrSettingWindow(CameraSetting setting)
        {
            mediator = App.GetService<IMediator>();
            Setting = setting;
            InitializeComponent();

            // 重置相机类型
            InitializeCameraTypeSelection();

            // 初始化连接设置
            InitializeConnectionSettings();

            // 初始化曝光值
            ExpPattern.Text = string.Join("\n", Setting.Exposure);

            // 初始化备份曝光值
            ExpBackupPattern.Text = string.Join("\n", Setting.ExposureBackup);
        }

        /// <summary>
        /// 根据当前设置初始化相机类型下拉菜单
        /// </summary>
        private void InitializeCameraTypeSelection()
        {
            // 若当前类型为 HikVision，则默认使用 HikVision。
            if (Setting.CameraAddresses is HKAddress)
            {
                CameraTypeCombo.SelectedIndex = 0; // HikVision
            }
            else if (Setting.CameraAddresses is HuaruiAddress)
            {
                CameraTypeCombo.SelectedIndex = 1; // Huarui
            }
            else
            {
                CameraTypeCombo.SelectedIndex = 0; // Default to HikVision
            }
            // 根据需要添加更多相机类型的初始化。
        }

        /// <summary>
        /// 根据当前设置初始化连接类型和值
        /// </summary>
        private void InitializeConnectionSettings()
        {
            if (Setting.CameraAddresses is HKAddress hkAddress)
            {
                if (hkAddress.ConnectAddress is Camera.Fs.Common.Key key)
                {
                    ConnectionTypeCombo.SelectedIndex = 0; // Key
                    ConnectionValueBox.Text = key.Value;
                }
                else if (hkAddress.ConnectAddress is Camera.Fs.Common.IPAddress ipAddress)
                {
                    ConnectionTypeCombo.SelectedIndex = 1; // IP
                    ConnectionValueBox.Text = ipAddress.Value;
                }
                else if (hkAddress.ConnectAddress is Camera.Fs.Common.Serial serial)
                {
                    ConnectionTypeCombo.SelectedIndex = 2; // Serial
                    ConnectionValueBox.Text = serial.Value;
                }
            }
            else if (Setting.CameraAddresses is HuaruiAddress hrAddress)
            {
                if (hrAddress.ConnectAddress is Camera.Fs.Common.Key key)
                {
                    ConnectionTypeCombo.SelectedIndex = 0; // Key
                    ConnectionValueBox.Text = key.Value;
                }
                else if (hrAddress.ConnectAddress is Camera.Fs.Common.IPAddress ipAddress)
                {
                    ConnectionTypeCombo.SelectedIndex = 1; // IP
                    ConnectionValueBox.Text = ipAddress.Value;
                }
                else if (hrAddress.ConnectAddress is Camera.Fs.Common.Serial serial)
                {
                    ConnectionTypeCombo.SelectedIndex = 2; // Serial
                    ConnectionValueBox.Text = serial.Value;
                }
            }
        }

        /// <summary>
        /// 相机类型选择更改
        /// </summary>
        private void CameraType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 目前我们仅支持HikVision，但未来可扩展支持更多品牌。
        }

        /// <summary>
        /// 处理连接类型选择变更
        /// </summary>
        private void ConnectionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 如有需要，可根据连接类型添加特定逻辑。
            // 例如，显示不同的用户界面元素或验证规则
        }

        /// <summary>
        /// “保存”按钮点击处理程序 - 保存所有设置
        /// </summary>
        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // 手动设置主曝光
                string exposureLogMessage = $"曝光{string.Join(",", Setting.Exposure)}修改为{ExpPattern.Text}";
                await mediator.Send(new OperationLogCommand(exposureLogMessage));
                Setting.Exposure = ExpPattern.Text.Trim().Split('\n').Select(int.Parse).ToArray();

                // 处理备份导出设置
                string backupExposureLogMessage = $"备用曝光{string.Join(",", Setting.ExposureBackup)}修改为{ExpBackupPattern.Text}";
                await mediator.Send(new OperationLogCommand(backupExposureLogMessage));
                Setting.ExposureBackup = ExpBackupPattern.Text.Trim().Split('\n').Select(int.Parse).ToArray();

                // 处理相机类型和连接值的变化
                UpdateCameraAddresses();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"有不合法数值，修改失败! 错误信息: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据用户界面选择更新摄像头地址设置
        /// </summary>
        private void UpdateCameraAddresses()
        {
            // 从用户界面获取连接值
            string connectionValue = ConnectionValueBox.Text;

            // 根据所选类型创建相应的连接地址类型。
            ConnectAddress connectAddress;
            switch (ConnectionTypeCombo.SelectedIndex)
            {
                case 0: // Key
                    connectAddress = new Camera.Fs.Common.Key(connectionValue);
                    break;
                case 1: // IP
                    connectAddress = new Camera.Fs.Common.IPAddress(connectionValue);
                    break;
                case 2: // Serial
                    connectAddress = new Camera.Fs.Common.Serial(connectionValue);
                    break;
                default:
                    connectAddress = new Camera.Fs.Common.Key(connectionValue);
                    break;
            }

            // 根据相机类型选择设置相机地址
            switch (CameraTypeCombo.SelectedIndex)
            {
                case 0: // HikVision
                    Setting.CameraAddresses = new HKAddress(connectAddress);
                    break;
                case 1: // HikVision
                    Setting.CameraAddresses = new HuaruiAddress(connectAddress);
                    break;

                default:
                    break;
                    // 根据需要添加更多类型的摄像头
            }
        }
    }
}
