using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Domain.Services;
using CaseManager.WinForms.ViewModels;
using System.Collections.Generic;
using System.Linq;
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
            List<Case> cases = await Task.Run(() => _caseService.GetUnassignedCases());

            List<CaseViewModel> caseViewModels = cases.Select(@case =>
                new CaseViewModel
                {
                    CaseNumber = @case.CaseNumber,
                    CreatedAt = @case.CreatedAt,
                    CreatedBy = @case.CreatedBy,
                    DepartmentId = @case.DepartmentId,
                    DirectorUsername = @case.DirectorUsername,
                    IsDeleted = @case.IsDeleted,
                    ManagerUsername = @case.ManagerUsername,
                    StatusId = @case.StatusId,
                    TeamAssistantUsername = @case.TeamAssistantUsername,
                    TeamLeaderUsername = @case.TeamLeaderUsername,
                    UpdatedAt = @case.UpdatedAt,
                    UpdatedBy = @case.UpdatedBy,
                    StatusName = @case.CaseStatus.Name
                })
                .ToList();

            gvUnassignedCases.DataSource = caseViewModels;
        }
    }
}
