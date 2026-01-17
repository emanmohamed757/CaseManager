using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Domain.Services;
using System.Windows.Forms;

namespace CaseManager.WinForms
{
    public partial class MainForm : Form
    {
        private readonly CaseService _caseService;
        private readonly UserContext _userContext;

        public MainForm(CaseService caseService, UserContext userContext)
        {
            InitializeComponent();
            _caseService = caseService;
            _userContext = userContext;
            label2.Text = $"Welcome, {userContext.Name}";
        }

        private void btnCreateCase_Click(object sender, System.EventArgs e)
        {
            var @case = new Case
            {
                CaseNumber = txtCaseNumber.Text,
            };

            _caseService.CreateCase(@case);

            MessageBox.Show("Case created successfully.");
        }
    }
}
