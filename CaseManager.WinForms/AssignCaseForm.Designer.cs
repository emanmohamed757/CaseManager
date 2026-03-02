namespace CaseManager.WinForms
{
    partial class AssignCaseForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblCaseNumber = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.ddManager = new System.Windows.Forms.ComboBox();
            this.btnAssignCase = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblCaseNumber
            // 
            this.lblCaseNumber.AutoSize = true;
            this.lblCaseNumber.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCaseNumber.Location = new System.Drawing.Point(30, 25);
            this.lblCaseNumber.Name = "lblCaseNumber";
            this.lblCaseNumber.Size = new System.Drawing.Size(150, 25);
            this.lblCaseNumber.TabIndex = 0;
            this.lblCaseNumber.Text = "Assigning Case";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(32, 62);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(111, 18);
            this.label1.TabIndex = 1;
            this.label1.Text = "Select Manager";
            // 
            // ddManager
            // 
            this.ddManager.FormattingEnabled = true;
            this.ddManager.Location = new System.Drawing.Point(35, 87);
            this.ddManager.Name = "ddManager";
            this.ddManager.Size = new System.Drawing.Size(369, 21);
            this.ddManager.TabIndex = 3;
            // 
            // btnAssignCase
            // 
            this.btnAssignCase.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAssignCase.Location = new System.Drawing.Point(281, 129);
            this.btnAssignCase.Name = "btnAssignCase";
            this.btnAssignCase.Size = new System.Drawing.Size(123, 26);
            this.btnAssignCase.TabIndex = 4;
            this.btnAssignCase.Text = "Assign Case";
            this.btnAssignCase.UseVisualStyleBackColor = true;
            this.btnAssignCase.Click += new System.EventHandler(this.btnAssignCase_Click);
            // 
            // AssignCaseForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(444, 177);
            this.Controls.Add(this.btnAssignCase);
            this.Controls.Add(this.ddManager);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblCaseNumber);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "AssignCaseForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Assign Case";
            this.Load += new System.EventHandler(this.AssignCaseForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblCaseNumber;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox ddManager;
        private System.Windows.Forms.Button btnAssignCase;
    }
}