using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Domain.Dtos;
using CaseManager.BusinessLogic.Domain.Services;
using CaseManager.WinForms.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            AddContextMenu();
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

            List<CaseDto> cases = await Task.Run(() => _caseService.GetUnassignedCases());

            _logger.Verbose("Changing loaded unassigned cases to CaseViewModel.");

            List<CaseViewModel> caseViewModels = cases.Select(@case =>
                new CaseViewModel
                {
                    Id = @case.Id,
                    CaseNumber = @case.CaseNumber,
                    CreatedAt = @case.CreatedAt,
                    CreatedBy = @case.CreatedBy,
                    DepartmentId = @case.DepartmentId,
                    DepartmentName = @case.DepartmentName,
                    //DirectorUsername = @case.DirectorUsername,
                    IsDeleted = @case.IsDeleted,
                    //ManagerUsername = @case.ManagerUsername,
                    StatusId = @case.StatusId,
                    //TeamAssistantUsername = @case.TeamAssistantUsername,
                    //TeamLeaderUsername = @case.TeamLeaderUsername,
                    UpdatedAt = @case.UpdatedAt,
                    UpdatedBy = @case.UpdatedBy,
                    StatusName = @case.CaseStatus.Name
                })
                .ToList();

            _logger.Verbose("Setting gvUnassignedCases.DataSource to caseViewModels.");

            gvUnassignedCases.DataSource = caseViewModels;
        }

        private void AddContextMenu()
        {
            ContextMenuStrip strip = new ContextMenuStrip();
            gvUnassignedCases.ContextMenuStrip = strip;

            var menuItem = new ToolStripMenuItem();
            menuItem.Text = "View Details";
            menuItem.Click += new EventHandler(viewDetails_Click);
            gvUnassignedCases.ContextMenuStrip.Items.Add(menuItem);

            var assignCaseMenuItem = new ToolStripMenuItem();
            assignCaseMenuItem.Text = "Assign Case";
            assignCaseMenuItem.Click += new EventHandler(AssignCaseClickEventHandler);
            gvUnassignedCases.ContextMenuStrip.Items.Add(assignCaseMenuItem);

            var approveCaseMenuItem = new ToolStripMenuItem();
            approveCaseMenuItem.Text = "Approve Case";
            approveCaseMenuItem.Click += new EventHandler(ApproveCaseClickEventHandler);
            gvUnassignedCases.ContextMenuStrip.Items.Add(approveCaseMenuItem);
        }

        private void viewDetails_Click(object sender, EventArgs e)
        {
            Debug.WriteLine(gvUnassignedCases.CurrentRow.Cells[0].Value.ToString());
        }

        private void AssignCaseClickEventHandler(object sender, EventArgs e)
        {
            Case @case = _caseService.GetCase(gvUnassignedCases.CurrentRow.Cells[0].Value.ToString());
            AssignCaseForm form = FormFactory.CreateAssignCaseForm(@case);
            form.ShowDialog();
        }

        private async void ApproveCaseClickEventHandler(object sender, EventArgs e)
        {
            Case @case = _caseService.GetCase(gvUnassignedCases.CurrentRow.Cells[0].Value.ToString());
            _caseService.ApproveCase(@case.Id);

            await LoadCases();
            MessageBox.Show("Case approved.", "Approve Case", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
