using CaseManager.BusinessLogic.Authorization;
using System.Windows.Forms;

namespace CaseManager.WinForms
{
    public partial class LoginForm : Form
    {
        private readonly UserContext _userContext;
        private readonly AuthorizationService _authorizationService;

        public LoginForm(UserContext userContext, AuthorizationService authorizationService)
        {
            InitializeComponent();
            _userContext = userContext;
            _authorizationService = authorizationService;
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            string username = textBox1.Text;

            if (!_authorizationService.Authorize(username))
            {
                MessageBox.Show("You are not authorized.");
                return;
            }

            UserContext userInfo = _authorizationService.GetUserInfo(username);

            _userContext.Username = username;
            _userContext.DepartmentId = userInfo.DepartmentId;
            _userContext.Name = userInfo.Name;
            _userContext.EffectivePermissions = userInfo.EffectivePermissions;

            DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
