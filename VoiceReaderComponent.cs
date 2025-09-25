using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Design;
using System.Text.RegularExpressions;

namespace VoiceReader
{
    // Custom text alignment enum that includes Justified
    public enum TextAlignment
    {
        Left,
        Center,
        Right
    }

    // Event arguments for link click
    public class LinkClickEventArgs : EventArgs
    {
        public MouseButtons Button { get; }
        public string Url { get; }
        public string LinkText { get; }
        public Point ClickLocation { get; }

        public LinkClickEventArgs(MouseButtons button, string url, string linkText, Point clickLocation)
        {
            Button = button;
            Url = url;
            LinkText = linkText;
            ClickLocation = clickLocation;
        }
    }

    // Word state enumeration
    public enum WordState
    {
        Unread,
        Read,
        Unrecognize,
        Ignored
    }

    // Word class for tracking read status
    public class TWord
    {
        public string Word { get; set; }
        public int WordStart { get; set; }  // HTML position
        public int WordEnd { get; set; }    // HTML position  
        public Point ClickLocation { get; set; }
        public WordState State { get; set; }
        
        public TWord(string word, int wordStart, int wordEnd, Point clickLocation)
        {
            Word = word;
            WordStart = wordStart;
            WordEnd = wordEnd;
            ClickLocation = clickLocation;
            State = WordState.Unread;
        }
    }

    // Event arguments for word context menu
    public class WordContextMenuEventArgs : EventArgs
    {
        public TWord Word { get; }
        public MouseButtons Button { get; }
        public ContextMenuStrip ContextMenu { get; set; }

        public WordContextMenuEventArgs(TWord word, MouseButtons button)
        {
            Word = word;
            Button = button;
        }
    }

    // HTML content segment for rendering
    public abstract class ContentSegment
    {
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public abstract void Render(Graphics g, ref Point position, Font font, Rectangle clientRect, VoiceReaderComponent control);
        public abstract Size Measure(Graphics g, Font font, int availableWidth);
        public abstract bool ContainsPosition(int charIndex);
        public abstract Point GetCharacterPosition(Graphics g, Font font, int relativeCharIndex);
    }

    public class TextContentSegment : ContentSegment
    {
        public string Text { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsLink { get; set; }
        public string LinkUrl { get; set; }

        public TextContentSegment(string text, int startIndex, bool isBold = false, bool isItalic = false, bool isLink = false, string linkUrl = null)
        {
            Text = text ?? "";
            StartIndex = startIndex;
            Length = text?.Length ?? 0;
            IsBold = isBold;
            IsItalic = isItalic;
            IsLink = isLink;
            LinkUrl = linkUrl;
        }

        public override void Render(Graphics g, ref Point position, Font font, Rectangle clientRect, VoiceReaderComponent control)
        {
            if (string.IsNullOrEmpty(Text)) return;

            Font renderFont = GetRenderFont(font);
            Color textColor = IsLink ? Color.Blue : control.ForeColor;

            TextRenderer.DrawText(g, Text, renderFont, position, textColor, TextFormatFlags.NoPadding);

            Size textSize = TextRenderer.MeasureText(g, Text, renderFont, Size.Empty, TextFormatFlags.NoPadding);
            position.X += textSize.Width;

            if (renderFont != font)
                renderFont.Dispose();
        }

        public override Size Measure(Graphics g, Font font, int availableWidth)
        {
            if (string.IsNullOrEmpty(Text)) return Size.Empty;

            Font renderFont = GetRenderFont(font);
            Size size = TextRenderer.MeasureText(g, Text, renderFont, Size.Empty, TextFormatFlags.NoPadding);

            if (renderFont != font)
                renderFont.Dispose();

            return size;
        }

        public override bool ContainsPosition(int charIndex)
        {
            return charIndex >= StartIndex && charIndex < StartIndex + Length;
        }

        public override Point GetCharacterPosition(Graphics g, Font font, int relativeCharIndex)
        {
            if (relativeCharIndex <= 0) return Point.Empty;
            if (relativeCharIndex >= Text.Length) relativeCharIndex = Text.Length;

            Font renderFont = GetRenderFont(font);
            string substring = Text.Substring(0, relativeCharIndex);
            Size size = TextRenderer.MeasureText(g, substring, renderFont, Size.Empty, TextFormatFlags.NoPadding);

            if (renderFont != font)
                renderFont.Dispose();

            return new Point(size.Width, 0);
        }

        private Font GetRenderFont(Font baseFont)
        {
            FontStyle style = FontStyle.Regular;
            if (IsBold) style |= FontStyle.Bold;
            if (IsItalic) style |= FontStyle.Italic;
            if (IsLink) style |= FontStyle.Underline;

            if (style == FontStyle.Regular)
                return baseFont;

            return new Font(baseFont, style);
        }
    }

    public class ImageContentSegment : ContentSegment
    {
        public string ImagePath { get; set; }
        public Image Image { get; set; }
        public Size DisplaySize { get; set; }

        public ImageContentSegment(string imagePath, int startIndex, int tagLength)
        {
            ImagePath = imagePath;
            StartIndex = startIndex;
            Length = tagLength;
            LoadImage();
        }

        private void LoadImage()
        {
            try
            {
                if (File.Exists(ImagePath))
                {
                    Image = Image.FromFile(ImagePath);
                    DisplaySize = Image.Size;
                }
                else
                {
                    // Try relative path
                    string relativePath = Path.Combine(Application.StartupPath, ImagePath);
                    if (File.Exists(relativePath))
                    {
                        Image = Image.FromFile(relativePath);
                        DisplaySize = Image.Size;
                    }
                }
            }
            catch
            {
                Image = null;
                DisplaySize = new Size(20, 20); // Placeholder size for broken images
            }
        }

        public void UpdateDisplaySize(int lineHeight)
        {
            if (Image == null) return;

            int maxHeight = lineHeight * 7; // Allow images up to X lines high

            if (Image.Height <= maxHeight)
            {
                DisplaySize = Image.Size;
            }
            else
            {
                double scale = (double)maxHeight / Image.Height;
                DisplaySize = new Size(
                    (int)(Image.Width * scale),
                    (int)(Image.Height * scale)
                );
            }
        }

        public override void Render(Graphics g, ref Point position, Font font, Rectangle clientRect, VoiceReaderComponent control)
        {
            // Position advancement only - actual rendering is handled by RenderImageInline
            position.X += DisplaySize.Width;
        }

        public override Size Measure(Graphics g, Font font, int availableWidth)
        {
            return DisplaySize;
        }

        public override bool ContainsPosition(int charIndex)
        {
            return charIndex >= StartIndex && charIndex < StartIndex + Length;
        }

        public override Point GetCharacterPosition(Graphics g, Font font, int relativeCharIndex)
        {
            return new Point(DisplaySize.Width, 0);
        }

        public void Dispose()
        {
            if (Image != null)
            {
                Image.Dispose();
                Image = null;
            }
        }
    }

    public class LineBreakContentSegment : ContentSegment
    {
        public LineBreakContentSegment(int startIndex)
        {
            StartIndex = startIndex;
            Length = 0;
        }

        public override void Render(Graphics g, ref Point position, Font font, Rectangle clientRect, VoiceReaderComponent control)
        {
            position.X = 0;
            position.Y += control.lineHeight;
        }

        public override Size Measure(Graphics g, Font font, int availableWidth)
        {
            return new Size(0, 0);
        }

        public override bool ContainsPosition(int charIndex)
        {
            return charIndex == StartIndex;
        }

        public override Point GetCharacterPosition(Graphics g, Font font, int relativeCharIndex)
        {
            return Point.Empty;
        }
    }

    public class VoiceReaderComponent : Control
    {
        private List<ContentSegment> segments = new List<ContentSegment>();
        private bool segmentsCacheDirty = true;

        // Core properties
        private bool wordWrap = true;
        private ScrollBars scrollBars = ScrollBars.Both;
        private TextAlignment textAlign = TextAlignment.Left;

        // Word tracking
        private List<TWord> words = new List<TWord>();
        private bool wordsCacheDirty = true;
        
        // Highlighting properties
        private Color readColor = Color.LightGreen;
        private Color unrecognizedColor = Color.Red;
        private Color ignoredColor = Color.LightGray;
        private bool autoScrollToHighlightEnd = true;
        private int highlightCornerRadius = 4;

        // корневой путь для загрузки картинок
        private string imagePath = "";

        private Color selectionWordColor = Color.HotPink;

        // Selection state  
        private int wordHighlightStart = -1;
        private int wordHighlightEnd = -1;

        // Scrolling
        private VScrollBar vScrollBar;
        private HScrollBar hScrollBar;
        private int scrollOffsetY = 0;
        private int scrollOffsetX = 0;
        public int lineHeight;
        private int maxContentWidth = 0;
        private int totalContentHeight = 0;
        private BorderStyle borderStyle = BorderStyle.None;

        // Events
        public event EventHandler<LinkClickEventArgs> OnLinkClick;
        public event EventHandler AllHighlighted;
        public event EventHandler<WordContextMenuEventArgs> WordClick;

        public VoiceReaderComponent()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            InitializeScrollBars();
            RecalculateTextMetrics();
        }

        // Finalizer as safety net
        ~VoiceReaderComponent()
        {
            Dispose(false);
        }

        #region Properties

        [AllowNull]
        [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design", typeof(UITypeEditor))]
        public override string Text
        {
            get => base.Text;
            set
            {
                if (base.Text != value)
                {
                    // Clean up existing segments before creating new ones
                    if (segments != null)
                    {
                        foreach (var segment in segments)
                        {
                            if (segment is ImageContentSegment imageSegment)
                            {
                                imageSegment.Dispose();
                            }
                        }
                        segments.Clear();
                    }

                    base.Text = value;
                    segmentsCacheDirty = true;
                    wordsCacheDirty = true;
                    UpdateScrollBars();
                    Invalidate();
                }
            }
        }

        public override Font Font
        {
            get => base.Font;
            set
            {
                if (base.Font != value)
                {
                    base.Font = value;
                    RecalculateTextMetrics();
                    segmentsCacheDirty = true;
                    wordsCacheDirty = true;
                    UpdateScrollBars();
                    Invalidate();
                }
            }
        }

        [DefaultValue(true)]
        public bool WordWrap
        {
            get => wordWrap;
            set
            {
                if (wordWrap != value)
                {
                    wordWrap = value;
                    segmentsCacheDirty = true;
                    wordsCacheDirty = true;
                    UpdateScrollBars();
                    Invalidate();
                }
            }
        }

        [DefaultValue(ScrollBars.Both)]
        public ScrollBars ScrollBars
        {
            get => scrollBars;
            set
            {
                if (scrollBars != value)
                {
                    scrollBars = value;
                    UpdateScrollBars();
                    Invalidate();
                }
            }
        }

        [DefaultValue(TextAlignment.Left)]
        public TextAlignment TextAlign
        {
            get => textAlign;
            set
            {
                if (textAlign != value)
                {
                    textAlign = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets all words extracted from HTML content
        /// </summary>
        [Browsable(false)]
        public List<TWord> Words
        {
            get
            {
                if (wordsCacheDirty) BuildWords();
                return words;
            }
        }

        [DefaultValue(typeof(Color), "HotPink")]
        public Color SelectionWordColor
        {
            get => selectionWordColor;
            set
            {
                if (selectionWordColor != value)
                {
                    selectionWordColor = value;
                    Invalidate();
                }
            }
        }

        [DefaultValue(typeof(Color), "LightGreen")]
        public Color ReadColor
        {
            get => readColor;
            set
            {
                if (readColor != value)
                {
                    readColor = value;
                    Invalidate();
                }
            }
        }

        [DefaultValue(typeof(Color), "Red")]
        public Color UnrecognizedColor
        {
            get => unrecognizedColor;
            set
            {
                if (unrecognizedColor != value)
                {
                    unrecognizedColor = value;
                    Invalidate();
                }
            }
        }

        [DefaultValue(typeof(Color), "LightGray")]
        public Color IgnoredColor
        {
            get => ignoredColor;
            set
            {
                if (ignoredColor != value)
                {
                    ignoredColor = value;
                    Invalidate();
                }
            }
        }

        [DefaultValue(true)]
        public bool AutoScrollToHighlightEnd
        {
            get => autoScrollToHighlightEnd;
            set => autoScrollToHighlightEnd = value;
        }

        [DefaultValue("")]
        public string ImagePath
        {
            get => imagePath;
            set => imagePath = value;
        }

        [DefaultValue(4)]
        public int HighlightCornerRadius
        {
            get => highlightCornerRadius;
            set
            {
                if (highlightCornerRadius != value)
                {
                    highlightCornerRadius = Math.Max(0, value);
                    Invalidate();
                }
            }
        }

        [DefaultValue(BorderStyle.None)]
        public BorderStyle BorderStyle
        {
            get => borderStyle;
            set
            {
                if (borderStyle != value)
                {
                    borderStyle = value;
                    Invalidate();
                }
            }
        }



        #endregion

        #region Word Management
        
        /// <summary>
        /// Gets the next unread word
        /// </summary>
        /// <returns>Next unread TWord or null if all words are read</returns>
        public TWord NextUnread()
        {
            if (wordsCacheDirty) BuildWords();

            for (int i = 0; i < words.Count; i++)
            {
                if (words[i].State == WordState.Unread)
                {
                    return words[i];
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Sets the state of a word
        /// </summary>
        /// <param name="word">The word to update</param>
        /// <param name="state">The new state to set</param>
        /// <param name="markPreviousAsIgnored">Whether to mark previous unread words as ignored (used for Unrecognize)</param>
        private void SetWordState(TWord word, WordState state, bool markPreviousAsIgnored = false)
        {
            if (word == null) return;
            
            // Ensure words are built
            if (wordsCacheDirty) BuildWords();
            
            // Find and update the word
            var targetWord = words.Contains(word) ? word :
                words.FirstOrDefault(w => (w.WordStart == word.WordStart && w.WordEnd == word.WordEnd) ||
                                         w.Word.Equals(word.Word, StringComparison.OrdinalIgnoreCase));
            
            if (targetWord == null) return;
            
            // Handle special logic for Unrecognize state
            if (markPreviousAsIgnored && state == WordState.Unrecognize)
            {
                int targetIndex = words.IndexOf(targetWord);
                
                // Mark all previous unread words as ignored
                for (int i = 0; i < targetIndex; i++)
                {
                    if (words[i].State == WordState.Unread)
                        words[i].State = WordState.Ignored;
                }
            }
            
            // Set the target word state
            targetWord.State = state;
            
            // Auto-scroll if enabled
            if (autoScrollToHighlightEnd)
                ScrollToCharacterPosition(targetWord.WordEnd, true);
            
            CheckAndTriggerAllHighlighted();
            Invalidate();
        }
        
        /// <summary>
        /// Marks a word as read
        /// </summary>
        /// <param name="word">The word to mark as read</param>
        public void SetRead(TWord word)
        {
            SetWordState(word, WordState.Read);
        }

        /// <summary>
        /// Marks a word as ignored
        /// </summary>
        /// <param name="word">The word to mark as ignored</param>
        public void SetIgnored(TWord word)
        {
            SetWordState(word, WordState.Ignored);
        }

        /// <summary>
        /// Marks a word as unrecognized and all previous unread words as ignored
        /// </summary>
        /// <param name="word">The word to mark as unrecognized</param>
        public void SetUnrecognize(TWord word)
        {
            SetWordState(word, WordState.Unrecognize, markPreviousAsIgnored: true);
        }
        
        /// <summary>
        /// Builds the words list from HTML content
        /// </summary>
        private void BuildWords()
        {
            // Store current word states before clearing
            var previousStates = new Dictionary<string, WordState>();
            
            foreach (var word in words)
            {
                string key = $"{word.WordStart}:{word.WordEnd}:{word.Word}";
                if (!previousStates.ContainsKey(key))
                {
                    previousStates[key] = word.State;
                }
            }
            
            words.Clear();
            
            if (string.IsNullOrEmpty(Text))
            {
                wordsCacheDirty = false;
                return;
            }
            
            ParseHtmlContent(); // Ensure segments are built
            
            // Extract words from text segments
            foreach (var segment in segments.OfType<TextContentSegment>()
                                           .Where(s => !string.IsNullOrWhiteSpace(s.Text)))
            {
                if (segment.IsLink)
                {
                    // For link segments, treat the entire link as a single word
                    words.Add(new TWord(segment.Text.Trim(), segment.StartIndex, segment.StartIndex + segment.Length, Point.Empty));
                }
                else
                {
                    // For regular text segments, extract individual words
                    ExtractWordsFromSegment(segment);
                }
            }
            
            // Restore word states after rebuilding
            int readWordsCount = 0;
            foreach (var word in words)
            {
                string key = $"{word.WordStart}:{word.WordEnd}:{word.Word}";
                if (previousStates.TryGetValue(key, out WordState savedState))
                {
                    word.State = savedState;
                    if (savedState == WordState.Read)
                        readWordsCount++;
                }
            }
            
            wordsCacheDirty = false;
        }
        
        /// <summary>
        /// Triggers AllHighlighted event when all words are marked as read
        /// </summary>
        private void CheckAndTriggerAllHighlighted()
        {
            if (AllHighlighted == null) return;
            
            if (wordsCacheDirty) BuildWords();
            
            // Check if all words are read (no unread words remaining)
            bool allWordsRead = words.Count > 0 && !words.Any(w => w.State == WordState.Unread);
            if (allWordsRead)
                AllHighlighted?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// Extracts words from a text segment
        /// </summary>
        private void ExtractWordsFromSegment(TextContentSegment segment)
        {
            string text = segment.Text;
            int currentIndex = 0;
            
            while (currentIndex < text.Length)
            {
                // Skip non-word characters
                while (currentIndex < text.Length && !IsWordCharacter(text[currentIndex]))
                {
                    currentIndex++;
                }
                
                if (currentIndex >= text.Length) break;
                
                // Find word boundaries
                int wordStart = currentIndex;
                while (currentIndex < text.Length && IsWordCharacter(text[currentIndex]))
                {
                    currentIndex++;
                }
                
                if (currentIndex > wordStart)
                {
                    string word = text.Substring(wordStart, currentIndex - wordStart);
                    int htmlStart = segment.StartIndex + wordStart;
                    int htmlEnd = segment.StartIndex + currentIndex;
                    
                    words.Add(new TWord(word, htmlStart, htmlEnd, Point.Empty));
                }
            }
        }
        
       
        #endregion

        protected override void OnPaint(PaintEventArgs e)
        {
            ParseHtmlContent();

            Graphics g = e.Graphics;
            Rectangle clientRect = GetScrollableClientRect();

            // Clear background
            g.FillRectangle(new SolidBrush(BackColor), ClientRectangle);

            // Draw border if needed
            if (borderStyle == BorderStyle.FixedSingle)
            {
                g.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);
            }

            if (segments.Count == 0) return;

            // Set clipping to prevent drawing outside client area
            g.SetClip(clientRect);

            Point currentPos = new Point(-scrollOffsetX, -scrollOffsetY);
            maxContentWidth = 0;
            totalContentHeight = 0;

            List<(ContentSegment segment, Point position, Size size)> currentLine =
                new List<(ContentSegment, Point, Size)>();
            int lineStartY = currentPos.Y;

            foreach (var segment in segments)
            {
                if (segment is LineBreakContentSegment)
                {
                    // Render current line before line break
                    if (currentLine.Count > 0)
                    {
                        // Calculate effective line height for this line
                        int effectiveLineHeight = CalculateEffectiveLineHeight(currentLine);
                        RenderLine(g, currentLine, clientRect, lineStartY, effectiveLineHeight);
                        currentLine.Clear();

                        // Move to next line with proper spacing
                        currentPos.Y += effectiveLineHeight;
                    }
                    else
                    {
                        currentPos.Y += lineHeight;
                    }

                    currentPos.X = -scrollOffsetX;
                    lineStartY = currentPos.Y;
                    continue;
                }

                if (segment is TextContentSegment textSegment && wordWrap)
                {
                    ProcessTextSegmentWithWordWrap(g, textSegment, ref currentPos, ref lineStartY, currentLine, clientRect);
                }
                else
                {
                    Size segmentSize = segment.Measure(g, Font, clientRect.Width);

                    if (wordWrap && currentPos.X + segmentSize.Width > clientRect.Width + scrollOffsetX && currentLine.Count > 0)
                    {
                        // Line would overflow, render current line and start new one
                        int effectiveLineHeight = CalculateEffectiveLineHeight(currentLine);
                        RenderLine(g, currentLine, clientRect, lineStartY, effectiveLineHeight);
                        currentLine.Clear();

                        currentPos.X = -scrollOffsetX;
                        currentPos.Y += effectiveLineHeight;
                        lineStartY = currentPos.Y;
                    }

                    currentLine.Add((segment, new Point(currentPos.X, currentPos.Y), segmentSize));
                    currentPos.X += segmentSize.Width;

                    // Update content bounds - account for images that might increase line height
                    maxContentWidth = Math.Max(maxContentWidth, currentPos.X + scrollOffsetX);
                    int segmentEffectiveHeight = lineHeight;
                    if (segment is ImageContentSegment imageSegment)
                    {
                        segmentEffectiveHeight = Math.Max(lineHeight, imageSegment.DisplaySize.Height);
                    }
                    totalContentHeight = Math.Max(totalContentHeight, currentPos.Y + segmentEffectiveHeight + scrollOffsetY);
                }
            }

            // Render last line
            if (currentLine.Count > 0)
            {
                int effectiveLineHeight = CalculateEffectiveLineHeight(currentLine);
                RenderLine(g, currentLine, clientRect, lineStartY, effectiveLineHeight);
            }

            UpdateScrollBars();
        }

        private int CalculateEffectiveLineHeight(List<(ContentSegment segment, Point position, Size size)> line)
        {
            int effectiveLineHeight = lineHeight;
            foreach (var (segment, position, size) in line)
            {
                if (segment is ImageContentSegment imageSegment)
                {
                    effectiveLineHeight = Math.Max(effectiveLineHeight, imageSegment.DisplaySize.Height);
                }
            }
            return effectiveLineHeight;
        }

        private void ProcessTextSegmentWithWordWrap(Graphics g, TextContentSegment textSegment,
            ref Point currentPos, ref int lineStartY,
            List<(ContentSegment segment, Point position, Size size)> currentLine,
            Rectangle clientRect)
        {
            if (!wordWrap)
            {
                Size segmentSize = textSegment.Measure(g, Font, clientRect.Width);
                currentLine.Add((textSegment, new Point(currentPos.X, currentPos.Y), segmentSize));
                currentPos.X += segmentSize.Width;

                maxContentWidth = Math.Max(maxContentWidth, currentPos.X + scrollOffsetX);
                totalContentHeight = Math.Max(totalContentHeight, currentPos.Y + lineHeight + scrollOffsetY);
                return;
            }

            // For link segments, treat as atomic unit - don't split by words
            if (textSegment.IsLink)
            {
                Size segmentSize = textSegment.Measure(g, Font, clientRect.Width);
                
                // Check if link fits on current line
                if (currentPos.X + segmentSize.Width > clientRect.Width + scrollOffsetX && currentLine.Count > 0)
                {
                    // Render current line and start new one
                    int effectiveLineHeight = CalculateEffectiveLineHeight(currentLine);
                    RenderLine(g, currentLine, clientRect, lineStartY, effectiveLineHeight);
                    currentLine.Clear();

                    currentPos.X = -scrollOffsetX;
                    currentPos.Y += effectiveLineHeight;
                    lineStartY = currentPos.Y;
                }
                
                currentLine.Add((textSegment, new Point(currentPos.X, currentPos.Y), segmentSize));
                currentPos.X += segmentSize.Width;
                
                maxContentWidth = Math.Max(maxContentWidth, currentPos.X + scrollOffsetX);
                totalContentHeight = Math.Max(totalContentHeight, currentPos.Y + lineHeight + scrollOffsetY);
                return;
            }

            string text = textSegment.Text;
            string[] words = text.Split(new char[] { ' ', '\t' }, StringSplitOptions.None);
            int charIndex = 0;

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (i < words.Length - 1) word += " "; // Add space back except for last word

                var wordSegment = new TextContentSegment(word, textSegment.StartIndex + charIndex,
                    textSegment.IsBold, textSegment.IsItalic, textSegment.IsLink, textSegment.LinkUrl);
                Size wordSize = wordSegment.Measure(g, Font, clientRect.Width);

                // Check if word fits on current line
                if (currentPos.X + wordSize.Width > clientRect.Width + scrollOffsetX && currentLine.Count > 0)
                {
                    // Render current line and start new one
                    int effectiveLineHeight = CalculateEffectiveLineHeight(currentLine);
                    RenderLine(g, currentLine, clientRect, lineStartY, effectiveLineHeight);
                    currentLine.Clear();

                    currentPos.X = -scrollOffsetX;
                    currentPos.Y += effectiveLineHeight;
                    lineStartY = currentPos.Y;
                }

                currentLine.Add((wordSegment, new Point(currentPos.X, currentPos.Y), wordSize));
                currentPos.X += wordSize.Width;
                charIndex += words[i].Length + (i < words.Length - 1 ? 1 : 0);

                maxContentWidth = Math.Max(maxContentWidth, currentPos.X + scrollOffsetX);
                totalContentHeight = Math.Max(totalContentHeight, currentPos.Y + lineHeight + scrollOffsetY);
            }
        }

        private void RenderLine(Graphics g, List<(ContentSegment segment, Point position, Size size)> line, Rectangle clientRect, int lineY, int effectiveLineHeight)
        {
            if (line.Count == 0) return;

            // Calculate line width for alignment
            int lineWidth = line.Sum(item => item.size.Width);
            int alignmentOffset = CalculateAlignmentOffset(lineWidth, clientRect.Width);

            // First pass: Render highlighting backgrounds
            foreach (var (segment, position, size) in line)
            {
                RenderHighlighting(g, segment, position, size, clientRect, effectiveLineHeight, alignmentOffset);
            }

            // Second pass: Render actual content (text and images)
            foreach (var (segment, position, size) in line)
            {
                Point adjustedPosition = new Point(position.X + alignmentOffset, position.Y);

                // Render content based on segment type with proper baseline alignment
                if (segment is ImageContentSegment imageSegment)
                {
                    // Render image inline with proper vertical alignment
                    RenderImageInline(g, imageSegment, adjustedPosition, clientRect, effectiveLineHeight);
                }
                else
                {
                    // Render text content with baseline alignment
                    int textBaseline = CalculateTextBaseline(effectiveLineHeight);
                    Point textPosition = new Point(adjustedPosition.X, adjustedPosition.Y + textBaseline);
                    Point textRenderPos = textPosition;
                    segment.Render(g, ref textRenderPos, Font, clientRect, this);
                }
            }
        }

        /// <summary>
        /// Highlights words based on their state
        /// </summary>
        private void RenderReadWordsHighlight(Graphics g, ContentSegment segment, Point position, Size size, Rectangle clientRect, int effectiveLineHeight)
        {
            if (!(segment is TextContentSegment textSegment)) return;
            
            // Ensure words are built
            if (wordsCacheDirty) BuildWords();
            
            // Find words that overlap with this segment and have non-unread states
            var highlightWords = words.Where(w => 
                w.State != WordState.Unread &&
                w.WordStart < segment.StartIndex + segment.Length && 
                w.WordEnd > segment.StartIndex);
            
            foreach (var word in highlightWords)
            {
                // Calculate overlap between word and segment
                int highlightStart = Math.Max(word.WordStart, segment.StartIndex);
                int highlightEnd = Math.Min(word.WordEnd, segment.StartIndex + segment.Length);
                
                if (highlightEnd <= highlightStart) continue;
                
                int startOffset = highlightStart - segment.StartIndex;
                int endOffset = highlightEnd - segment.StartIndex;
                
                Point startPos = startOffset > 0 ? 
                    textSegment.GetCharacterPosition(g, Font, startOffset) : Point.Empty;
                Point endPos = textSegment.GetCharacterPosition(g, Font, endOffset);
                
                var highlightRect = new Rectangle(
                    position.X + startPos.X, position.Y, 
                    endPos.X - startPos.X, effectiveLineHeight);
                
                // Choose color based on word state
                Color color = word.State switch
                {
                    WordState.Read => readColor,
                    WordState.Unrecognize => unrecognizedColor,
                    WordState.Ignored => ignoredColor,
                    _ => Color.Transparent
                };
                
                if (color != Color.Transparent)
                {
                    using var brush = new SolidBrush(color);
                    g.FillRoundedRectangle(brush, highlightRect, highlightCornerRadius);
                }
            }
        }
        private void RenderHighlighting(Graphics g, ContentSegment segment, Point position, Size size, Rectangle clientRect, int effectiveLineHeight, int alignmentOffset)
        {
            Rectangle segmentRect = new Rectangle(position.X + alignmentOffset, position.Y, size.Width, effectiveLineHeight);

            // Render read words highlighting
            RenderReadWordsHighlight(g, segment, new Point(position.X + alignmentOffset, position.Y), size, clientRect, effectiveLineHeight);

            // Render word selection using HTML coordinates
            if (wordHighlightStart >= 0 && wordHighlightEnd > wordHighlightStart && segment is TextContentSegment textSegment)
            {
                // Check if this segment overlaps with the word highlight range in HTML coordinates
                if (segment.StartIndex < wordHighlightEnd && segment.StartIndex + segment.Length > wordHighlightStart)
                {
                    Rectangle wordHighlightRect = segmentRect;

                    // Calculate precise highlighting within the text segment using HTML coordinates
                    int highlightStartInSegment = Math.Max(0, wordHighlightStart - segment.StartIndex);
                    int highlightEndInSegment = Math.Min(segment.Length, wordHighlightEnd - segment.StartIndex);

                    if (highlightEndInSegment > highlightStartInSegment)
                    {
                        Point startPos = highlightStartInSegment > 0 ?
                            textSegment.GetCharacterPosition(g, Font, highlightStartInSegment) : Point.Empty;
                        Point endPos = textSegment.GetCharacterPosition(g, Font, highlightEndInSegment);

                        wordHighlightRect.X = position.X + startPos.X + alignmentOffset;
                        wordHighlightRect.Width = endPos.X - startPos.X;
                    }

                    using (Brush brush = new SolidBrush(selectionWordColor))
                    {
                        g.FillRoundedRectangle(brush, wordHighlightRect, highlightCornerRadius);
                    }
                }
            }
        }

        private int CalculateTextBaseline(int effectiveLineHeight)
        {
            int textHeight = GetTextHeight();
            return Math.Max(0, (effectiveLineHeight - textHeight) / 2);
        }

        private void RenderImageInline(Graphics g, ImageContentSegment imageSegment, Point position, Rectangle clientRect, int effectiveLineHeight)
        {
            if (imageSegment.Image != null)
            {
                // Calculate vertical alignment for image within the effective line height
                int imageY = position.Y + Math.Max(0, (effectiveLineHeight - imageSegment.DisplaySize.Height) / 2);

                Point imagePosition = new Point(position.X, imageY);
                Rectangle imageRect = new Rectangle(imagePosition, imageSegment.DisplaySize);

                // Draw the image
                g.DrawImage(imageSegment.Image, imageRect);
            }
            else
            {
                // Draw placeholder for missing image with vertical centering
                int imageY = position.Y + Math.Max(0, (effectiveLineHeight - imageSegment.DisplaySize.Height) / 2);
                Rectangle placeholderRect = new Rectangle(new Point(position.X, imageY), imageSegment.DisplaySize);

                using (Brush brush = new SolidBrush(Color.LightGray))
                {
                    g.FillRectangle(brush, placeholderRect);
                }
                g.DrawRectangle(Pens.Gray, placeholderRect);

                using (Brush textBrush = new SolidBrush(Color.DarkGray))
                {
                    using (StringFormat format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    })
                    {
                        g.DrawString("Image", Font, textBrush, placeholderRect, format);
                    }
                }
            }
        }

        private int GetTextHeight()
        {
            using (Graphics g = CreateGraphics())
            {
                if (g != null && Font != null)
                {
                    Size textSize = TextRenderer.MeasureText(g, "Agj", Font, Size.Empty, TextFormatFlags.NoPadding);
                    return textSize.Height;
                }
                return 16; // Fallback value
            }
        }

        private int CalculateAlignmentOffset(int lineWidth, int clientWidth)
        {
            switch (textAlign)
            {
                case TextAlignment.Center:
                    return Math.Max(0, (clientWidth - lineWidth) / 2);
                case TextAlignment.Right:
                    return Math.Max(0, clientWidth - lineWidth);
                default: // Left
                    return 0;
            }
        }

        #region HTML Parsing

        private void ParseHtmlContent()
        {
            if (!segmentsCacheDirty) return;

            // Clean up existing segments before creating new ones
            if (segments != null)
            {
                foreach (var segment in segments)
                {
                    if (segment is ImageContentSegment imageSegment)
                    {
                        imageSegment.Dispose();
                    }
                }
                segments.Clear();
            }
            else
            {
                segments = new List<ContentSegment>();
            }

            if (string.IsNullOrEmpty(Text))
            {
                segmentsCacheDirty = false;
                return;
            }

            string content = Text;
            int currentIndex = 0;

            // Pattern to match HTML tags
            var tagPattern = @"<(/?)(\w+)(?:\s+([^>]*))?\s*/?>";
            var matches = Regex.Matches(content, tagPattern, RegexOptions.IgnoreCase);

            var tagStack = new Stack<(string tag, bool isBold, bool isItalic, bool isLink, string linkUrl)>();
            bool currentBold = false;
            bool currentItalic = false;
            bool currentLink = false;
            string currentLinkUrl = null;

            foreach (Match match in matches)
            {
                // Add text before tag
                if (match.Index > currentIndex)
                {
                    string textBefore = content.Substring(currentIndex, match.Index - currentIndex);
                    
                    // Always normalize and check the result, don't pre-filter
                    string normalizedText = NormalizeContentWhitespace(textBefore, currentIndex == 0);
                    // Only add text segments that contain actual content (not just whitespace)
                    if (!string.IsNullOrEmpty(normalizedText) && !string.IsNullOrWhiteSpace(normalizedText))
                    {
                        segments.Add(new TextContentSegment(normalizedText, currentIndex, currentBold, currentItalic, currentLink, currentLinkUrl));
                    }
                }

                bool isClosing = !string.IsNullOrEmpty(match.Groups[1].Value);
                string tagName = match.Groups[2].Value.ToLower();
                string attributes = match.Groups[3].Value;

                switch (tagName)
                {
                    case "p":
                        if (isClosing)
                        {
                            segments.Add(new LineBreakContentSegment(match.Index + match.Length));
                        }
                        break;

                    case "br":
                        segments.Add(new LineBreakContentSegment(match.Index));
                        break;

                    case "b":
                    case "strong":
                        if (!isClosing)
                        {
                            tagStack.Push(("b", currentBold, currentItalic, currentLink, currentLinkUrl));
                            currentBold = true;
                        }
                        else if (tagStack.Count > 0 && tagStack.Peek().tag == "b")
                        {
                            var popped = tagStack.Pop();
                            currentBold = popped.isBold;
                            currentItalic = popped.isItalic;
                            currentLink = popped.isLink;
                            currentLinkUrl = popped.linkUrl;
                        }
                        break;

                    case "i":
                    case "em":
                        if (!isClosing)
                        {
                            tagStack.Push(("i", currentBold, currentItalic, currentLink, currentLinkUrl));
                            currentItalic = true;
                        }
                        else if (tagStack.Count > 0 && tagStack.Peek().tag == "i")
                        {
                            var popped = tagStack.Pop();
                            currentBold = popped.isBold;
                            currentItalic = popped.isItalic;
                            currentLink = popped.isLink;
                            currentLinkUrl = popped.linkUrl;
                        }
                        break;

                    case "a":
                        if (!isClosing)
                        {
                            string href = ExtractHrefAttribute(attributes);
                            tagStack.Push(("a", currentBold, currentItalic, currentLink, currentLinkUrl));
                            currentLink = true;
                            currentLinkUrl = href;
                        }
                        else if (tagStack.Count > 0 && tagStack.Peek().tag == "a")
                        {
                            var popped = tagStack.Pop();
                            currentBold = popped.isBold;
                            currentItalic = popped.isItalic;
                            currentLink = popped.isLink;
                            currentLinkUrl = popped.linkUrl;
                        }
                        break;

                    case "img":
                        string src = ExtractSrcAttribute(attributes);
                        if (!string.IsNullOrEmpty(src))
                        {
                            src = Path.Combine(imagePath, src);

                            var imageSegment = new ImageContentSegment(src, match.Index, match.Length);
                            imageSegment.UpdateDisplaySize(lineHeight);
                            segments.Add(imageSegment);
                        }
                        break;
                }

                currentIndex = match.Index + match.Length;
            }

            // Add remaining text
            if (currentIndex < content.Length)
            {
                string remainingText = content.Substring(currentIndex);
                
                // Always normalize and check the result, don't pre-filter
                string normalizedText = NormalizeContentWhitespace(remainingText, false);
                // Only add text segments that contain actual content (not just whitespace)
                if (!string.IsNullOrEmpty(normalizedText) && !string.IsNullOrWhiteSpace(normalizedText))
                {
                    segments.Add(new TextContentSegment(normalizedText, currentIndex, currentBold, currentItalic, currentLink, currentLinkUrl));
                }
            }

            segmentsCacheDirty = false;
        }



        private string ExtractHrefAttribute(string attributes)
        {
            var hrefMatch = Regex.Match(attributes, @"href\s*=\s*['""]([^'""]*)['""]", RegexOptions.IgnoreCase);
            return hrefMatch.Success ? hrefMatch.Groups[1].Value : "";
        }

        private string ExtractSrcAttribute(string attributes)
        {
            var srcMatch = Regex.Match(attributes, @"src\s*=\s*['""]([^'""]*)['""]", RegexOptions.IgnoreCase);
            return srcMatch.Success ? srcMatch.Groups[1].Value : "";
        }

        private string NormalizeContentWhitespace(string text, bool isFirstSegment)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Decode HTML entities first
            text = DecodeHtmlEntities(text);

            // In HTML, all line breaks and multiple whitespace are collapsed to single spaces
            // Only <br> and <p> tags should create actual line breaks
            // Remove all line breaks (\r, \n, \r\n) and replace with spaces
            text = text.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");
            
            // Replace multiple consecutive whitespace characters with single spaces
            string normalized = Regex.Replace(text, @"\s+", " ");

            // Don't trim leading/trailing whitespace to preserve spaces around HTML tags
            // Only trim if this is truly empty content after normalization
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return "";
            }

            return normalized;
        }

        private string DecodeHtmlEntities(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Common HTML entities for typography
            var entityMap = new Dictionary<string, string>
            {
                // Basic HTML entities
                { "&amp;", "&" },
                { "&lt;", "<" },
                { "&gt;", ">" },
                { "&quot;", "\"" },
                { "&apos;", "'" },
                { "&#39;", "'" },
                
                // Typography entities
                { "&mdash;", "—" },     // Em dash
                { "&ndash;", "–" },     // En dash
                { "&hellip;", "…" },    // Horizontal ellipsis
                { "&lsquo;", "'" },     // Left single quotation mark
                { "&rsquo;", "'" },     // Right single quotation mark
                { "&ldquo;", "\"" },     // Left double quotation mark
                { "&rdquo;", "\"" },     // Right double quotation mark
                { "&laquo;", "«" },     // Left angle quotation mark
                { "&raquo;", "»" },     // Right angle quotation mark
                { "&bull;", "•" },      // Bullet
                { "&middot;", "·" },    // Middle dot
                { "&nbsp;", " " },      // Non-breaking space
                { "&#160;", " " },      // Non-breaking space (numeric)
                
                // Common symbols
                { "&copy;", "©" },      // Copyright
                { "&reg;", "®" },       // Registered trademark
                { "&trade;", "™" },     // Trademark
                { "&sect;", "§" },      // Section sign
                { "&para;", "¶" },      // Pilcrow (paragraph sign)
                { "&dagger;", "†" },    // Dagger
                { "&Dagger;", "‡" },    // Double dagger
                
                // Mathematical symbols
                { "&minus;", "−" },     // Minus sign
                { "&plusmn;", "±" },    // Plus-minus sign
                { "&times;", "×" },     // Multiplication sign
                { "&divide;", "÷" },    // Division sign
                { "&frac12;", "½" },    // Fraction one half
                { "&frac14;", "¼" },    // Fraction one quarter
                { "&frac34;", "¾" },    // Fraction three quarters
                
                // Currency symbols
                { "&euro;", "€" },      // Euro
                { "&pound;", "£" },     // Pound
                { "&yen;", "¥" },       // Yen
                { "&cent;", "¢" },      // Cent
                
                // Accented characters (common ones)
                { "&aacute;", "á" },    // a with acute
                { "&Aacute;", "Á" },    // A with acute
                { "&eacute;", "é" },    // e with acute
                { "&Eacute;", "É" },    // E with acute
                { "&iacute;", "í" },    // i with acute
                { "&Iacute;", "Í" },    // I with acute
                { "&oacute;", "ó" },    // o with acute
                { "&Oacute;", "Ó" },    // O with acute
                { "&uacute;", "ú" },    // u with acute
                { "&Uacute;", "Ú" },    // U with acute
                { "&agrave;", "à" },    // a with grave
                { "&Agrave;", "À" },    // A with grave
                { "&egrave;", "è" },    // e with grave
                { "&Egrave;", "È" },    // E with grave
                { "&ntilde;", "ñ" },    // n with tilde
                { "&Ntilde;", "Ñ" },    // N with tilde
                { "&ccedil;", "ç" },    // c with cedilla
                { "&Ccedil;", "Ç" },    // C with cedilla
            };

            // Replace HTML entities with their characters
            string result = text;
            foreach (var entity in entityMap)
            {
                result = result.Replace(entity.Key, entity.Value);
            }

            // Handle numeric entities (&#123; format)
            result = Regex.Replace(result, @"&#(\d+);", match =>
            {
                if (int.TryParse(match.Groups[1].Value, out int code) && code >= 0 && code <= 1114111)
                {
                    try
                    {
                        return char.ConvertFromUtf32(code);
                    }
                    catch
                    {
                        return match.Value; // Return original if conversion fails
                    }
                }
                return match.Value;
            });

            // Handle hexadecimal numeric entities (&#x1A; format)
            result = Regex.Replace(result, @"&#x([0-9A-Fa-f]+);", match =>
            {
                if (int.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, null, out int code) && code >= 0 && code <= 1114111)
                {
                    try
                    {
                        return char.ConvertFromUtf32(code);
                    }
                    catch
                    {
                        return match.Value; // Return original if conversion fails
                    }
                }
                return match.Value;
            });

            return result;
        }

        #endregion

        #region Mouse Handling

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            Focus();
            int charIndex = GetCharIndexFromPosition(e.Location);

            // Check if clicking on a link
            var segment = GetSegmentAtPosition(e.Location);
            if (segment is TextContentSegment textSegment && textSegment.IsLink)
            {
                // Highlight the entire link segment
                wordHighlightStart = textSegment.StartIndex;
                wordHighlightEnd = textSegment.StartIndex + textSegment.Length;
                
                // Create a TWord for the link
                var linkWord = new TWord(textSegment.Text, textSegment.StartIndex, textSegment.StartIndex + textSegment.Length, e.Location);
                var eventArgs = new WordContextMenuEventArgs(linkWord, e.Button);
                WordClick?.Invoke(this, eventArgs);
                
                OnLinkClick?.Invoke(this, new LinkClickEventArgs(e.Button, textSegment.LinkUrl, textSegment.Text, e.Location));
                
                Invalidate();
                return;
            }

            // Handle word clicks
            var wordBounds = GetWordBoundsAtPosition(charIndex);
            if (wordBounds.HasValue)
            {
                string word = GetWordAtPosition(charIndex);
                
                // Ensure words are built
                if (wordsCacheDirty) BuildWords();
                
                // Find the actual TWord object from collection
                var existingWord = words.FirstOrDefault(w => 
                    w.WordStart < wordBounds.Value.End && w.WordEnd > wordBounds.Value.Start) ??
                    words.FirstOrDefault(w => w.Word.Equals(word, StringComparison.OrdinalIgnoreCase));
                
                var clickedWord = existingWord ?? new TWord(word, wordBounds.Value.Start, wordBounds.Value.End, e.Location);
                if (existingWord != null) clickedWord.ClickLocation = e.Location;
                
                var eventArgs = new WordContextMenuEventArgs(clickedWord, e.Button);

                // Handle word highlighting
                if (e.Button == MouseButtons.Left | e.Button == MouseButtons.Right)
                {
                    wordHighlightStart = wordBounds.Value.Start;
                    wordHighlightEnd = wordBounds.Value.End;
                }

                WordClick?.Invoke(this, eventArgs);
                eventArgs.ContextMenu?.Show(this, e.Location);
            }
            else
            {
                // Clear word highlight when clicking outside any word (left click only)
                if (e.Button == MouseButtons.Left)
                {
                    wordHighlightStart = -1;
                    wordHighlightEnd = -1;
                }
            }

            Invalidate();
        }



        #endregion

        #region Position Calculations

        private int GetCharIndexFromPosition(Point location)
        {
            ParseHtmlContent();

            // Convert client coordinates to content coordinates by adding scroll offsets
            Point adjustedLocation = new Point(location.X + scrollOffsetX, location.Y + scrollOffsetY);
            
            using (Graphics g = CreateGraphics())
            {
                // Recreate the exact same coordinate system as OnPaint
                Rectangle clientRect = GetScrollableClientRect();
                Point currentPos = new Point(0, 0);  // Start from content origin, not adjusted for scroll
                
                List<(ContentSegment segment, Point position, Size size)> currentLine =
                    new List<(ContentSegment, Point, Size)>();
                int lineStartY = currentPos.Y;

                foreach (var segment in segments)
                {
                    if (segment is LineBreakContentSegment)
                    {
                        // Check current line before processing line break
                        if (currentLine.Count > 0)
                        {
                            int effectiveLineHeight = CalculateEffectiveLineHeight(currentLine);
                            if (adjustedLocation.Y >= lineStartY && adjustedLocation.Y < lineStartY + effectiveLineHeight)
                            {
                                return GetCharIndexInLine(g, currentLine, adjustedLocation);
                            }
                            currentPos.Y += effectiveLineHeight;
                        }
                        else
                        {
                            if (adjustedLocation.Y >= lineStartY && adjustedLocation.Y < lineStartY + lineHeight)
                            {
                                return segment.StartIndex;
                            }
                            currentPos.Y += lineHeight;
                        }

                        currentLine.Clear();
                        currentPos.X = 0;  // Reset to content origin
                        lineStartY = currentPos.Y;
                        continue;
                    }

                    if (segment is TextContentSegment textSegment && wordWrap)
                    {
                        // Use exact same logic as ProcessTextSegmentWithWordWrap for position mapping
                        if (ProcessTextSegmentForPositionDetection(g, textSegment, ref currentPos, ref lineStartY, currentLine, clientRect, adjustedLocation, out int? foundCharIndex))
                        {
                            return foundCharIndex.Value;
                        }
                    }
                    else
                    {
                        Size segmentSize = segment.Measure(g, Font, clientRect.Width);

                        if (wordWrap && currentPos.X + segmentSize.Width > clientRect.Width && currentLine.Count > 0)
                        {
                            // Check current line before wrapping
                            int effectiveLineHeight = CalculateEffectiveLineHeight(currentLine);
                            if (adjustedLocation.Y >= lineStartY && adjustedLocation.Y < lineStartY + effectiveLineHeight)
                            {
                                return GetCharIndexInLine(g, currentLine, adjustedLocation);
                            }

                            currentLine.Clear();
                            currentPos.X = 0;  // Reset to content origin
                            currentPos.Y += effectiveLineHeight;
                            lineStartY = currentPos.Y;
                        }

                        currentLine.Add((segment, new Point(currentPos.X, currentPos.Y), segmentSize));
                        currentPos.X += segmentSize.Width;
                    }
                }

                // Check last line
                if (currentLine.Count > 0)
                {
                    int effectiveLineHeight = CalculateEffectiveLineHeight(currentLine);
                    if (adjustedLocation.Y >= lineStartY && adjustedLocation.Y < lineStartY + effectiveLineHeight)
                    {
                        return GetCharIndexInLine(g, currentLine, adjustedLocation);
                    }
                }
            }

            return Text?.Length ?? 0;
        }


        /// <summary>
        /// Processes text segment for position detection - exactly mirrors ProcessTextSegmentWithWordWrap
        /// </summary>
        private bool ProcessTextSegmentForPositionDetection(Graphics g, TextContentSegment textSegment,
            ref Point currentPos, ref int lineStartY,
            List<(ContentSegment segment, Point position, Size size)> currentLine,
            Rectangle clientRect, Point targetLocation, out int? foundCharIndex)
        {
            foundCharIndex = null;
            
            if (!wordWrap)
            {
                Size segmentSize = textSegment.Measure(g, Font, clientRect.Width);
                currentLine.Add((textSegment, new Point(currentPos.X, currentPos.Y), segmentSize));
                currentPos.X += segmentSize.Width;
                return false;
            }

            // For link segments, treat as atomic unit - don't split by words
            if (textSegment.IsLink)
            {
                Size segmentSize = textSegment.Measure(g, Font, clientRect.Width);
                
                // Check if link fits on current line - exact same logic as ProcessTextSegmentWithWordWrap
                if (currentPos.X + segmentSize.Width > clientRect.Width && currentLine.Count > 0)
                {
                    // Check current line before wrapping
                    int effectiveLineHeight = CalculateEffectiveLineHeight(currentLine);
                    if (targetLocation.Y >= lineStartY && targetLocation.Y < lineStartY + effectiveLineHeight)
                    {
                        foundCharIndex = GetCharIndexInLine(g, currentLine, targetLocation);
                        return true;
                    }

                    currentLine.Clear();
                    currentPos.X = 0;  // Reset to content origin
                    currentPos.Y += effectiveLineHeight;
                    lineStartY = currentPos.Y;
                }
                
                currentLine.Add((textSegment, new Point(currentPos.X, currentPos.Y), segmentSize));
                currentPos.X += segmentSize.Width;
                return false;
            }

            string text = textSegment.Text;
            string[] words = text.Split(new char[] { ' ', '\t' }, StringSplitOptions.None);
            int charIndex = 0;

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (i < words.Length - 1) word += " "; // Add space back except for last word

                var wordSegment = new TextContentSegment(word, textSegment.StartIndex + charIndex,
                    textSegment.IsBold, textSegment.IsItalic, textSegment.IsLink, textSegment.LinkUrl);
                Size wordSize = wordSegment.Measure(g, Font, clientRect.Width);

                // Check if word fits on current line - exact same logic as ProcessTextSegmentWithWordWrap
                if (currentPos.X + wordSize.Width > clientRect.Width && currentLine.Count > 0)
                {
                    // Check current line before wrapping
                    int effectiveLineHeight = CalculateEffectiveLineHeight(currentLine);
                    if (targetLocation.Y >= lineStartY && targetLocation.Y < lineStartY + effectiveLineHeight)
                    {
                        foundCharIndex = GetCharIndexInLine(g, currentLine, targetLocation);
                        return true;
                    }

                    currentLine.Clear();
                    currentPos.X = 0;  // Reset to content origin
                    currentPos.Y += effectiveLineHeight;
                    lineStartY = currentPos.Y;
                }

                currentLine.Add((wordSegment, new Point(currentPos.X, currentPos.Y), wordSize));
                currentPos.X += wordSize.Width;
                charIndex += words[i].Length + (i < words.Length - 1 ? 1 : 0);
            }
            
            return false;
        }

        private int GetCharIndexInLine(Graphics g, List<(ContentSegment segment, Point position, Size size)> line, Point contentLocation)
        {
            // Calculate alignment offset for the complete line
            int lineWidth = line.Sum(item => item.size.Width);
            int alignmentOffset = CalculateAlignmentOffset(lineWidth, GetScrollableClientRect().Width);
            
            // The contentLocation already includes scroll offsets (adjusted coordinates)
            // Subtract alignment offset to get the relative position within the line
            int targetX = contentLocation.X - alignmentOffset;

            // Use cumulative approach like the old logic but with proper coordinate handling
            int currentX = 0;
            foreach (var (segment, position, size) in line)
            {
                // Check if click is within this segment's bounds
                if (targetX >= currentX && targetX < currentX + size.Width)
                {
                    if (segment is TextContentSegment textSegment)
                    {
                        int relativeX = targetX - currentX;
                        int charIndex = GetCharIndexInTextSegment(g, textSegment, relativeX);
                        return charIndex;
                    }
                    return segment.StartIndex;
                }
                currentX += size.Width;
            }

            // Click is beyond the line, return end of last segment
            if (line.Count > 0)
            {
                var lastSegment = line.Last();
                return lastSegment.segment.StartIndex + lastSegment.segment.Length;
            }
            
            return 0;
        }

        private int GetCharIndexInTextSegment(Graphics g, TextContentSegment textSegment, int relativeX)
        {
            if (string.IsNullOrEmpty(textSegment.Text)) return textSegment.StartIndex;

            Font renderFont = GetRenderFont(textSegment);

            int left = 0;
            int right = textSegment.Text.Length;

            while (left < right)
            {
                int mid = (left + right) / 2;
                string substring = textSegment.Text.Substring(0, mid);
                Size textSize = TextRenderer.MeasureText(g, substring, renderFont, Size.Empty, TextFormatFlags.NoPadding);

                if (textSize.Width < relativeX)
                    left = mid + 1;
                else
                    right = mid;
            }

            if (renderFont != Font)
                renderFont.Dispose();

            return textSegment.StartIndex + Math.Min(left, textSegment.Text.Length);
        }

        private Font GetRenderFont(TextContentSegment segment)
        {
            FontStyle style = FontStyle.Regular;
            if (segment.IsBold) style |= FontStyle.Bold;
            if (segment.IsItalic) style |= FontStyle.Italic;
            if (segment.IsLink) style |= FontStyle.Underline;

            if (style == FontStyle.Regular)
                return Font;

            return new Font(Font, style);
        }

        private ContentSegment GetSegmentAtPosition(Point location)
        {
            int charIndex = GetCharIndexFromPosition(location);
            return segments.FirstOrDefault(s => s.ContainsPosition(charIndex));
        }

        private (int Start, int End)? GetWordBoundsAtPosition(int charIndex)
        {
            if (string.IsNullOrEmpty(Text) || charIndex < 0 || charIndex >= Text.Length)
                return null;

            // Find the segment containing this character index
            var containingSegment = segments.FirstOrDefault(s => s.ContainsPosition(charIndex));
            if (containingSegment == null || !(containingSegment is TextContentSegment textSegment))
                return null;

            // If this is a link segment, return the entire link as word bounds
            if (textSegment.IsLink)
            {
                return (textSegment.StartIndex, textSegment.StartIndex + textSegment.Length);
            }

            // Convert global character index to local index within the segment
            int localCharIndex = charIndex - textSegment.StartIndex;
            if (localCharIndex < 0 || localCharIndex >= textSegment.Text.Length)
                return null;

            // Check if the character at the position is a word character
            char currentChar = textSegment.Text[localCharIndex];
            if (!IsWordCharacter(currentChar))
                return null;

            // Find word boundaries within the segment
            int localStart = localCharIndex;
            int localEnd = localCharIndex;

            // Move start backwards to find word beginning within segment
            while (localStart > 0 && IsWordCharacter(textSegment.Text[localStart - 1]))
            {
                localStart--;
            }

            // Move end forwards to find word ending within segment
            while (localEnd < textSegment.Text.Length && IsWordCharacter(textSegment.Text[localEnd]))
            {
                localEnd++;
            }

            if (localStart == localEnd) return null;

            // Convert back to global indices
            int globalStart = textSegment.StartIndex + localStart;
            int globalEnd = textSegment.StartIndex + localEnd;

            return (globalStart, globalEnd);
        }

        private bool IsWordCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || 
                   char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.ConnectorPunctuation ||
                   c == '\'' || c == '\u2019'; // Include apostrophe and right single quotation mark
        }

        private string GetWordAtPosition(int charIndex)
        {
            if (string.IsNullOrEmpty(Text) || charIndex < 0 || charIndex >= Text.Length)
                return string.Empty;

            // Find the segment containing this character index
            var containingSegment = segments.FirstOrDefault(s => s.ContainsPosition(charIndex));
            if (containingSegment is TextContentSegment textSegment)
            {
                // If this is a link segment, return the entire link text
                if (textSegment.IsLink)
                {
                    return textSegment.Text.Trim();
                }
                
                // For regular text segments, find the specific word
                var wordBounds = GetWordBoundsAtPosition(charIndex);
                if (wordBounds.HasValue)
                {
                    int localStart = wordBounds.Value.Start - textSegment.StartIndex;
                    int localEnd = wordBounds.Value.End - textSegment.StartIndex;

                    if (localStart >= 0 && localEnd <= textSegment.Text.Length && localEnd > localStart)
                    {
                        return textSegment.Text.Substring(localStart, localEnd - localStart);
                    }
                }
            }

            return string.Empty;
        }



        #endregion

        #region Scrolling

        private void InitializeScrollBars()
        {
            vScrollBar = new VScrollBar
            {
                Dock = DockStyle.Right,
                Visible = false
            };
            vScrollBar.Scroll += VScrollBar_Scroll;

            hScrollBar = new HScrollBar
            {
                Dock = DockStyle.Bottom,
                Visible = false
            };
            hScrollBar.Scroll += HScrollBar_Scroll;

            Controls.Add(vScrollBar);
            Controls.Add(hScrollBar);
        }

        private void VScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            scrollOffsetY = e.NewValue;
            Invalidate();
        }

        private void HScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            scrollOffsetX = e.NewValue;
            Invalidate();
        }

        private void UpdateScrollBars()
        {
            Rectangle clientRect = ClientRectangle;

            bool needVScroll = scrollBars != ScrollBars.None && scrollBars != ScrollBars.Horizontal &&
                              totalContentHeight > clientRect.Height;
            bool needHScroll = scrollBars != ScrollBars.None && scrollBars != ScrollBars.Vertical &&
                              maxContentWidth > clientRect.Width;

            // Adjust for scroll bar space
            if (needVScroll && needHScroll)
            {
                needVScroll = totalContentHeight > clientRect.Height - SystemInformation.HorizontalScrollBarHeight;
                needHScroll = maxContentWidth > clientRect.Width - SystemInformation.VerticalScrollBarWidth;
            }

            vScrollBar.Visible = needVScroll;
            hScrollBar.Visible = needHScroll;

            if (needVScroll)
            {
                vScrollBar.Maximum = totalContentHeight;
                vScrollBar.LargeChange = Math.Max(1, clientRect.Height - (needHScroll ? SystemInformation.HorizontalScrollBarHeight : 0));
                vScrollBar.SmallChange = lineHeight;
                vScrollBar.Value = Math.Min(scrollOffsetY, Math.Max(0, vScrollBar.Maximum - vScrollBar.LargeChange));
            }

            if (needHScroll)
            {
                hScrollBar.Maximum = maxContentWidth;
                hScrollBar.LargeChange = Math.Max(1, clientRect.Width - (needVScroll ? SystemInformation.VerticalScrollBarWidth : 0));
                hScrollBar.SmallChange = 20;
                hScrollBar.Value = Math.Min(scrollOffsetX, Math.Max(0, hScrollBar.Maximum - hScrollBar.LargeChange));
            }
        }

        private Rectangle GetScrollableClientRect()
        {
            Rectangle rect = ClientRectangle;

            if (vScrollBar.Visible)
                rect.Width -= SystemInformation.VerticalScrollBarWidth;
            if (hScrollBar.Visible)
                rect.Height -= SystemInformation.HorizontalScrollBarHeight;

            return rect;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (vScrollBar.Visible)
            {
                int newValue = scrollOffsetY - (e.Delta / 120) * lineHeight * 3;
                newValue = Math.Max(0, Math.Min(newValue, vScrollBar.Maximum - vScrollBar.LargeChange));

                if (newValue != scrollOffsetY)
                {
                    scrollOffsetY = newValue;
                    vScrollBar.Value = newValue;
                    Invalidate();
                }
            }
        }

        private void ScrollToCharacterPosition(int charIndex, bool centerInView = false)
        {
            if (charIndex <= 0) return;

            ParseHtmlContent();

            using (Graphics g = CreateGraphics())
            {
                if (g == null) return;

                // Use EXACT same coordinate system as OnPaint for consistency
                Rectangle clientRect = GetScrollableClientRect();
                Point currentPos = new Point(-scrollOffsetX, -scrollOffsetY);
                List<(ContentSegment segment, Point position, Size size)> currentLine =
                    new List<(ContentSegment, Point, Size)>();
                int lineStartY = currentPos.Y;
                bool found = false;

                foreach (var segment in segments)
                {
                    if (segment is LineBreakContentSegment)
                    {
                        // Process current line
                        if (currentLine.Count > 0)
                        {
                            int effectiveLineHeight = CalculateEffectiveLineHeight(currentLine);

                            // Check if target character is in this line
                            if (segment.StartIndex >= charIndex || IsCharIndexInLine(currentLine, charIndex))
                            {
                                if (centerInView)
                                    ScrollToCenterPosition(lineStartY + scrollOffsetY); // Convert back to content coordinates
                                else
                                    ScrollToPosition(0, lineStartY + scrollOffsetY); // Convert back to content coordinates
                                found = true;
                                break;
                            }

                            currentPos.Y += effectiveLineHeight;
                        }
                        else
                        {
                            if (segment.StartIndex >= charIndex)
                            {
                                if (centerInView)
                                    ScrollToCenterPosition(lineStartY + scrollOffsetY); // Convert back to content coordinates
                                else
                                    ScrollToPosition(0, lineStartY + scrollOffsetY); // Convert back to content coordinates
                                found = true;
                                break;
                            }
                            currentPos.Y += lineHeight;
                        }

                        currentLine.Clear();
                        currentPos.X = -scrollOffsetX;
                        lineStartY = currentPos.Y;
                        continue;
                    }

                    // Check if target character is within this segment
                    if (segment.ContainsPosition(charIndex))
                    {
                        if (centerInView)
                            ScrollToCenterPosition(lineStartY + scrollOffsetY); // Convert back to content coordinates
                        else
                            ScrollToPosition(0, lineStartY + scrollOffsetY); // Convert back to content coordinates
                        found = true;
                        break;
                    }

                    if (segment is TextContentSegment textSegment && wordWrap)
                    {
                        if (ProcessTextSegmentForScrolling(g, textSegment, ref currentPos, ref lineStartY, currentLine, charIndex, centerInView))
                        {
                            found = true;
                            break;
                        }
                    }
                    else
                    {
                        Size segmentSize = segment.Measure(g, Font, clientRect.Width);

                        // Use EXACT same wrap condition as OnPaint rendering
                        if (wordWrap && currentPos.X + segmentSize.Width > clientRect.Width + scrollOffsetX && currentLine.Count > 0)
                        {
                            // Check if target is in current line before wrapping
                            if (IsCharIndexInLine(currentLine, charIndex))
                            {
                                if (centerInView)
                                    ScrollToCenterPosition(lineStartY + scrollOffsetY); // Convert back to content coordinates
                                else
                                    ScrollToPosition(0, lineStartY + scrollOffsetY); // Convert back to content coordinates
                                found = true;
                                break;
                            }

                            currentLine.Clear();
                            currentPos.X = -scrollOffsetX;
                            currentPos.Y += CalculateEffectiveLineHeight(currentLine);
                            lineStartY = currentPos.Y;
                        }

                        currentLine.Add((segment, new Point(currentPos.X, currentPos.Y), segmentSize));
                        currentPos.X += segmentSize.Width;
                    }
                }

                // Check last line if not found yet
                if (!found && currentLine.Count > 0 && IsCharIndexInLine(currentLine, charIndex))
                {
                    if (centerInView)
                        ScrollToCenterPosition(lineStartY + scrollOffsetY); // Convert back to content coordinates
                    else
                        ScrollToPosition(0, lineStartY + scrollOffsetY); // Convert back to content coordinates
                }
            }
        }

        private bool ProcessTextSegmentForScrolling(Graphics g, TextContentSegment textSegment,
            ref Point currentPos, ref int lineStartY,
            List<(ContentSegment segment, Point position, Size size)> currentLine, int targetCharIndex, bool centerInView = false)
        {
            string text = textSegment.Text;
            string[] words = text.Split(new char[] { ' ', '\t' }, StringSplitOptions.None);
            int charIndex = 0;
            Rectangle clientRect = GetScrollableClientRect();

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (i < words.Length - 1) word += " "; // Add space back except for last word

                var wordSegment = new TextContentSegment(word, textSegment.StartIndex + charIndex,
                    textSegment.IsBold, textSegment.IsItalic, textSegment.IsLink, textSegment.LinkUrl);
                Size wordSize = wordSegment.Measure(g, Font, clientRect.Width);

                // Use EXACT same word wrap condition as OnPaint rendering
                if (currentPos.X + wordSize.Width > clientRect.Width + scrollOffsetX && currentLine.Count > 0)
                {
                    // Check if target is in current line before wrapping
                    if (IsCharIndexInLine(currentLine, targetCharIndex))
                    {
                        if (centerInView)
                            ScrollToCenterPosition(lineStartY + scrollOffsetY); // Convert back to content coordinates
                        else
                            ScrollToPosition(0, lineStartY + scrollOffsetY); // Convert back to content coordinates
                        return true;
                    }

                    currentLine.Clear();
                    currentPos.X = -scrollOffsetX;
                    currentPos.Y += CalculateEffectiveLineHeight(currentLine);
                    lineStartY = currentPos.Y;
                }

                currentLine.Add((wordSegment, new Point(currentPos.X, currentPos.Y), wordSize));

                // Check if target character is in this word
                if (targetCharIndex >= wordSegment.StartIndex && targetCharIndex < wordSegment.StartIndex + wordSegment.Length)
                {
                    if (centerInView)
                        ScrollToCenterPosition(lineStartY + scrollOffsetY); // Convert back to content coordinates
                    else
                        ScrollToPosition(0, lineStartY + scrollOffsetY); // Convert back to content coordinates
                    return true;
                }

                currentPos.X += wordSize.Width;
                charIndex += words[i].Length + (i < words.Length - 1 ? 1 : 0);
            }
            
            return false;
        }

        private bool IsCharIndexInLine(List<(ContentSegment segment, Point position, Size size)> line, int charIndex)
        {
            foreach (var (segment, position, size) in line)
            {
                if (segment.ContainsPosition(charIndex))
                {
                    return true;
                }
            }
            return false;
        }

        public void ScrollToPosition(int x, int y)
        {
            Rectangle clientRect = GetScrollableClientRect();
            int margin = lineHeight; // Add margin to keep highlighted text visible

            // Ensure the position is visible by adjusting scroll offsets
            if (y < scrollOffsetY)
            {
                // Target is above current view, scroll up with margin
                scrollOffsetY = Math.Max(0, y - margin);
            }
            else if (y > scrollOffsetY + clientRect.Height - lineHeight - margin)
            {
                // Target is below current view, scroll down with margin
                scrollOffsetY = Math.Max(0, y - clientRect.Height + lineHeight + margin);
            }

            // Update scroll bar value and ensure it's within valid range
            if (vScrollBar.Visible)
            {
                int maxScrollValue = Math.Max(0, vScrollBar.Maximum - vScrollBar.LargeChange);
                scrollOffsetY = Math.Min(scrollOffsetY, maxScrollValue);
                vScrollBar.Value = scrollOffsetY;
            }

            Invalidate();
        }
        
        /// <summary>
        /// Scrolls to center the specified position in the visible area
        /// </summary>
        /// <param name="y">Y position to center</param>
        public void ScrollToCenterPosition(int y)
        {
            Rectangle clientRect = GetScrollableClientRect();
            
            // Center the position in the visible area
            int centerOffset = clientRect.Height / 2;
            int targetScrollY = Math.Max(0, y - centerOffset);
            
            // Update scroll offset
            scrollOffsetY = targetScrollY;

            // Update scroll bar value and ensure it's within valid range
            if (vScrollBar.Visible)
            {
                int maxScrollValue = Math.Max(0, vScrollBar.Maximum - vScrollBar.LargeChange);
                scrollOffsetY = Math.Min(scrollOffsetY, maxScrollValue);
                vScrollBar.Value = scrollOffsetY;
            }

            Invalidate();
        }





        #endregion

        #region Utilities

        private void RecalculateTextMetrics()
        {
            using (Graphics g = CreateGraphics())
            {
                if (g != null && Font != null)
                {
                    Size testSize = TextRenderer.MeasureText(g, "Agj", Font, Size.Empty, TextFormatFlags.NoPadding);
                    lineHeight = testSize.Height + 2; // Add padding
                }
                else
                {
                    lineHeight = 16; // Fallback value
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBars();
            Invalidate();
        }

        public void ClearWordHighlight()
        {
            wordHighlightStart = wordHighlightEnd = -1;
            
            // Reset all words to Unread state
            if (wordsCacheDirty) BuildWords();
            
            foreach (var word in words)
                word.State = WordState.Unread;
            
            Invalidate();
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose scroll bars first
                if (vScrollBar != null)
                {
                    vScrollBar.Scroll -= VScrollBar_Scroll;
                    vScrollBar.Dispose();
                    vScrollBar = null;
                }

                if (hScrollBar != null)
                {
                    hScrollBar.Scroll -= HScrollBar_Scroll;
                    hScrollBar.Dispose();
                    hScrollBar = null;
                }

                // Dispose images in segments
                if (segments != null)
                {
                    foreach (var segment in segments)
                    {
                        if (segment is ImageContentSegment imageSegment && imageSegment.Image != null)
                        {
                            imageSegment.Image.Dispose();
                            imageSegment.Image = null;
                        }
                    }
                    segments.Clear();
                    segments = null;
                }

                // Suppress finalizer since we've disposed managed resources
                GC.SuppressFinalize(this);
            }
            base.Dispose(disposing);
        }
    }
}
