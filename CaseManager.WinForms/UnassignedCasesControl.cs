using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Domain.Services;
using CaseManager.WinForms.ViewModels;
using Serilog;
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

        private ILogger _logger;

        public UnassignedCasesControl()
        {
            InitializeComponent();
        }

        public void Setup(CaseService caseService, UserContext userContext, ILogger logger)
        {
            _caseService = caseService;
            _userContext = userContext;
            _logger = logger.ForContext<UnassignedCasesControl>();
        }

        public async Task LoadCases()
        {
            _logger.Verbose("Loading unassigned cases.");

            List<Case> cases = await Task.Run(() => _caseService.GetUnassignedCases());

            _logger.Verbose("Changing loaded unassigned cases to CaseViewModel.");

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

            _logger.Verbose("Setting gvUnassignedCases.DataSource to caseViewModels.");

            gvUnassignedCases.DataSource = caseViewModels;
        }
    }
}
