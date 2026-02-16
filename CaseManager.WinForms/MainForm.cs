using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Domain.Services;
using Serilog;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CaseManager.WinForms
{
    public partial class MainForm : Form
    {
        private readonly CaseService _caseService;

        private readonly UserContext _userContext;

        private readonly ILogger _logger;

        public MainForm(CaseService caseService, UserContext userContext, ILogger logger)
        {
            InitializeComponent();
            _caseService = caseService;
            _userContext = userContext;
            _logger = logger.ForContext<MainForm>();
            label2.Text = $"Welcome, {userContext.Name}";
        }

        private async void MainForm_Load(object sender, System.EventArgs e)
        {
            unassignedCasesControl1.Setup(_caseService, _userContext, _logger);
            await unassignedCasesControl1.LoadCases();
        }

        private async void btnCreateCase_Click(object sender, System.EventArgs e)
        {
            var @case = new Case
            {
                CaseNumber = txtCaseNumber.Text,
            };
            await Task.Run(() => _caseService.CreateCase(@case));

            await unassignedCasesControl1.LoadCases();
            MessageBox.Show("Case created successfully.");
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            FormFactory.CreateAnotherForm("This is a message.").Show();
        }
    }
}
