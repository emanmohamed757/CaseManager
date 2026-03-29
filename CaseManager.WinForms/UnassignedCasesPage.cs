using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Domain.Dtos;
using CaseManager.BusinessLogic.Domain.Enums;
using CaseManager.BusinessLogic.Domain.Exceptions;
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
    public partial class UnassignedCasesPage : UserControl
    {
        private CaseService _caseService;

        private UserContext _userContext;

        private ILogger _logger;

        private ToolStripMenuItem _assignCaseMenuItem = new ToolStripMenuItem();

        private ToolStripMenuItem _approveCaseMenuItem = new ToolStripMenuItem();

        private List<UnassignedCaseViewModel> _caseViewModels = new List<UnassignedCaseViewModel>();

        public UnassignedCasesPage()
        {
            InitializeComponent();
            AddContextMenu();
        }

        public void Setup(CaseService caseService, UserContext userContext, ILogger logger)
        {
            _caseService = caseService;
            _userContext = userContext;
            _logger = logger.ForContext<UnassignedCasesPage>();
        }

        public async Task LoadCases()
        {
            _logger.Verbose("Loading unassigned cases.");

            List<CaseDto> cases = await Task.Run(() => _caseService.GetUnassignedCases());

            _logger.Verbose("Changing loaded unassigned cases to UnassignedCaseViewModel.");

            _caseViewModels = cases.Select(@case =>
                new UnassignedCaseViewModel
                {
                    Id = @case.Id,
                    CaseNumber = @case.CaseNumber,
                    CreatedAt = @case.CreatedAt,
                    CreatedBy = @case.CreatedBy,
                    DepartmentId = @case.DepartmentId,
                    DepartmentName = @case.DepartmentName,
                    IsDeleted = @case.IsDeleted,
                    StatusId = @case.StatusId,
                    UpdatedAt = @case.UpdatedAt,
                    UpdatedBy = @case.UpdatedBy,
                    StatusName = @case.CaseStatus.Name
                })
                .ToList();

            _logger.Verbose("Setting gvUnassignedCases.DataSource to caseViewModels.");

            gvUnassignedCases.DataSource = _caseViewModels;
        }

        private void AddContextMenu()
        {
            ContextMenuStrip strip = new ContextMenuStrip();
            gvUnassignedCases.ContextMenuStrip = strip;

            var menuItem = new ToolStripMenuItem();
            menuItem.Text = "View Details";
            menuItem.Click += new EventHandler(viewDetails_Click);
            gvUnassignedCases.ContextMenuStrip.Items.Add(menuItem);

            _assignCaseMenuItem.Text = "Assign Case";
            _assignCaseMenuItem.Click += new EventHandler(AssignCaseClickEventHandler);
            gvUnassignedCases.ContextMenuStrip.Items.Add(_assignCaseMenuItem);

            _approveCaseMenuItem.Text = "Approve Case";
            _approveCaseMenuItem.Click += new EventHandler(ApproveCaseClickEventHandler);
            gvUnassignedCases.ContextMenuStrip.Items.Add(_approveCaseMenuItem);
        }

        private void viewDetails_Click(object sender, EventArgs e)
        {
            //Debug.WriteLine(gvUnassignedCases.CurrentRow.Cells[0].Value.ToString());
        }

        private async void AssignCaseClickEventHandler(object sender, EventArgs e)
        {
            UnassignedCaseViewModel @case = GetSelectedCase();

            if (@case.StatusId != (int)CaseStatusOption.Approved)
            {
                MessageBox.Show(
                    "The case must be approved before it can be assigned.",
                    "Assign Case",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            AssignCaseForm form = FormFactory.CreateAssignCaseForm(@case.Id);

            if (form.ShowDialog() == DialogResult.OK)
            {
                await LoadCases();
            }
        }

        private async void ApproveCaseClickEventHandler(object sender, EventArgs e)
        {
            try
            {
                UnassignedCaseViewModel @case = GetSelectedCase();
                _caseService.ApproveCase(@case.Id);

                await LoadCases();
                MessageBox.Show("Case approved.", "Approve Case", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (CaseNotInProposedStatusException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Approve Case",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void gvUnassignedCases_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            UnassignedCaseViewModel @case = GetSelectedCase();
            _approveCaseMenuItem.Visible = @case.StatusId == (int)CaseStatusOption.Proposed;
            _assignCaseMenuItem.Visible = @case.StatusId == (int)CaseStatusOption.Approved;
        }

        private UnassignedCaseViewModel GetSelectedCase()
        {
            string caseNumber = gvUnassignedCases.CurrentRow.Cells[0].Value.ToString();
            return _caseViewModels.FirstOrDefault(c => c.CaseNumber == caseNumber);
        }
    }
}
