using System.Windows.Forms;

namespace CaseManager.WinForms
{
    public partial class AnotherForm : Form
    {
        public AnotherForm(string message)
        {
            InitializeComponent();

            label1.Text = message;
        }
    }
}
