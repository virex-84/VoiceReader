namespace VoiceReader
{
    public partial class frmReadStatistics : Form
    {
        private static string getString(string name)
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(frmReadStatistics));
            return resources.GetString(name);
        }

        public frmReadStatistics(DateTime readStart, List<TWord> words)
        {
            InitializeComponent();

            //статистика по времени
            TimeSpan duration = DateTime.Now.Subtract(readStart);
            string readtime = duration.ToString(@"mm\:ss");
            if (duration.Hours > 0) readtime = duration.ToString(@"hh\:mm\:ss");
            if (duration.Days > 0) readtime = duration.ToString(@"dd\:hh\:mm\:ss");

            //статистика по словам
            var allCount = words.Count;
            var readCount = words.Where(x => x.State == WordState.Read).Count();
            var unrecognizeCount = words.Where(x => x.State == WordState.Unrecognize).Count();

            //label1.Text = $"Your statistics:\nTime: {0}\nAll words: {1}\nRead: { 2}\nUnrecognize: {3}";
            label1.Text = string.Format(getString("read_statistics_text"), readtime, allCount, readCount, unrecognizeCount);
        }
    }
}
