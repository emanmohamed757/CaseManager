using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Domain.Services;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CaseManager.WinForms
{
    public partial class UnassignedCasesControl : UserControl
    {
        private CaseService _caseService;

        private UserContext _userContext;

        public UnassignedCasesControl()
        {
            InitializeComponent();
        }

        public void Setup(CaseService caseService, UserContext userContext)
        {
            _caseService = caseService;
            _userContext = userContext;
        }

        public async Task LoadCases()
        {
            gvUnassignedCases.DataSource = await Task.Run(() => _caseService.GetUnassignedCases());
        }
    }
}
