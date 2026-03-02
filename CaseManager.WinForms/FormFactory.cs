using Autofac;
using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Domain.Services;
using System;
using System.Windows.Forms;

namespace CaseManager.WinForms
{
    internal static class FormFactory
    {
        static IContainer _container;
        
        public static void SetContainer(IContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// Instantiates a Form that does not take free parameters (parameters that are not injected by the DI container).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Create<T>()
            where T : Form
        {
            ILifetimeScope scope = _container.BeginLifetimeScope();

            var form = scope.Resolve<T>();

            form.FormClosed += (_, __) => scope.Dispose();
            return form;
        }

        public static AnotherForm CreateAnotherForm(string message)
        {
            ILifetimeScope scope = _container.BeginLifetimeScope();

            var form = _container.Resolve<AnotherForm>(
                new TypedParameter(typeof(string), message));

            form.FormClosed += (_, __) => scope.Dispose();
            return form;
        }

        public static AssignCaseForm CreateAssignCaseForm(Case @case)
        {
            ILifetimeScope scope = _container.BeginLifetimeScope();

            var form = _container.Resolve<AssignCaseForm>(
                new TypedParameter(typeof(Case), @case));

            form.FormClosed += (_, __) => scope.Dispose();
            return form;
        }
    }
}
