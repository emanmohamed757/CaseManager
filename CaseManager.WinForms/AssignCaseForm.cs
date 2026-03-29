using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Data.HR;
using CaseManager.BusinessLogic.Domain.Dtos;
using CaseManager.BusinessLogic.Domain.Exceptions;
using CaseManager.BusinessLogic.Domain.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CaseManager.WinForms
{
    public partial class AssignCaseForm : Form
    {
        private readonly int _caseId;

        private readonly TeamService _teamService;

        private readonly CaseService _caseService;

        private readonly UserContext _userContext;

        public AssignCaseForm(int caseId, TeamService teamService, CaseService caseService, UserContext userContext)
        {
            InitializeComponent();
            _caseId = caseId;
            _teamService = teamService;
            _caseService = caseService;
            _userContext = userContext;
        }

        private async void AssignCaseForm_Load(object sender, EventArgs e)
        {
            List<Employee> subordinates = await _teamService.GetImmediateSubordinatesWithFullName(_userContext.Username);
            ddManager.DisplayMember = nameof(Employee.Name);
            ddManager.ValueMember = nameof(Employee.Username);
            ddManager.DataSource = subordinates;

            Case @case = await _caseService.GetCase(_caseId);
            lblCaseNumber.Text = $"Assigning Case {@case.CaseNumber}";
        }

        private async void btnAssignCase_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.AppStarting;

            CaseAssignmentResponse response;
            try
            {
                string manager = ddManager.SelectedValue.ToString();
                response = await Task.Run(() =>
                    _caseService.AssignCase(_caseId, _userContext.Username, manager));
            }
            catch (CaseNotInApprovedStatusException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Assign Case",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Cursor = Cursors.Default;

            MessageBox.Show(
                $"Case assigned." +
                    $"\nDirector: {response.DirectorUsername}" +
                    $"\nManager: {response.ManagerUsername}" +
                    $"\nTeam Leader: {response.TeamLeaderUsername}" +
                    $"\nTeamAssistant: {response.TeamAssistantUsername}",
                "Assign Case",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // Close the form.
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
