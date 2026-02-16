using CaseManager.BusinessLogic.Authorization;
using Serilog;
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

            _logger.Information($"Username \"{username}\" was authorized");

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
