
using System;
using System.Windows;
using System.Windows.Controls;
using TrayScanStandard.Attritubes;

namespace TrayScanStandard.View.User
{
    /// <summary>
    /// AddUser.xaml 的交互逻辑
    /// </summary>
    public partial class AddUser : Window
    {
        public IEnumerable<string> RoleList { get; set; }
        public IEnumerable<RoleEnum> RoleEnumList { get; set; }
        public AddUser()
        {
            DataContext = this;
            RoleEnumList = Enum.GetValues<RoleEnum>()
                .Where(role => role != RoleEnum.SuperAdmin)
                .ToList();
            RoleList = RoleEnumList.Select(Utils.Utils.GetRoleName);
            InitializeComponent();
        }

        public string UserName;
        public string PassWord;
        public string PerName;

        public RoleEnum RoleEnum { get; internal set; }
        public string RFID { get; internal set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox;
            UserName = UName.Text;
            RFID = mm.Password.ToString();
            if (MType.SelectedIndex < 0)
            {
                MessageBox.Show("请选择用户角色");
                return;
            }

            RoleEnum = RoleEnumList.ElementAt(MType.SelectedIndex);
            UserName = UserName.Trim();
            RFID = RFID.Trim();
            if (UserName == null || UserName == "")
            {
                MessageBox.Show("用户名不能为空");
            }
            else if (string.IsNullOrWhiteSpace(mm.Password))
            {
                MessageBox.Show("RF不能为空");
            }
            else
            {
                DialogResult = true;
                Close();
            }
        }
    }
}
