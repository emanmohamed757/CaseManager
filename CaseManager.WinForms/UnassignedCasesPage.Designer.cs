namespace CaseManager.WinForms
{
    partial class UnassignedCasesPage
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.gvUnassignedCases = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.gvUnassignedCases)).BeginInit();
            this.SuspendLayout();
            // 
            // gvUnassignedCases
            // 
            this.gvUnassignedCases.AllowUserToAddRows = false;
            this.gvUnassignedCases.AllowUserToDeleteRows = false;
            this.gvUnassignedCases.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gvUnassignedCases.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gvUnassignedCases.Location = new System.Drawing.Point(0, 0);
            this.gvUnassignedCases.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.gvUnassignedCases.Name = "gvUnassignedCases";
            this.gvUnassignedCases.ReadOnly = true;
            this.gvUnassignedCases.RowHeadersWidth = 51;
            this.gvUnassignedCases.RowTemplate.Height = 24;
            this.gvUnassignedCases.Size = new System.Drawing.Size(536, 388);
            this.gvUnassignedCases.TabIndex = 0;
            this.gvUnassignedCases.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gvUnassignedCases_CellClick);
            // 
            // UnassignedCasesPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gvUnassignedCases);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "UnassignedCasesPage";
            this.Size = new System.Drawing.Size(536, 388);
            ((System.ComponentModel.ISupportInitialize)(this.gvUnassignedCases)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView gvUnassignedCases;
    }
}
