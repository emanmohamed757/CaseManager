using CaseManager.BusinessLogic.Authorization;
using Serilog;
using Serilog.Context;
using System.Windows.Forms;

namespace CaseManager.WinForms
{
    public partial class LoginForm : Form
    {
        private readonly UserContext _userContext;

        private readonly AuthorizationService _authorizationService;

        private readonly ILogger _logger;

        public LoginForm(UserContext userContext, AuthorizationService authorizationService, ILogger logger)
        {
            InitializeComponent();
            _userContext = userContext;
            _authorizationService = authorizationService;
            _logger = logger.ForContext<LoginForm>();
            textBox1.Text = "davidk_d3";
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            string username = textBox1.Text;

            if (!_authorizationService.Authorize(username))
            {
                _logger.Information($"Username \"{username}\" was unauthorized");
                MessageBox.Show("You are not authorized.");
                return;
            }

            UserContext userInfo = _authorizationService.GetUserInfo(username);

            _userContext.Username = username;
            _userContext.DepartmentId = userInfo.DepartmentId;
            _userContext.Name = userInfo.Name;
            _userContext.EffectivePermissions = userInfo.EffectivePermissions;

            _logger.Information($"Username \"{username}\" was authorized");

            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                button1.PerformClick();
            }
        }
    }
}
