using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Data.HR;
using Effort.DataLoaders;
using Moq;
using System;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using Xunit;

namespace CaseManager.Tests
{
    public class AuthorizationServiceMethod
    {

        private readonly HRDbContext _arrangeHRDbContext;

        private readonly HRDbContext _assertHRDbContext;

        private readonly AuthorizationService _authorizationService;

        public AuthorizationServiceMethod()
        {
            // Create the connection that exists for the lifetime of this class, and hence the lifetime of EACH test.
            // dataLoader for loading static data.
            var dataLoader = new EntityDataLoader("name=HRDbContext");
            DbConnection connection = Effort.EntityConnectionFactory.CreateTransient("name=HRDbContext", dataLoader);

            _arrangeHRDbContext = new HRDbContext(connection);
            _assertHRDbContext = new HRDbContext(connection);

            var mockHRDbContextFactory = new Mock<IDbContextFactory<HRDbContext>>();
            mockHRDbContextFactory.Setup(x => x.Create()).Returns(() =>
            {
                return new HRDbContext(connection);
            });

            var dataLoader2 = new EntityDataLoader("name=CaseManagerDbContext");
            DbConnection connection2 = Effort.EntityConnectionFactory.CreateTransient("name=CaseManagerDbContext", dataLoader);
            var mockCaseManagerDbContextFactory = new Mock<IDbContextFactory<CaseManagerDbContext>>();
            mockCaseManagerDbContextFactory.Setup(x => x.Create()).Returns(() =>
            {
                return new CaseManagerDbContext(connection);
            });

            _authorizationService = new AuthorizationService(
                mockHRDbContextFactory.Object, 
                mockCaseManagerDbContextFactory.Object);
        }

        [Theory]
        [InlineData("a", "a", true)]
        [InlineData("a", "b", false)]
        [InlineData("b", "b", true)]
        [InlineData("b", "a", false)]
        public void Authorize_DoesNotAllowLoginUnlessUserIsInHRDatabase(
            string usernameInDb,
            string currentUsername,
            bool shouldAuthorized)
        {
            // Arrange.
            _arrangeHRDbContext.Employees.Add(new Employee
            {
                Username = usernameInDb,
                DepartmentId = 1,
                DesignationId = 1,
                IsDeleted = false,
                Name = usernameInDb,
            });
            _arrangeHRDbContext.SaveChanges();

            // Act.
            bool isAuthorized = _authorizationService.Authorize(currentUsername);

            // Assert.
            Assert.Equal(shouldAuthorized, isAuthorized);
        }
    }
}
