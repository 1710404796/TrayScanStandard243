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
using LinxUniverse.Auth;
using TrayScanStandard.Attritubes;
using TrayScanStandard.View.User;
using TrayScanStandard.ViewModel;

namespace TrayScanStandard.View
{
    /// <summary>
    /// UserManagerView.xaml 的交互逻辑
    /// </summary>
    [PowerView(PowerEnum.用户管理界面)]
    public partial class UserManagerView : Page
    {
        private readonly UserManager<LinxUser> _userManager;
        public UserManagerViewModel ViewModel { get; }

        public UserManagerView(UserManager<LinxUser> userManager, UserManagerViewModel userManagerViewModel)
        {
            DataContext = this;
            _userManager = userManager;
            ViewModel = userManagerViewModel;

            InitializeComponent();
        }

        private void ddShow_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {

        }

        private void ddShow_LoadingRow(object sender, DataGridRowEventArgs e)
        {

        }

        private async void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var newUser = new AddUser();

            if (newUser.ShowDialog() ?? false)
            {
                var user = new LinxUser
                {
                    Password = newUser.RFID.Trim(),
                    UserName = newUser.UserName.Trim(),
                };

                var result = await _userManager.CreateWithRoleAsync(user, newUser.RoleEnum.ToString());
                if (!result.IsSuccess)
                {
                    MessageBox.Show(result.Message, "添加用户失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ViewModel.Users.Add(new AdvUser { LinxUser = user, Role = newUser.RoleEnum });
            }
        }

        private async void DelUser_Click(object sender, RoutedEventArgs e)
        {
            var advuser = ddShow.SelectedItem as AdvUser;
            if (advuser != null)
            {

                var result = await _userManager.DeleteAsync(advuser.LinxUser);
                if (result.IsSuccess)
                {
                    ViewModel.Users.Remove(advuser);
                }
                else
                {
                    MessageBox.Show(result.Message, "删除用户失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }


        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {

            IEnumerable<LinxUser> enumerable = (await _userManager.GetAllUserAsync());
            List<AdvUser> advUsers = [];
            foreach (LinxUser user in enumerable)
            {
                string? roleName;
                try
                {
                    roleName = (await _userManager.GetUserRolesAsync(user)).FirstOrDefault();
                }
                catch
                {
                    advUsers.Add(new AdvUser
                    {
                        LinxUser = user,
                        RoleDisplayOverride = "读取角色失败"
                    });
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(roleName)
                    && Enum.TryParse<RoleEnum>(roleName, ignoreCase: true, out var role)
                    && Enum.IsDefined(typeof(RoleEnum), role))
                {
                    advUsers.Add(new AdvUser
                    {
                        LinxUser = user,
                        Role = role
                    });
                }
                else
                {
                    advUsers.Add(new AdvUser
                    {
                        LinxUser = user,
                        RoleDisplayOverride = string.IsNullOrWhiteSpace(roleName)
                            ? "未分配角色"
                            : roleName
                    });
                }
            }

            ViewModel.Users = new(advUsers);
        }

        private async void EditUser_Click(object sender, RoutedEventArgs e)
        {
            //var advuser = ddShow.SelectedItem as AdvUser;
            //if (advuser != null)
            //{

            //    var newUser = new AddUser() { RFID = advuser.LinxUser.Password, RoleEnum }

            //if (newUser.ShowDialog() ?? false)
            //    {
            //        LinxUser user = new LinxUser
            //        {
            //            Password = newUser.RFID,
            //            UserName = newUser.UserName,
            //        };

            //    } 
            //}
        }
    }
}
