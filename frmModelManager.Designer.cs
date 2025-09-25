namespace VoiceReader
{
    partial class frmModelManager
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmModelManager));
            listViewModels = new ListView();
            columnHeaderName = new ColumnHeader();
            columnHeaderLanguage = new ColumnHeader();
            columnHeaderSize = new ColumnHeader();
            columnHeaderStatus = new ColumnHeader();
            progressBarDownload = new ProgressBar();
            lblProgress = new Label();
            btnRefresh = new Button();
            btnSelect = new Button();
            btnCancel = new Button();
            lblFilter = new Label();
            cmbLanguageFilter = new ComboBox();
            chkHideObsolete = new CheckBox();
            chkShowLoaded = new CheckBox();
            SuspendLayout();
            // 
            // listViewModels
            // 
            listViewModels.CheckBoxes = true;
            listViewModels.Columns.AddRange(new ColumnHeader[] { columnHeaderName, columnHeaderLanguage, columnHeaderSize, columnHeaderStatus });
            listViewModels.FullRowSelect = true;
            listViewModels.GridLines = true;
            resources.ApplyResources(listViewModels, "listViewModels");
            listViewModels.Name = "listViewModels";
            listViewModels.UseCompatibleStateImageBehavior = false;
            listViewModels.View = View.Details;
            listViewModels.ItemCheck += listViewModels_ItemCheck;
            // 
            // columnHeaderName
            // 
            resources.ApplyResources(columnHeaderName, "columnHeaderName");
            // 
            // columnHeaderLanguage
            // 
            resources.ApplyResources(columnHeaderLanguage, "columnHeaderLanguage");
            // 
            // columnHeaderSize
            // 
            resources.ApplyResources(columnHeaderSize, "columnHeaderSize");
            // 
            // columnHeaderStatus
            // 
            resources.ApplyResources(columnHeaderStatus, "columnHeaderStatus");
            // 
            // progressBarDownload
            // 
            resources.ApplyResources(progressBarDownload, "progressBarDownload");
            progressBarDownload.Name = "progressBarDownload";
            // 
            // lblProgress
            // 
            resources.ApplyResources(lblProgress, "lblProgress");
            lblProgress.Name = "lblProgress";
            // 
            // btnRefresh
            // 
            resources.ApplyResources(btnRefresh, "btnRefresh");
            btnRefresh.Name = "btnRefresh";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // btnSelect
            // 
            btnSelect.DialogResult = DialogResult.OK;
            resources.ApplyResources(btnSelect, "btnSelect");
            btnSelect.Name = "btnSelect";
            btnSelect.UseVisualStyleBackColor = true;
            btnSelect.Click += btnSelect_Click;
            // 
            // btnCancel
            // 
            resources.ApplyResources(btnCancel, "btnCancel");
            btnCancel.Name = "btnCancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // lblFilter
            // 
            resources.ApplyResources(lblFilter, "lblFilter");
            lblFilter.Name = "lblFilter";
            // 
            // cmbLanguageFilter
            // 
            cmbLanguageFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLanguageFilter.FormattingEnabled = true;
            resources.ApplyResources(cmbLanguageFilter, "cmbLanguageFilter");
            cmbLanguageFilter.Name = "cmbLanguageFilter";
            cmbLanguageFilter.SelectedIndexChanged += cmbLanguageFilter_SelectedIndexChanged;
            // 
            // chkHideObsolete
            // 
            resources.ApplyResources(chkHideObsolete, "chkHideObsolete");
            chkHideObsolete.Checked = true;
            chkHideObsolete.CheckState = CheckState.Checked;
            chkHideObsolete.Name = "chkHideObsolete";
            chkHideObsolete.UseVisualStyleBackColor = true;
            chkHideObsolete.CheckedChanged += chkHideObsolete_CheckedChanged;
            // 
            // chkShowLoaded
            // 
            resources.ApplyResources(chkShowLoaded, "chkShowLoaded");
            chkShowLoaded.Name = "chkShowLoaded";
            chkShowLoaded.UseVisualStyleBackColor = true;
            chkShowLoaded.CheckedChanged += chkShowLoaded_CheckedChanged;
            // 
            // frmModelManager
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(chkShowLoaded);
            Controls.Add(chkHideObsolete);
            Controls.Add(cmbLanguageFilter);
            Controls.Add(lblFilter);
            Controls.Add(btnSelect);
            Controls.Add(btnCancel);
            Controls.Add(btnRefresh);
            Controls.Add(lblProgress);
            Controls.Add(progressBarDownload);
            Controls.Add(listViewModels);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmModelManager";
            ShowInTaskbar = false;
            Load += frmModelManager_Load;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListView listViewModels;
        private System.Windows.Forms.ColumnHeader columnHeaderName;
        private System.Windows.Forms.ColumnHeader columnHeaderLanguage;
        private System.Windows.Forms.ColumnHeader columnHeaderSize;
        private System.Windows.Forms.ColumnHeader columnHeaderStatus;
        private System.Windows.Forms.ProgressBar progressBarDownload;
        private System.Windows.Forms.Label lblProgress;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnSelect;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblFilter;
        private System.Windows.Forms.ComboBox cmbLanguageFilter;
        private System.Windows.Forms.CheckBox chkHideObsolete;
        private CheckBox chkShowLoaded;
    }
}