namespace VoiceReader
{
    public partial class frmModelManager : Form
    {
        private VoskModelManager modelManager;
        private List<VoskModel> allModels = new List<VoskModel>();
        private List<VoskModel> filteredModels = new List<VoskModel>();
        private CancellationTokenSource? downloadCancellationTokenSource;
        private bool isDownloading = false;
        private bool isSelectionMode = false;
        private string lang = ""; //фильтр по языку

        public string? SelectedModelPath { get; private set; }

        private static string getString(string name)
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(frmModelManager));
            return resources.GetString(name);
        }

        public frmModelManager() : this(false, "")
        {
        }

        public frmModelManager(bool selectionMode, string filterLang)
        {
            InitializeComponent();
            isSelectionMode = selectionMode;
            lang = filterLang;
            modelManager = new VoskModelManager();

            // Subscribe to events
            modelManager.DownloadProgressChanged += ModelManager_DownloadProgressChanged;
            modelManager.ModelStatusChanged += ModelManager_ModelStatusChanged;

            SetupUI();
        }

        private void SetupUI()
        {
            // Hide progress controls initially
            progressBarDownload.Visible = false;
            lblProgress.Visible = false;
            btnCancel.Visible = false;

            // Setup language filter
            cmbLanguageFilter.Items.Add("All Languages");
            cmbLanguageFilter.SelectedIndex = 0;

            // Configure UI based on mode
            if (isSelectionMode)
            {
                chkShowLoaded.Checked = true;
                btnSelect.Text = getString("select");
                listViewModels.CheckBoxes = true; // Keep checkboxes for downloading new models
                listViewModels.MultiSelect = false;
                listViewModels.FullRowSelect = true;
                
                // Show detected language info if specified
                if (!string.IsNullOrEmpty(lang))
                {
                    this.Text += $" - Detected: {lang}";
                }
            }
            else
            {
                chkShowLoaded.Checked = false;
                btnSelect.Text = getString("close");
                listViewModels.CheckBoxes = true;
                listViewModels.MultiSelect = false;
            }

            // Setup ListView columns
            columnHeaderName.Text = getString("model");
            columnHeaderLanguage.Text = getString("language");
            columnHeaderSize.Text = getString("size");
            columnHeaderStatus.Text = getString("status");

            columnHeaderName.Width = 300;
            columnHeaderLanguage.Width = 150;
            columnHeaderSize.Width = 100;
            columnHeaderStatus.Width = 120;
        }

        private async void frmModelManager_Load(object sender, EventArgs e)
        {
            await LoadModelsAsync();
        }

        private async Task LoadModelsAsync()
        {
            try
            {
                btnRefresh.Enabled = false;
                btnSelect.Enabled = false;
                btnRefresh.Text = getString("loading");

                allModels = await modelManager.GetAvailableModelsAsync();
                UpdateTitleWithLanguageInfo();
                PopulateLanguageFilter();
                FilterAndDisplayModels();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load models: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Enabled = true;
                btnRefresh.Text = getString("refresh");
                btnSelect.Enabled = true;
            }
        }

        private void UpdateTitleWithLanguageInfo()
        {
            // Update title with language matching info (only in selection mode)
            if (isSelectionMode && !string.IsNullOrEmpty(lang))
            {
                // Convert detected language to display name
                var langName = GetLanguageName(lang);
                
                // Check what will actually be selected in the combo box
                string actualSelection;
                //небольшой хак: нет модели English, есть US English, UK English. выбираем по умолчанию US English
                if (langName == "English" && allModels.Where(x => x.LanguageText.Contains("US English")).Count() > 0)
                {
                    actualSelection = "US English";
                }
                else if (!string.IsNullOrEmpty(langName) && cmbLanguageFilter.Items.Contains(langName))
                {
                    actualSelection = langName;
                }
                else
                {
                    actualSelection = langName;
                }
                
                // Check if the language has matching models
                var hasMatchingModels = !string.IsNullOrEmpty(actualSelection) && 
                    allModels.Any(m => m.LanguageText.Equals(actualSelection, StringComparison.OrdinalIgnoreCase));
                
                // Remove the previous detected info from title if exists
                var baseTitle = this.Text.Split(new[] { " - Detected:" }, StringSplitOptions.None)[0];
                
                if (hasMatchingModels)
                {
                    this.Text = baseTitle + $" - Detected: {actualSelection}";
                }
                else
                {
                    var displayLang = !string.IsNullOrEmpty(actualSelection) ? actualSelection : lang;
                    this.Text = baseTitle + $" - Detected: {displayLang} (no models found, showing all)";
                }
            }
        }

        private void PopulateLanguageFilter()
        {
            var languages = allModels
                .Where(m => !m.IsObsolete || !chkHideObsolete.Checked)
                .Select(m => m.LanguageText)
                .Distinct()
                .OrderBy(l => l)
                .ToList();

            var currentSelection = cmbLanguageFilter.SelectedItem?.ToString();

            cmbLanguageFilter.Items.Clear();
            cmbLanguageFilter.Items.Add("All Languages");

            foreach (var language in languages)
            {
                cmbLanguageFilter.Items.Add(language);
            }

            // Apply initial language filter only on first load
            if (!string.IsNullOrEmpty(lang) && currentSelection == "All Languages")
            {
                // Convert detected language code to display name using CultureInfo
                var langName = GetLanguageName(lang);
                
                // Special case: when English is detected, prefer US English
                if (langName == "English" && cmbLanguageFilter.Items.Contains("US English"))
                {
                    cmbLanguageFilter.SelectedItem = "US English";
                }
                // Try to find matching language by display name
                else if (!string.IsNullOrEmpty(langName) && cmbLanguageFilter.Items.Contains(langName))
                {
                    cmbLanguageFilter.SelectedItem = langName;
                }
                else
                {
                    // If no matching language found, default to "All Languages" to show all models
                    cmbLanguageFilter.SelectedIndex = 0; // "All Languages" - safer fallback
                }
            }
            // Restore previous selection (user has changed filter)
            else if (!string.IsNullOrEmpty(currentSelection) && cmbLanguageFilter.Items.Contains(currentSelection))
            {
                cmbLanguageFilter.SelectedItem = currentSelection;
            }
            else
            {
                cmbLanguageFilter.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Converts language ISO code to display name using CultureInfo
        /// </summary>
        private string GetLanguageName(string isoCode)
        {
            if (string.IsNullOrEmpty(isoCode))
                return string.Empty;

            var result = string.Empty;

            try
            {
                var cultures = System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.AllCultures);
                
                // Try to find by 3-letter ISO code first
                var culture = cultures.FirstOrDefault(c => 
                    c.ThreeLetterISOLanguageName.Equals(isoCode, StringComparison.OrdinalIgnoreCase));
                
                if (culture != null)
                {
                    result = culture.EnglishName;
                }
                
                // Try to find by 2-letter ISO code as fallback
                culture = cultures.FirstOrDefault(c => 
                    c.TwoLetterISOLanguageName.Equals(isoCode, StringComparison.OrdinalIgnoreCase));
                    
                if (culture != null)
                {
                    result =  culture.EnglishName;
                }
            }
            catch
            {
                return string.Empty;
            }

            return result;
        }

        private void FilterAndDisplayModels()
        {
            var selectedLanguage = cmbLanguageFilter.SelectedItem?.ToString();

            filteredModels = allModels.Where(m =>
            {
                // Filter by obsolete
                if (chkHideObsolete.Checked && m.IsObsolete)
                    return false;

                // Filter by language
                if (selectedLanguage != "All Languages" && m.LanguageText != selectedLanguage)
                    return false;

                // Filter by only downloaded
                if (chkShowLoaded.Checked && string.IsNullOrEmpty(m.LocalPath))
                    return false;

                return true;
            }).OrderBy(m => m.LanguageText).ThenBy(m => m.Name).ToList();

            DisplayModels();
        }

        private void DisplayModels()
        {
            listViewModels.Items.Clear();

            foreach (var model in filteredModels)
            {
                var item = new ListViewItem(model.Name)
                {
                    Tag = model,
                    Checked = false // No active model concept
                };

                item.SubItems.Add(model.LanguageText);
                item.SubItems.Add(model.SizeText);
                item.SubItems.Add(GetStatusText(model));

                // Set different colors based on status
                switch (model.Status)
                {
                    case ModelStatus.Available:
                        item.ForeColor = Color.Black;
                        break;
                    case ModelStatus.Downloaded:
                        item.ForeColor = Color.Blue;
                        break;
                    case ModelStatus.Downloading:
                        item.ForeColor = Color.Orange;
                        break;
                }

                listViewModels.Items.Add(item);
            }

            // In selection mode, select first downloaded model by default
            if (isSelectionMode && listViewModels.Items.Count > 0)
            {
                var firstDownloaded = listViewModels.Items.Cast<ListViewItem>()
                    .FirstOrDefault(i => ((VoskModel)i.Tag).Status == ModelStatus.Downloaded);
                if (firstDownloaded != null)
                {
                    firstDownloaded.Selected = true;
                    firstDownloaded.Focused = true;
                }
            }

        }

        private string GetStatusText(VoskModel model)
        {
            return model.Status switch
            {
                ModelStatus.Available => getString("status_available"),
                ModelStatus.Downloaded => getString("status_downloaded"),
                ModelStatus.Downloading => getString("status_downloading"),
                _ => getString("status_unknown")
            };
        }

        private async void listViewModels_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (isDownloading)
            {
                e.NewValue = e.CurrentValue; // Prevent change during download
                return;
            }

            var item = listViewModels.Items[e.Index];
            var model = (VoskModel)item.Tag;

            if (e.NewValue == CheckState.Checked)
            {
                try
                {
                    if (model.Status == ModelStatus.Available)
                    {
                        // Need to download first
                        var result = MessageBox.Show(
                            //$"Model '{model.Name}' is not downloaded. Download it now? (Size: {model.SizeText})",
                            string.Format(getString("ask_download_model_text"), model.Name, model.SizeText),
                            //"Download Required",
                            getString("ask_download_model_title"),
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            await DownloadModelAsync(model);
                        }
                        else
                        {
                            e.NewValue = e.CurrentValue; // Cancel the check
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to download model: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.NewValue = e.CurrentValue; // Cancel the check
                }
            }
        }

        private async Task DownloadModelAsync(VoskModel model)
        {
            try
            {
                isDownloading = true;
                downloadCancellationTokenSource = new CancellationTokenSource();

                // Show progress controls
                progressBarDownload.Visible = true;
                lblProgress.Visible = true;
                btnCancel.Visible = true;
                progressBarDownload.Value = 0;
                lblProgress.Text = $"Downloading {model.Name}...";

                // Disable form controls
                listViewModels.Enabled = false;
                btnRefresh.Enabled = false;
                btnSelect.Enabled = false;

                await modelManager.DownloadModelAsync(model, downloadCancellationTokenSource.Token);

                lblProgress.Text = "Download completed successfully!";

                // Wait a moment before hiding progress
                await Task.Delay(1000);
            }
            catch (OperationCanceledException)
            {
                lblProgress.Text = "Download cancelled.";
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Download failed: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Hide progress controls
                progressBarDownload.Visible = false;
                lblProgress.Visible = false;
                btnCancel.Visible = false;

                // Re-enable form controls
                listViewModels.Enabled = true;
                btnRefresh.Enabled = true;
                btnSelect.Enabled = true;

                isDownloading = false;
                downloadCancellationTokenSource?.Dispose();
                downloadCancellationTokenSource = null;
            }
        }

        private void ModelManager_DownloadProgressChanged(object sender, ModelDownloadProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ModelManager_DownloadProgressChanged(sender, e)));
                return;
            }

            progressBarDownload.Value = e.ProgressPercentage;
            lblProgress.Text = $"Downloading {e.Model.Name}... {e.ProgressPercentage}% ({FormatBytes(e.DownloadedBytes)} / {FormatBytes(e.TotalBytes)})";
            //lblProgress.Text = string.Format(getString("download_progress"), e.Model.Name, e.ProgressPercentage, FormatBytes(e.DownloadedBytes), FormatBytes(e.TotalBytes));
        }

        private void ModelManager_ModelStatusChanged(object sender, ModelStatusChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ModelManager_ModelStatusChanged(sender, e)));
                return;
            }

            // Update the display
            foreach (ListViewItem item in listViewModels.Items)
            {
                var model = (VoskModel)item.Tag;
                if (model.Name == e.Model.Name)
                {
                    item.SubItems[3].Text = GetStatusText(e.Model);

                    // Update colors
                    switch (e.Model.Status)
                    {
                        case ModelStatus.Available:
                            item.ForeColor = Color.Black;
                            item.Font = new Font(item.Font, FontStyle.Regular);
                            break;
                        case ModelStatus.Downloaded:
                            item.ForeColor = Color.Blue;
                            item.Font = new Font(item.Font, FontStyle.Regular);
                            break;
                        case ModelStatus.Downloading:
                            item.ForeColor = Color.Orange;
                            item.Font = new Font(item.Font, FontStyle.Regular);
                            break;
                    }
                    break;
                }
            }
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await LoadModelsAsync();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            downloadCancellationTokenSource?.Cancel();
        }

        private void cmbLanguageFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            FilterAndDisplayModels();
        }

        private void chkHideObsolete_CheckedChanged(object sender, EventArgs e)
        {
            PopulateLanguageFilter();
            FilterAndDisplayModels();
        }

        private void chkShowLoaded_CheckedChanged(object sender, EventArgs e)
        {
            FilterAndDisplayModels();
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            if (isSelectionMode)
            {
                // Selection mode - select a model
                if (listViewModels.SelectedItems.Count > 0)
                {
                    var selectedModel = (VoskModel)listViewModels.SelectedItems[0].Tag;
                    if (selectedModel.Status == ModelStatus.Downloaded)
                    {
                        SelectedModelPath = selectedModel.LocalPath;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        //MessageBox.Show("Please select a downloaded model or download the selected model first.", "Model Not Downloaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        MessageBox.Show(getString("no_model_downloaded_text"), getString("no_model_doenloaded_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        this.DialogResult = DialogResult.None;
                    }
                }
                else
                {
                    //MessageBox.Show("Please select a model from the list.", "No Model Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    MessageBox.Show(getString("no_model_selected_text"), getString("no_model_selected_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                }
            }
            else
            {
                // Settings mode - just close the window
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}