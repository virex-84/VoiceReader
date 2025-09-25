namespace VoiceReader
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            cbMicrofone = new ComboBox();
            tbFile = new TextBox();
            btnFile = new Button();
            tbUserText = new TextBox();
            voiceReaderComponent1 = new VoiceReaderComponent();
            toolStrip1 = new ToolStrip();
            tsFontSizePlus = new ToolStripButton();
            tsFontSizeMinus = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            tsLeft = new ToolStripButton();
            tsCenter = new ToolStripButton();
            tsRight = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            tsAutoScroll = new ToolStripButton();
            toolStripSeparator3 = new ToolStripSeparator();
            tsShowStatistics = new ToolStripButton();
            toolStripSeparator4 = new ToolStripSeparator();
            tsModels = new ToolStripButton();
            btnRecognize = new Button();
            imageList1 = new ImageList(components);
            lbPath = new Label();
            lbMicrofone = new Label();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // cbMicrofone
            // 
            resources.ApplyResources(cbMicrofone, "cbMicrofone");
            cbMicrofone.DropDownStyle = ComboBoxStyle.DropDownList;
            cbMicrofone.FormattingEnabled = true;
            cbMicrofone.Name = "cbMicrofone";
            // 
            // tbFile
            // 
            resources.ApplyResources(tbFile, "tbFile");
            tbFile.Name = "tbFile";
            tbFile.ReadOnly = true;
            // 
            // btnFile
            // 
            resources.ApplyResources(btnFile, "btnFile");
            btnFile.Name = "btnFile";
            btnFile.UseVisualStyleBackColor = true;
            btnFile.Click += btnFile_Click;
            // 
            // tbUserText
            // 
            resources.ApplyResources(tbUserText, "tbUserText");
            tbUserText.Name = "tbUserText";
            tbUserText.ReadOnly = true;
            // 
            // voiceReaderComponent1
            // 
            resources.ApplyResources(voiceReaderComponent1, "voiceReaderComponent1");
            voiceReaderComponent1.BackColor = Color.FloralWhite;
            voiceReaderComponent1.ForeColor = Color.Black;
            voiceReaderComponent1.Name = "voiceReaderComponent1";
            voiceReaderComponent1.ScrollBars = ScrollBars.Vertical;
            voiceReaderComponent1.OnLinkClick += voiceReaderComponent1_OnLinkClick;
            voiceReaderComponent1.AllHighlighted += voiceReaderComponent1_AllHighlighted;
            voiceReaderComponent1.WordClick += voiceReaderComponent1_WordClick;
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new ToolStripItem[] { tsFontSizePlus, tsFontSizeMinus, toolStripSeparator1, tsLeft, tsCenter, tsRight, toolStripSeparator2, tsAutoScroll, toolStripSeparator3, tsShowStatistics, toolStripSeparator4, tsModels });
            resources.ApplyResources(toolStrip1, "toolStrip1");
            toolStrip1.Name = "toolStrip1";
            toolStrip1.RenderMode = ToolStripRenderMode.System;
            toolStrip1.ItemClicked += toolStrip1_ItemClicked;
            // 
            // tsFontSizePlus
            // 
            tsFontSizePlus.DisplayStyle = ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(tsFontSizePlus, "tsFontSizePlus");
            tsFontSizePlus.Name = "tsFontSizePlus";
            // 
            // tsFontSizeMinus
            // 
            tsFontSizeMinus.DisplayStyle = ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(tsFontSizeMinus, "tsFontSizeMinus");
            tsFontSizeMinus.Name = "tsFontSizeMinus";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(toolStripSeparator1, "toolStripSeparator1");
            // 
            // tsLeft
            // 
            tsLeft.DisplayStyle = ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(tsLeft, "tsLeft");
            tsLeft.Name = "tsLeft";
            // 
            // tsCenter
            // 
            tsCenter.DisplayStyle = ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(tsCenter, "tsCenter");
            tsCenter.Name = "tsCenter";
            // 
            // tsRight
            // 
            tsRight.DisplayStyle = ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(tsRight, "tsRight");
            tsRight.Name = "tsRight";
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(toolStripSeparator2, "toolStripSeparator2");
            // 
            // tsAutoScroll
            // 
            tsAutoScroll.Checked = true;
            tsAutoScroll.CheckState = CheckState.Indeterminate;
            tsAutoScroll.DisplayStyle = ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(tsAutoScroll, "tsAutoScroll");
            tsAutoScroll.Name = "tsAutoScroll";
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            resources.ApplyResources(toolStripSeparator3, "toolStripSeparator3");
            // 
            // tsShowStatistics
            // 
            tsShowStatistics.CheckOnClick = true;
            tsShowStatistics.DisplayStyle = ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(tsShowStatistics, "tsShowStatistics");
            tsShowStatistics.Name = "tsShowStatistics";
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            resources.ApplyResources(toolStripSeparator4, "toolStripSeparator4");
            // 
            // tsModels
            // 
            tsModels.DisplayStyle = ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(tsModels, "tsModels");
            tsModels.Name = "tsModels";
            // 
            // btnRecognize
            // 
            resources.ApplyResources(btnRecognize, "btnRecognize");
            btnRecognize.ImageList = imageList1;
            btnRecognize.Name = "btnRecognize";
            btnRecognize.UseVisualStyleBackColor = true;
            btnRecognize.Click += btnRecognize_Click;
            // 
            // imageList1
            // 
            imageList1.ColorDepth = ColorDepth.Depth32Bit;
            imageList1.ImageStream = (ImageListStreamer)resources.GetObject("imageList1.ImageStream");
            imageList1.TransparentColor = Color.Transparent;
            imageList1.Images.SetKeyName(0, "microphone.png");
            imageList1.Images.SetKeyName(1, "stop.png");
            // 
            // lbPath
            // 
            resources.ApplyResources(lbPath, "lbPath");
            lbPath.Name = "lbPath";
            // 
            // lbMicrofone
            // 
            resources.ApplyResources(lbMicrofone, "lbMicrofone");
            lbMicrofone.Name = "lbMicrofone";
            // 
            // frmMain
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(lbMicrofone);
            Controls.Add(lbPath);
            Controls.Add(btnRecognize);
            Controls.Add(toolStrip1);
            Controls.Add(voiceReaderComponent1);
            Controls.Add(tbUserText);
            Controls.Add(btnFile);
            Controls.Add(tbFile);
            Controls.Add(cbMicrofone);
            Name = "frmMain";
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ComboBox cbMicrofone;
        private TextBox tbFile;
        private Button btnFile;
        private TextBox tbUserText;
        private VoiceReaderComponent voiceReaderComponent1;
        private ToolStrip toolStrip1;
        private ToolStripButton tsFontSizePlus;
        private ToolStripButton tsFontSizeMinus;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton tsLeft;
        private ToolStripButton tsCenter;
        private ToolStripButton tsRight;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton tsAutoScroll;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripButton tsShowStatistics;
        private Button btnRecognize;
        private Label lbPath;
        private Label lbMicrofone;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripButton tsModels;
        private ImageList imageList1;
    }
}
