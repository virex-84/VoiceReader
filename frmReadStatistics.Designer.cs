namespace VoiceReader
{
    partial class frmReadStatistics
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmReadStatistics));
            btnClose = new Button();
            label1 = new Label();
            SuspendLayout();
            // 
            // btnClose
            // 
            btnClose.DialogResult = DialogResult.OK;
            resources.ApplyResources(btnClose, "btnClose");
            btnClose.Name = "btnClose";
            btnClose.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // frmReadStatistics
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(label1);
            Controls.Add(btnClose);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "frmReadStatistics";
            ResumeLayout(false);
        }

        #endregion
        private Button btnClose;
        private Label label1;
    }
}