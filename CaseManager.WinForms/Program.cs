using Autofac;
using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Data.HR;
using CaseManager.BusinessLogic.Domain.Services;
using CaseManager.BusinessLogic.Interfaces.Logging;
using CaseManager.BusinessLogic.Interfaces.Notification;
using CaseManager.Infrastructure.Logging;
using CaseManager.Infrastructure.Notification;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CaseManager.WinForms
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var builder = new ContainerBuilder();
            RegisterServices(builder);
            RegisterForms(builder);
            var container = builder.Build();

            FormFactory.SetContainer(container);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            LoginForm loginForm = FormFactory.CreateLoginForm();

            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                Application.Run(FormFactory.CreateMainForm());
            }
        }

        private static void RegisterServices(ContainerBuilder builder)
        {
            builder.RegisterType<UserContext>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<AuthorizationService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<CaseService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<NextStatusService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<DbContextFactory<HRDbContext>>().As<IDbContextFactory<HRDbContext>>().InstancePerLifetimeScope();
            builder.RegisterType<DbContextFactory<CaseManagerDbContext>>().As<IDbContextFactory<CaseManagerDbContext>>().InstancePerLifetimeScope();
            builder.RegisterType<EFLogger>().As<ILogger>().InstancePerLifetimeScope();
            builder.RegisterType<EFNotificationService>().As<INotificationService>().InstancePerLifetimeScope();
        }

        private static void RegisterForms(ContainerBuilder builder)
        {
            builder.RegisterType<LoginForm>();
            builder.RegisterType<MainForm>();
        }
    }
}
