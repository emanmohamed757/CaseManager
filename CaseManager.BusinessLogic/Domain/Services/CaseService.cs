using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Domain.Enums;
using CaseManager.BusinessLogic.Domain.Exceptions;
using CaseManager.BusinessLogic.Interfaces.Notification;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity;
using System.Linq;
using Serilog;

namespace CaseManager.BusinessLogic.Domain.Services
{
    public class CaseService
    {
        private readonly IDbContextFactory<CaseManagerDbContext> _caseManagerDbContextFactory;


        private readonly UserContext _userContext;

        private readonly ILogger _logger;

        private readonly INotificationService _notificationService;

        private readonly NextStatusService _nextStatusService;

        public CaseService(
            IDbContextFactory<CaseManagerDbContext> caseManagerDbContextFactory,
            UserContext userContext,
            ILogger logger,
            INotificationService notificationService,
            NextStatusService nextStatusService)
        {
            _caseManagerDbContextFactory = caseManagerDbContextFactory;
            _userContext = userContext;
            _logger = logger.ForContext<CaseService>();
            _notificationService = notificationService;
            _nextStatusService = nextStatusService;
        }

        // TODO: Should the presentation layer pass a DTO instead of the data/domain model itself? For now I am passing the data/domain model. Btw, is it "data" or "domain" model?
        public void CreateCase(Case @case)
        {
            // Initial status is "Proposed".
            @case.StatusId = (int)CaseStatusOption.Proposed;
            @case.DepartmentId = _userContext.DepartmentId;

            SetAuditProperties(@case);

            using (var dbContext = _caseManagerDbContextFactory.Create())
            {
                dbContext.Cases.Add(@case);
                dbContext.SaveChanges();
            }

            _logger.Information("Case created.");
        }

        public void ApproveCase(int caseId)
        {
            Case @case;
            using (var dbContext = _caseManagerDbContextFactory.Create())
            {
                @case = dbContext.Cases.Find(caseId);
                @case.StatusId = (int)CaseStatusOption.Approved;
                dbContext.SaveChanges();
            }

            _logger.Information($"Case (Id: {caseId}) approved.");

            _notificationService.Notify(
                "The case was approved",
                new string[] { @case.CreatedBy },
                null);
        }

        public List<Case> GetUnassignedCases()
        {
            using (var dbContext = _caseManagerDbContextFactory.Create())
            {
                IQueryable<Case> unassignedCasesQuery = dbContext.Cases
                    .Include(@case => @case.CaseStatus)
                    .Where(@case => @case.StatusId == (int)CaseStatusOption.Proposed
                        || @case.StatusId == (int)CaseStatusOption.Approved);

                _logger.Verbose("Checking whether user has permission to ViewAllCasesInTheDepartment.");
                bool canViewAllCasesInTheDepartment =
                    _userContext.HasPermission((int)PermissionOption.ViewAllUnassignedCasesInDepartment);

                if (canViewAllCasesInTheDepartment)
                {
                    _logger.Verbose("User has permission to ViewAllCasesInTheDepartment.");
                    // Filter by department of user.
                    unassignedCasesQuery = unassignedCasesQuery
                        .Where(@case => @case.DepartmentId == _userContext.DepartmentId);
                }
                else
                {
                    _logger.Verbose("User does not have permission to ViewAllCasesInTheDepartment.");
                    // Filter by created by user.
                    unassignedCasesQuery = unassignedCasesQuery
                        .Where(@case => @case.CreatedBy == _userContext.Username);
                }

                return unassignedCasesQuery.ToList();
            }
        }

        /// <summary>
        /// Audit properties are properties like created at, created by.
        /// </summary>
        private void SetAuditProperties(Case @case)
        {
            @case.CreatedAt = DateTime.Today;
            @case.CreatedBy = _userContext.Username;
            @case.UpdatedAt = DateTime.Today;
            @case.UpdatedBy = _userContext.Username;
        }

        public void RejectCase(int id)
        {
            Case @case;
            using (var dbContext = _caseManagerDbContextFactory.Create())
            {
                @case = dbContext.Cases.Find(id);
                @case.StatusId = (int)CaseStatusOption.Rejected;
                dbContext.SaveChanges();
            }

            _logger.Information($"Case (Id: {@case.Id}) rejected.");

            _notificationService.Notify(
                "The case was rejected",
                new string[] { @case.CreatedBy },
                null);
        }

        public void AssignCase(int caseId, string director, string manager)
        {
            Case @case;
            using (var dbContext = _caseManagerDbContextFactory.Create())
            {
                @case = dbContext.Cases.Find(caseId);

                // Only allow approved cases to be assigned.
                if (@case.StatusId != (int)CaseStatusOption.Approved)
                {
                    throw new CaseNotInApprovedStatusException();
                }

                // Find team leader and team assistant of the team of the given manager.
                Team managerTeam = dbContext.Teams
                    .Include(team => team.TeamMembers)
                    .First(team => team.SupervisorUsername == manager);
                TeamMember teamLeader = managerTeam.TeamMembers.First(member => member.IsTeamLeader);
                TeamMember teamAssistant = managerTeam.TeamMembers.First(member => !member.IsTeamLeader);

                // Set case members.
                @case.DirectorUsername = director;
                @case.ManagerUsername = manager;
                @case.TeamLeaderUsername = teamLeader.Username;
                @case.TeamAssistantUsername = teamAssistant.Username;

                // Change status.
                @case.StatusId = (int)CaseStatusOption.Assigned;

                dbContext.SaveChanges();
            }

            _logger.Information($"Case (Id: {@case.Id}) assigned.");

            _notificationService.Notify(
                "The case was assigned",
                new string[] { @case.TeamLeaderUsername },
                new string[] { @case.ManagerUsername, @case.TeamAssistantUsername, @case.DirectorUsername });
        }

        public List<Case> GetOngoingCases()
        {
            using (var dbContext = _caseManagerDbContextFactory.Create())
            {
                var ongoingCaseStatuses = new int[]
                {
                    (int)CaseStatusOption.Assigned,
                    (int)CaseStatusOption.Planning,
                    (int)CaseStatusOption.InProgress,
                    (int)CaseStatusOption.OnHold,
                    (int)CaseStatusOption.PendingReview,
                    (int)CaseStatusOption.Disputed,
                };

                IQueryable<Case> query = dbContext.Cases
                    .Where(@case => ongoingCaseStatuses.Contains(@case.StatusId));

                bool canUserViewAllCasesInDepartment = _userContext
                    .HasPermission((int)PermissionOption.ViewAllOngoingCasesInDepartment);
                if (canUserViewAllCasesInDepartment)
                {
                    query = query.Where(@case =>
                        @case.DepartmentId == _userContext.DepartmentId
                        || @case.DirectorUsername == _userContext.Username
                        || @case.ManagerUsername == _userContext.Username
                        || @case.TeamLeaderUsername == _userContext.Username
                        || @case.TeamAssistantUsername == _userContext.Username
                        || @case.CreatedBy == _userContext.Username);
                }
                else
                {
                    query = query.Where(@case =>
                        @case.DirectorUsername == _userContext.Username
                        || @case.ManagerUsername == _userContext.Username
                        || @case.TeamLeaderUsername == _userContext.Username
                        || @case.TeamAssistantUsername == _userContext.Username
                        || @case.CreatedBy == _userContext.Username);
                }

                return query.ToList();
            }
        }

        public void ChangeCaseStatus(int caseId, CaseStatusOption nextStatus)
        {
            using (var dbContext = _caseManagerDbContextFactory.Create())
            {
                Case @case = dbContext.Cases.Find(caseId);

                // Make sure that the requested status is valid for the case now.
                List<CaseStatusOption> nextStatuses = _nextStatusService.GetNextStatuses((CaseStatusOption)@case.StatusId);
                if (!nextStatuses.Contains(nextStatus))
                {
                    throw new InvalidStatusException();
                }

                @case.StatusId = (int)nextStatus;
                dbContext.SaveChanges();
            }
        }

        public List<Case> GetClosedCases()
        {
            using (var dbContext = _caseManagerDbContextFactory.Create())
            {
                return dbContext.Cases.Where(@case => @case.StatusId == (int)CaseStatusOption.Closed).ToList();
            }
        }
    }
}
