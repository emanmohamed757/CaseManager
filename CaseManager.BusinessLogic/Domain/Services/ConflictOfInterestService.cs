using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Data.HR;
using CaseManager.BusinessLogic.Domain.Enums;
using CaseManager.BusinessLogic.Domain.Exceptions;
using CaseManager.BusinessLogic.Interfaces.Notification;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace CaseManager.BusinessLogic.Domain.Services
{
    public class ConflictOfInterestService
    {
        private readonly IDbContextFactory<CaseManagerDbContext> _caseManagerDbContextFactory;

        private readonly IDbContextFactory<HRDbContext> _hrDbContextFactory;


        private readonly UserContext _userContext;

        private readonly INotificationService _notificationService;

        public ConflictOfInterestService(
            IDbContextFactory<CaseManagerDbContext> caseManagerDbContextFactory,
            IDbContextFactory<HRDbContext> hrDbContextFactory,
            UserContext userContext,
            INotificationService notificationService)
        {
            _caseManagerDbContextFactory = caseManagerDbContextFactory;
            _hrDbContextFactory = hrDbContextFactory;
            _userContext = userContext;
            _notificationService = notificationService;
        }

        public void DeclareConflictOfInterest(int caseId)
        {
            using (var dbContext = _caseManagerDbContextFactory.Create())
            {
                // If conflict is already declared, disallow another declaration.
                if (dbContext.ConflictOfInterests.Any(conflict => 
                    conflict.CaseId == caseId 
                    && conflict.Username == _userContext.Username))
                {
                    throw new ConfictOfInterestDeclarationException();
                }

                Employee employee;
                using (var hrDbContext = _hrDbContextFactory.Create())
                {
                    employee = hrDbContext.Employees.Find(_userContext.Username);
                }

                // Declare conflict of interest.
                var conflictOfInterest = new ConflictOfInterest
                {
                    CaseId = caseId,
                    Username = _userContext.Username,
                    DeclaredDate = DateTime.Now,
                    StaffDesignationId = employee.DesignationId,
                };
                dbContext.ConflictOfInterests.Add(conflictOfInterest);

                // Notifications and reassignment follow.
                var @case = dbContext.Cases.Find(caseId);

                if (employee.DesignationId == (int)DesignationOption.OperationalStaff)
                {
                    // If operational staff declared, ideally the manager should get the notification.
                    string manager = @case.ManagerUsername;

                    List<TeamMember> availableTeamMembers = GetAvailableTeamMembers(dbContext, manager, @case);

                    if (availableTeamMembers.Count > 1)
                    {
                        // More than one team member can be assigned. Notify manager.
                        _notificationService.Notify(
                            $"{_userContext.Username} has declared conflict of interest on case {@case.Id}. "
                                + $"Please reassign the case to another member of your team.",
                            new string[] { manager },
                            new string[] { _userContext.Username });
                        @case.IsAwaitingReassignment = true;
                    }
                    else if (availableTeamMembers.Count == 1)
                    {
                        // If the manager has only one staff member who does not have a conflict,
                        // then that staff member must be automatically assigned.
                        // Reassign by setting the appropriate property of the case.
                        if (_userContext.Username == @case.TeamLeaderUsername)
                        {
                            @case.TeamLeaderUsername = availableTeamMembers[0].Username;
                        }
                        else
                        {
                            @case.TeamAssistantUsername = availableTeamMembers[0].Username;
                        }

                        _notificationService.Notify(
                            $"{_userContext.Username} has declared conflict of interest on case {@case.Id}. "
                                + $"{availableTeamMembers[0].Username} has been automatically assigned to the case.",
                            new string[] { manager },
                            new string[] { _userContext.Username, availableTeamMembers[0].Username });
                    }
                    else
                    {
                        // If the manager has no staff to assign to, the director should be notified.
                        _notificationService.Notify(
                            $"{_userContext.Username} has declared conflict of interest on case {@case.Id}. "
                                + $"The supervisor has no staff to reassign to, so the reassignment has been escalated to you.",
                            new string[] { @case.DirectorUsername },
                            new string[] { @case.ManagerUsername, _userContext.Username });
                        @case.IsAwaitingReassignment = true;
                    }
                }

                dbContext.SaveChanges();
            }

            List<TeamMember> GetAvailableTeamMembers(CaseManagerDbContext dbContext, string manager, Case @case)
            {
                List<TeamMember> teamMembers = dbContext.TeamMembers
                                        .Where(member => member.Team.SupervisorUsername == manager)
                                        .ToList();

                List<string> usersWhoHaveConflict = dbContext.ConflictOfInterests
                    .Where(conflict => conflict.CaseId == caseId)
                    .Select(conflict => conflict.Username)
                    .ToList();
                List<TeamMember> availableTeamMembers = teamMembers
                    .Where(member => !usersWhoHaveConflict.Contains(member.Username)
                        && member.Username != _userContext.Username
                        && @case.TeamAssistantUsername != member.Username
                        && @case.TeamLeaderUsername != member.Username)
                    .ToList();
                return availableTeamMembers;
            }
        }
    }
}
