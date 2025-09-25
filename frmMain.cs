using LanguageDetection;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Vosk;

namespace VoiceReader
{
    public partial class frmMain : Form
    {
        private Model model;
        private VoskRecognizer? recognizer;
        private WaveInEvent? waveIn;
        private bool break_recognize = false;
        private DateTime readStart;
        private VRLoader? loader;

        LanguageDetector detector = new LanguageDetector();

        public frmMain()
        {
            InitializeComponent();
            InitializeMicrofones();

            tsLeft.Checked = voiceReaderComponent1.TextAlign == TextAlignment.Left;
            tsCenter.Checked = voiceReaderComponent1.TextAlign == TextAlignment.Center;
            tsRight.Checked = voiceReaderComponent1.TextAlign == TextAlignment.Right;

            tsAutoScroll.Checked = voiceReaderComponent1.AutoScrollToHighlightEnd;

            detector.AddAllLanguages();
        }
        private class MicInfo
        {
            public string deviceName { get; set; }
            public int deviceID { get; set; }
        }

        private class RecognizeWord
        {
            public double conf { get; set; }
            public double end { get; set; }
            public double start { get; set; }
            public string word { get; set; }
        }
        private class RecognizeResult
        {
            public RecognizeWord[] result { get; set; }
            public string partial { get; set; }
            public string text { get; set; }
        }

        private void showMessage(string text)
        {
            MessageBox.Show(text);
        }

        private void setTitle(string title)
        {
            frmMain.ActiveForm.Text = string.Format("VoiceReader: {0}", title);
        }

        private static string getString(string name)
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            return resources.GetString(name);
        }

        private void InitializeMicrofones()
        {
            cbMicrofone.Items.Clear();

            cbMicrofone.DisplayMember = "deviceName";
            cbMicrofone.ValueMember = "deviceName";

            Dictionary<string, MMDevice> retVal = new Dictionary<string, MMDevice>();
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                //deviceInfo.ProductName truncated 32 simbols
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.All))
                {
                    if (device.FriendlyName.StartsWith(deviceInfo.ProductName))
                    {
                        MicInfo info = new MicInfo() { deviceID = waveInDevice, deviceName = device.FriendlyName };
                        cbMicrofone.Items.Add(info);
                        break;
                    }
                }
            }

            if (cbMicrofone.Items.Count > 0)
                cbMicrofone.SelectedIndex = 0;
        }

        private bool CheckModel(Model model)
        {
            //проверяем правильно ли загружена модель
            Type objectType = model.GetType();
            FieldInfo? fieldInfo = objectType.GetField("handle", BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo == null)
                return false;

            object? fieldValue = fieldInfo.GetValue(model);

            if (fieldValue == null)
                return false;

            HandleRef handleRefValue = (HandleRef)fieldValue;

            //если нуль - то точно с моделькой проблемы
            if (handleRefValue.Handle == 0)
                return false;

            return true;
        }

        private async void InitializeVosk(string modelPath)
        {
            if (recognizer != null) return;

            Vosk.Vosk.SetLogLevel(-1);

            model = new Model(modelPath);

            if (!CheckModel(model))
                throw new Exception(getString("vosk_model_load_error"));

            recognizer = new VoskRecognizer(model, 16000.0f);
            recognizer.SetMaxAlternatives(0); //set no alternate tags
            recognizer.SetWords(true); //add array of words
        }

        private void StopVosk()
        {
            if (recognizer != null)
            {
                recognizer.Dispose();
                recognizer = null;
            }
        }

        private async void btnRecognize_Click(object sender, EventArgs e)
        {
            if (waveIn == null)
            {
                if (cbMicrofone.SelectedIndex < 0 || cbMicrofone.SelectedIndex > cbMicrofone.Items.Count)
                {
                    showMessage(getString("not_select_microfone"));
                    return;
                }

                break_recognize = false;
                btnRecognize.ImageIndex = 0;

                try
                {
                    if (loader != null)
                        InitializeVosk(loader.Path);
                    else
                    //если выбрали txt файл - выбираем модель
                    //т.к. мы не можем заранее определить какой там язык
                    {
                        //выдает язык по спецификации  ISO 639-3: eng, rus и т.д.
                        var lang = detector.Detect(voiceReaderComponent1.Text);

                        using (var modelManager = new frmModelManager(true, lang)) // true = selection mode, pass detected language
                        {
                            if (modelManager.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(modelManager.SelectedModelPath))
                            {
                                InitializeVosk(modelManager.SelectedModelPath);
                            } else
                                //ничего не выбрали - выходим
                                return;
                        }
                    }

                    waveIn = new WaveInEvent();
                    MicInfo micInfo = (MicInfo)cbMicrofone.Items[cbMicrofone.SelectedIndex];
                    waveIn.DeviceNumber = micInfo.deviceID;
                    waveIn.WaveFormat = new WaveFormat(16000, 1);
                    waveIn.DataAvailable += WaveIn_DataAvailable;

                    waveIn.StartRecording();

                    btnRecognize.Text = getString("Stop");
                    btnRecognize.ImageIndex = 1;

                    voiceReaderComponent1.ClearWordHighlight();

                    prepareText();

                    voiceReaderComponent1.ScrollToPosition(0, 0);

                    readStart = DateTime.Now;

                } catch (Exception ex)
                {
                    showMessage(ex.Message);
                }
            }
            else
            {
                break_recognize = true;

                waveIn.DataAvailable -= WaveIn_DataAvailable;
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;

                StopVosk();

                tbUserText.Text = "";

                btnRecognize.Text = getString("Recognize");
                btnRecognize.ImageIndex = 0;
            }
        }

        //[HandleProcessCorruptedStateExceptions()] //catch access violation exception
        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (break_recognize) return;

            if (recognizer == null) return;
            try
            {

                if (recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
                {
                    //string txt = recognizer.Result(); vosk ASSERTION_FAILED (VoskAPI:Compute():mel-computations.cc:229) Assertion failed: (mel_energies_out->Dim() == num_bins)
                    string txt = recognizer.FinalResult();
                    UpdateFinalTextBox(txt, "default", true);
                }
                else
                {
                    //recognizer.PartialResult()
                    string txt = recognizer.PartialResult();
                    UpdateFinalTextBox(txt, "default", false);
                }
            }
            catch (Exception)
            {
            }
        }

        private void UpdateFinalTextBox(string text, string language, bool isReset)
        {
            if (break_recognize) return;

            if (text.Length == 0) return;
            RecognizeResult values = JsonConvert.DeserializeObject<RecognizeResult>(text);

            List<string> words = new List<string>();

            if (values.text != null && values.text.Length > 0)
            {
                words.AddRange(values.result.Select((f) => f.word).ToList());
            }

            if (values.partial != null && values.partial.Length > 0)
            {
                words.AddRange(values.partial.Split(" "));
            }

            if (words.Count == 0) return;

            tbUserText.Invoke(new Action(() =>
            {
                tbUserText.Text = string.Join(" ", words);
            }));

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    foreach (var word in words)
                    {

                        if (voiceReaderComponent1.NextUnread().Word.Equals(word, StringComparison.InvariantCultureIgnoreCase))
                        {
                            voiceReaderComponent1.SetRead(voiceReaderComponent1.NextUnread());
                        }
                        /*
                        else
                        {
                            //автоматически игнорируем слова которых нет в модели
                            var nextword = toANSI(voiceReaderComponent1.NextUnread().Word.ToLower());

                            if (model != null)
                            {
                                if (model.FindWord(nextword) < 0)
                                {
                                    voiceReaderComponent1.setIgnored(voiceReaderComponent1.NextUnread());
                                }
                            }

                        }
                        */
                    }
                    if (isReset && recognizer != null) recognizer.Reset();
                }));
            }
        }

        private void prepareText()
        {
            var words = voiceReaderComponent1.Words;
            foreach (var word in words)
            {
                //автоматически игнорируем слова которых нет в модели
                var nextword = toANSI(word.Word.ToLower());

                if (model != null)
                {
                    if (model.FindWord(nextword) < 0)
                    {
                        voiceReaderComponent1.SetIgnored(word);
                    }
                }
            }
        }

        private string toANSI(string word)
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(word + "\0");
            IntPtr ansiStringPtr = Marshal.AllocHGlobal(utf8Bytes.Length);
            Marshal.Copy(utf8Bytes, 0, ansiStringPtr, utf8Bytes.Length);
            var result = Marshal.PtrToStringAnsi(ansiStringPtr);
            Marshal.FreeHGlobal(ansiStringPtr);
            return result;
        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            if (recognizer != null)
                return;

            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Voice Reader Book (*.vrbook)|*.vrbook|" + "*.txt (*.txt)|*.txt" + "|*.html (*.html)|*.html";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Multiselect = false;

                if (File.Exists(tbFile.Text))
                    openFileDialog.InitialDirectory = Path.GetFullPath(tbFile.Text);

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    tbFile.Text = openFileDialog.FileName;

                    if (loader != null) loader.Dispose();
                    loader = null;

                    //выбрали *.vrbook
                    if (openFileDialog.FilterIndex == 1)
                    {
                        //загружаем нашу модель
                        loader = new VRLoader();
                        loader.LoadFile(tbFile.Text);
                        //указываем путь до папки с моделями
                        voiceReaderComponent1.ImagePath = loader.Path;
                        //текст - уже в html
                        voiceReaderComponent1.Text = loader.Book.Text;
                        setTitle(loader.Book.Title);
                    }
                    else
                    {
                        //указываем путь до папки
                        voiceReaderComponent1.ImagePath = Path.GetDirectoryName(tbFile.Text);

                        //текст
                        var content = File.ReadAllText(tbFile.Text);

                        //определим html это или нет
                        var tagPattern = @"<(/?)(\w+)(?:\s+([^>]*))?\s*/?>";
                        var matches = Regex.Matches(content, tagPattern, RegexOptions.IgnoreCase);

                        //если нет ни одного тега - превратим обычный текст в html (добавим переносы строк)
                        //если есть хоть один тег - оставим как есть
                        if (matches.Count == 0)
                        {
                            content = content.Replace("\r\n", "<br>").Replace("\r", "<br>").Replace("\n", "<br>");

                            //нормализуем текст, заменим хитрые апострофы, дефисы и прочее
                            content = content
                                .Replace('\u2019', '\'')  // Right Single Quotation Mark
                                .Replace('\u2018', '\'')  // Left Single Quotation Mark  
                                .Replace('\u201B', '\'')  // Single High-Reversed-9 Quotation Mark
                                .Replace('\u2032', '\'')  // Prime
                                .Replace('\u201C', '"')   // Left Double Quotation Mark
                                .Replace('\u201D', '"')   // Right Double Quotation Mark
                                .Replace('\u201F', '"')   // Double High-Reversed-9 Quotation Mark
                                .Replace('\u2033', '"')   // Double Prime
                                .Replace('\u2010', '-')   // Hyphen
                                .Replace('\u2011', '-')   // Non-Breaking Hyphen
                                .Replace('\u2012', '-')   // Figure Dash
                                .Replace('\u2013', '-')   // En Dash
                                .Replace('\u2014', '-')   // Em Dash
                                .Replace('\u00A0', ' ')   // No-Break Space
                                .Replace('\u202F', ' ')   // Narrow No-Break Space
                                .Replace('\u2009', ' ')   // Thin Space
                                .Replace('\u2002', ' ')   // En Space
                                .Replace('\u2003', ' ');  // Em Space
                        }

                        voiceReaderComponent1.Text = content;

                        setTitle(Path.GetFileName(tbFile.Text));
                    }

                    voiceReaderComponent1.ClearWordHighlight();
                    voiceReaderComponent1.ScrollToPosition(0, 0);
                    voiceReaderComponent1.Refresh();
                }
            }
        }

        private void voiceReaderComponent1_WordClick(object sender, WordContextMenuEventArgs e)
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            if (e.Word.State == WordState.Unread & recognizer != null & e.Button == MouseButtons.Right)
            {
                //"Добавить [слово] как нераспознанное"
                var menu = getString("add_x_as_unrecognized");
                menu = string.Format(menu, e.Word.Word);

                contextMenu.Items.Add(menu, null, (s, args) =>
                {
                    voiceReaderComponent1.SetUnrecognize(e.Word);
                });
            }

            e.ContextMenu = contextMenu;
        }

        private void voiceReaderComponent1_AllHighlighted(object sender, EventArgs e)
        {
            //все прочитали? останавливаем
            if (recognizer != null)
            {
                btnRecognize_Click(sender, e);

                if (!tsShowStatistics.Checked) { return; }
                frmReadStatistics frmReadStatistics = new frmReadStatistics(readStart, voiceReaderComponent1.Words);
                frmReadStatistics.ShowDialog();
            }
        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Name == "tsFontSizePlus" | e.ClickedItem.Name == "tsFontSizeMinus")
            {
                var koeff = 0.0F;

                if (e.ClickedItem.Name == "tsFontSizePlus") koeff = 2.0F;
                if (e.ClickedItem.Name == "tsFontSizeMinus") koeff = -2.0F;

                // Get the current font of the label
                Font currentFont = voiceReaderComponent1.Font;

                if (currentFont.Size + koeff < 0) return;

                // Create a new Font object with an increased size
                // You can adjust the '2.0F' to increase by a different amount
                Font newFont = new Font(
                    currentFont.FontFamily,
                    currentFont.Size + koeff, // Increase the font size by 2 points
                    currentFont.Style,
                    currentFont.Unit
                );

                // Assign the new font to the label
                voiceReaderComponent1.Font = newFont;

                return;
            }

            if (e.ClickedItem.Name == "tsLeft") voiceReaderComponent1.TextAlign = TextAlignment.Left;
            if (e.ClickedItem.Name == "tsCenter") voiceReaderComponent1.TextAlign = TextAlignment.Center;
            if (e.ClickedItem.Name == "tsRight") voiceReaderComponent1.TextAlign = TextAlignment.Right;

            if (e.ClickedItem.Name == "tsAutoScroll")
            {
                tsAutoScroll.Checked = !tsAutoScroll.Checked;
                voiceReaderComponent1.AutoScrollToHighlightEnd = tsAutoScroll.Checked;
            }

            if (e.ClickedItem.Name == "tsModels")
            {
                //идет процесс чтения - не открываем список моделей
                if (recognizer != null) return;

                var modelManagerForm = new frmModelManager();
                var result = modelManagerForm.ShowDialog();
                modelManagerForm.Dispose();
                return;
            }

            tsLeft.Checked = voiceReaderComponent1.TextAlign == TextAlignment.Left;
            tsCenter.Checked = voiceReaderComponent1.TextAlign == TextAlignment.Center;
            tsRight.Checked = voiceReaderComponent1.TextAlign == TextAlignment.Right;
        }

        private void voiceReaderComponent1_OnLinkClick(object sender, LinkClickEventArgs e)
        {
            //открываем только левой кнопкой мыши
            if (e.Button != MouseButtons.Left) return;

            string question = string.Format(getString("do_you_want_open_link"),e.Url);
            DialogResult result = MessageBox.Show(question, e.Url, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo(e.Url) { UseShellExecute = true });
            }
        }
    }
}
