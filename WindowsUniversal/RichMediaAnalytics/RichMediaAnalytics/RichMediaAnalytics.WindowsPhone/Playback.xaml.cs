using HOD.Client;
using HOD.Response.Parser;
using Newtonsoft.Json;
using RichMediaAnalytics.DataModel;
using RichMediaAnalytics.DataModels;
using System;
using System.Collections.Generic;

using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace RichMediaAnalytics
{
    public sealed partial class Playback : Page
    {
        private static readonly Dictionary<string, string> LanguageCollection = new Dictionary<string, string>
        {
            {"en-US", "eng"},
            {"en-GB", "eng"},
            {"es-ES", "spa"},
            {"de-DE", "ger"},
            {"en-ES", "spa"},
            {"fr-FR", "fre"},
            {"it-IT", "ita"},
            {"zh-CN", "chi"},
            {"ru-RU", "rus"},
            {"pt-BR", "por"}
        };

        Windows.UI.Core.CoreDispatcher messageDispatcher = Window.Current.CoreWindow.Dispatcher;

        string mJobID = "";
        HODClient mHodClient = null;
        HODResponseParser mParser = new HODResponseParser();
        DispatcherTimer mTimer;
        string mHodApp = "";
        int mIndex = 0;
        QueryTextIndexResponse mMediaList = null;
        GetContentResponse mContentResponse = null;
        EntityExtractionResponse mEeResponse = null;
        SentimentAnalysisResponse mSaResponse = null;
        string mReadText = "";

        TextViewModel mTextItem = new TextViewModel();
        DispatcherTimer mDelayTimer;
        DispatcherTimer mDelayTextChangedTimer;

        ContentListViewModel mListViewModel = new ContentListViewModel();
        List<object> mUrlList = new List<object>();
        List<int> mWordsIndex = new List<int>();
        int mSearchIndex = 0;

        bool mInprogress = false;
        bool mPunctuation = true;
        bool mIsReading = false;

        enum FunctionID {TRANSCRIPT = 0, CONCEPTS, OPINIONS, INTERESTS }
        FunctionID mCurrentFunction = FunctionID.TRANSCRIPT;

        public Playback()
        {
            this.InitializeComponent();
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            Application.Current.Resuming += Current_Resuming;
            Application.Current.Suspending += Current_Suspending;
            mHodClient = App.GetHODClient();
        }

        private void Current_Resuming(object sender, object e)
        {
            //mplayer.Stop();
        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            if (mplayer.CurrentState == MediaElementState.Playing)
                mplayer.Stop();
            if (mTimer.IsEnabled)
                mTimer.Stop();
        }

        void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            mplayer.CurrentStateChanged -= Mplayer_CurrentStateChanged;
            if (mplayer.CurrentState == MediaElementState.Playing)
                mplayer.Stop();
            if (mTimer.IsEnabled)
                mTimer.Stop();
            mTimer = null;
            if (mDelayTimer.IsEnabled)
                mDelayTimer.Stop();
            mDelayTimer = null;
            if (mDelayTextChangedTimer.IsEnabled)
                mDelayTextChangedTimer.Stop();
            mDelayTextChangedTimer = null;

            mHodClient.onErrorOccurred -= HodClient_onErrorOccurred;
            mHodClient.requestCompletedWithContent -= HodClient_requestCompletedWithContent;
            mHodClient.requestCompletedWithJobID -= HodClient_requestCompletedWithJobID;

            if (this.Frame != null && this.Frame.CanGoBack)
            {
                e.Handled = true;
                this.Frame.GoBack();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            mHodClient.onErrorOccurred += HodClient_onErrorOccurred;
            mHodClient.requestCompletedWithContent += HodClient_requestCompletedWithContent;
            mHodClient.requestCompletedWithJobID += HodClient_requestCompletedWithJobID;

            mTimer = new DispatcherTimer();
            mTimer.Tick += Timer_Tick;
            mplayer.CurrentStateChanged += Mplayer_CurrentStateChanged;

            mDelayTimer = new DispatcherTimer();
            mDelayTimer.Tick += DelayTimer_Tick;
            mDelayTimer.Interval = TimeSpan.FromMilliseconds(100);
            runningText.DataContext = mTextItem;
            processedcontent.NavigationStarting += Processedcontent_NavigationStarting;

            mDelayTextChangedTimer = new DispatcherTimer();
            mDelayTextChangedTimer.Tick += DelayTextChangedTimer_Tick;
            mDelayTextChangedTimer.Interval = TimeSpan.FromMilliseconds(1500);

            operation.Text = "Reading media content.";
            loadingindicator.Visibility = Visibility.Visible;
            mIsReading = true;
            ContentModel item = (ContentModel)e.Parameter;
            mHodApp = HODApps.GET_CONTENT;
            var Params = new Dictionary<string, object>();
            Params.Add("index_reference", item.reference);
            Params.Add("indexes", item.index);
            if (mPunctuation == true)
                Params.Add("print_fields", "offset,text,concepts,occurrences,language");
            else
                Params.Add("print_fields", "offset,text,content,concepts,occurrences,language");

            mHodClient.GetRequest(ref Params, mHodApp, HODClient.REQ_MODE.SYNC);
            mIndex = 0;
            mReadText = "";
            mTextItem.ReadText = "";
            mTextItem.Word = "";
            mTextItem.UnreadText = "";

            mEeResponse = null;
            mSaResponse = null;

            conceptbtn.IsEnabled = true;
            sentimentbtn.IsEnabled = true;
            entitybtn.IsEnabled = true;
            transcriptbtn.IsEnabled = false;
            nextwordbtn.IsEnabled = false;
            searchwordcount.Text = "";
            string content = "http://www.hodshowcase.com/media/" + item.filename;
            Uri contentLink = new Uri(content, UriKind.RelativeOrAbsolute);
            mplayer.Source = contentLink;
        }

        private void instantSearch_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (mInprogress)
                    return;
                Windows.UI.ViewManagement.InputPane.GetForCurrentView().TryHide();
                instantSearchText();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            
            base.OnNavigatedFrom(e);
        }

        private async void Processedcontent_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (null != args.Uri)
            {
                if (args.Uri.OriginalString.Contains("hod_link"))
                {
                    var arr = args.Uri.OriginalString.Split(':');
                    if (arr.Length == 3)
                    {
                        var concept = arr[2];
                        if (concept.Length > 1)
                            findSimilar(concept);
                    }
                    args.Cancel = true;
                    return;
                }
                else if (args.Uri.OriginalString.Contains("hod_home"))
                {
                    args.Cancel = true;
                    conceptbtn_Click(null, null);
                    return;
                }
                else
                {
                    foreach (string url in mUrlList)
                    {
                        if (args.Uri.OriginalString == url || args.Uri.OriginalString == url + "/")
                        {
                            args.Cancel = true;
                            await Launcher.LaunchUriAsync(args.Uri);
                            break;
                        }
                    }
                }
            }
        }
        private void findSimilar(string concept)
        {
            if (mContentResponse == null || mContentResponse.documents == null)
                return;
            var lang = "";
            if (mContentResponse.documents[0].language != null)
            {
                lang = mContentResponse.documents[0].language[0];
            }
            mHodApp = HODApps.FIND_SIMILAR;
            var indexes = new List<object>();
            if (lang == "en-US" || lang == "en-GB")
            {
                indexes.Add("wiki_eng");
                indexes.Add("news_eng");
            }
            else if (lang == "it-IT")
            {
                indexes.Add("wiki_ita");
                indexes.Add("news_ita");
            }
            else if (lang == "fr-FR")
            {
                indexes.Add("wiki_fra");
                indexes.Add("news_fra");
            }
            else if (lang == "de-DE")
            {
                indexes.Add("wiki_ger");
                indexes.Add("news_ger");
            }
            else if (lang == "es-ES")
            {
                indexes.Add("wiki_spa");
            }
            else if (lang == "zh-CN")
            {
                indexes.Add("wiki_chi");
            }
            else
            {
                operation.Text = "No public index database for this language!";
                return;
            }
            var Params = new Dictionary<string, object>()
            {
                {"text", concept },
                {"indexes", indexes },
                {"print_fields", "title,weight,summary" }
            };
            HodClient_onErrorOccurred("Finding similar content.");
            mHodClient.PostRequest(ref Params, mHodApp, HODClient.REQ_MODE.SYNC);
        }
        private async void DelayTimer_Tick(object sender, object e)
        {
            if (mContentResponse != null && mContentResponse.documents != null)
            {
                GetContentResponse.Document doc = mContentResponse.documents[0];
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    var pos = mplayer.Position.TotalMilliseconds;
                    if (mIndex < doc.offset.Count)
                    {
                        var check = doc.offset[mIndex];
                        if (pos > check)
                        {
                            string word = doc.text[mIndex];
                            mTextItem.ReadText = mReadText;

                            int start = mIndex + 1;
                            var sub = doc.text.GetRange(start, doc.offset.Count - start);
                            string leftOver = String.Join(" ", sub);
                            mTextItem.Word = word;
                            mTextItem.UnreadText = leftOver;
                            mReadText += " " + word;
                            mIndex++;
                        }
                    }
                });
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            mTimer.Stop();
            if (mHodClient != null && mJobID != "")
                mHodClient.GetJobStatus(mJobID);
        }

        private void HodClient_requestCompletedWithJobID(string response)
        {
            mJobID = mParser.ParseJobID(response);
            if (mJobID != "")
                mHodClient.GetJobStatus(mJobID);
        }
        public static string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input))
                throw new ArgumentException("ARGH!");
            return input[0].ToString().ToUpper() + input.Substring(1);
        }

        async private void HodClient_requestCompletedWithContent(string response)
        {
            await messageDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                mJobID = "";
                String text = "";
                indicator.Visibility = Visibility.Collapsed;
                if (mHodApp == HODApps.GET_CONTENT)
                {
                    mContentResponse = (GetContentResponse)mParser.ParseCustomResponse<GetContentResponse>(ref response);
                    if (mContentResponse != null)
                    {
                        // purify word array
                        if (mPunctuation == true)
                        {
                            var words = mContentResponse.documents[0].text;
                            var timestamp = mContentResponse.documents[0].offset;

                            int count = mContentResponse.documents[0].text.Count;
                            int start = (mContentResponse.documents[0].text.Count > 10) ? 10 : 0;
                            int end = (mContentResponse.documents[0].text.Count > 200) ? 150 : mContentResponse.documents[0].text.Count;
                            var total = 0.0;
                            for (int i = start; i < end; i++)
                            {
                                total += timestamp[i] - timestamp[i - 1];
                            }
                            var average = total / end - 1;
                            double para = average + 1500;
                            double dot = average + 800;
                            double commas = average + 400;
                            count = words.Count;
                            words[0] = FirstCharToUpper(words[0]);
                            var len = 0;
                            for (int i = 1; i < count; i++)
                            {
                                if (!words[i].Equals("<Music/Noise>") && !words[i - 1].Equals("<Music/Noise>"))
                                {
                                    var diff = timestamp[i] - timestamp[i - 1];
                                    if (diff > para && len > 1) // 2000
                                    {
                                        words[i - 1] += ".\n";
                                        words[i] = FirstCharToUpper(words[i]);
                                        len = 0;
                                    }
                                    else if (diff > dot && len > 1) // 1500
                                    {
                                        words[i - 1] += ".";
                                        words[i] = FirstCharToUpper(words[i]);
                                        len = 0;
                                    }
                                    else if (diff > commas && len > 1) // 1000
                                    {
                                        words[i - 1] += ",";
                                        len = 0;
                                    }
                                    else
                                        len++;
                                }
                            }
                            mContentResponse.documents[0].text = words;
                            mContentResponse.documents[0].offset = timestamp;
                            mContentResponse.documents[0].content = String.Join(" ", words);

                            mTextItem.UnreadText = mContentResponse.documents[0].content;
                            mIsReading = false;
                            if (mplayer.CurrentState != MediaElementState.Playing)
                                mplayer.Play();
                        }
                        else
                        {
                            mTextItem.UnreadText += mContentResponse.documents[0].content;
                        }
                        mIndex = 0;
                    }
                }
                else if (mHodApp == HODApps.FIND_SIMILAR)
                {
                    var result = (FindSimilarResponse)mParser.ParseCustomResponse<FindSimilarResponse>(ref response);
                    if (result != null)
                    {
                        text = "<html><head/><body><div style=\"font-size:1.0em\">";
                        mUrlList.Clear();
                        text += "<div style=\"text-align:right\"><a href=\"hod_home\">Back to Concepts</a></div>";
                        foreach (var document in result.documents)
                        {
                            text += String.Format("<div><b>Title: </b>{0} </div>", document.title);
                            text += String.Format("<div><b>Relevance: </b>{0}%</div>", document.weight.ToString("0.00"));
                            if (document.summary != null)
                                text += String.Format("<div><b>Summary: </b>{0}</div>", document.summary);
                            if (document.reference != null)
                            {
                                text += String.Format("<div><b>Content: </b><a href=\"{0}\">website</a></div>", document.reference);
                                mUrlList.Add(document.reference);
                            }
                            text += "</br>";
                        }
                        text += "</div></body></html>";
                        processedcontent.NavigateToString(text);
                    }
                }
                else if (mHodApp == HODApps.QUERY_TEXT_INDEX)
                {
                    mInprogress = false;
                    mMediaList = (QueryTextIndexResponse)mParser.ParseCustomResponse<QueryTextIndexResponse>(ref response);
                    if (mMediaList != null)
                    {
                        mListViewModel.ClearData();
                        foreach (QueryTextIndexResponse.Document doc in mMediaList.documents)
                        {
                            ContentModel item = new ContentModel();
                            item.Type = doc.mediatype[0];
                            item.Title = doc.medianame[0];
                            if (doc.filename != null)
                                item.filename = doc.filename[0];
                            item.reference = doc.reference;
                            item.index = doc.index;

                            var type = item.Type.Split('/');
                            if (type[0] == "video")
                                item.Icon = "Assets/video_icon.png";
                            else
                                item.Icon = "Assets/audio_icon.png";

                            mListViewModel.Items.Add(item);
                        }
                    }
                }
                else if (mHodApp == HODApps.ANALYZE_SENTIMENT)
                {
                    mSaResponse = mParser.ParseSentimentAnalysisResponse(ref response);
                    if (mSaResponse != null)
                    {
                        parseSentimentAnalysis();
                    }
                }
                else if (mHodApp == HODApps.ENTITY_EXTRACTION)
                {
                    mEeResponse = (EntityExtractionResponse)mParser.ParseCustomResponse<EntityExtractionResponse>(ref response);
                    if (mEeResponse != null)
                    {
                        parseEntityExtraction();
                    }
                    else
                    {
                        text += "Error!</div></body></html>";
                        processedcontent.NavigateToString(text);
                    }
                }

            });
        }

        async private void HodClient_onErrorOccurred(string errorMessage)
        {
            await messageDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                operation.Text = errorMessage;
                mInprogress = false;
                indicator.Visibility = Visibility.Collapsed;
            });
        }

        private void Mplayer_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            switch (mplayer.CurrentState)
            {
                case MediaElementState.Playing:
                    {
                        mDelayTimer.Start();
                    }
                    break;
                case MediaElementState.Stopped:
                    mDelayTimer.Stop();
                    mIndex = 0;
                    break;
                case MediaElementState.Closed:
                    mDelayTimer.Stop();
                    mIndex = 0;
                    break;
                case MediaElementState.Paused:
                    mDelayTimer.Stop();
                    break;
            }
        }

        private void mplayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (!mIsReading)
                mplayer.Play();
        }

        private void mplayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            mIndex = 0;
            mReadText = "";
        }

        private void DelayTextChangedTimer_Tick(object sender, object e)
        {
            mDelayTextChangedTimer.Stop();
            instantSearchText();
        }
        private void instantSearchText()
        {
            var searchTerms = instantsearch.Text.ToLower();
            searchTerms = searchTerms.Trim();
            if (searchTerms.Length > 0)
            {
                GetContentResponse.Document doc = mContentResponse.documents[0];
                var words = doc.text.FindAll(item => item.ToLower().Contains(searchTerms));
                mWordsIndex.Clear();
                if (words.Count >= 1)
                {
                    var r = 0;
                    foreach (var word in words)
                    {
                        int m = doc.text.FindIndex(r, item => item.Equals(word));
                        r = m + 1;
                        if (m >= 0)
                            mWordsIndex.Add(m);
                    }
                }

                if (mWordsIndex.Count > 0)
                {
                    if (mWordsIndex.Count > 1)
                    {
                        nextwordbtn.IsEnabled = true;
                        searchwordcount.Text = "1/" + mWordsIndex.Count.ToString();
                    }
                    else
                    {
                        nextwordbtn.IsEnabled = false;
                        searchwordcount.Text = "";
                    }
                    mSearchIndex = 0;
                    int c = mWordsIndex[mSearchIndex];
                    mplayer.Position = TimeSpan.FromMilliseconds(doc.offset[c]);
                    resetReadText(c);
                    instantsearch.SelectAll();
                }
            }
        }
        private void search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (mDelayTextChangedTimer.IsEnabled)
                mDelayTextChangedTimer.Stop();
            mDelayTextChangedTimer.Start();
        }

        private void resetReadText(int pos)
        {
            GetContentResponse.Document doc = mContentResponse.documents[0];
            mIndex = pos;
            var sub = doc.text.GetRange(0, mIndex);
            mReadText = String.Join(" ", sub);

            if (mplayer.CurrentState != MediaElementState.Playing)
            {
                if (mIndex < doc.offset.Count)
                {
                    string word = doc.text[mIndex];
                    mTextItem.ReadText = mReadText;

                    int start = mIndex + 1;
                    sub = doc.text.GetRange(start, doc.offset.Count - start);
                    string leftOver = String.Join(" ", sub);
                    mReadText += " " + word;
                    mTextItem.Word = word;
                    mTextItem.UnreadText = leftOver;
                    mIndex++;
                }
            }
        }
        private void HandleFunctionButtons(FunctionID selectFunction)
        {
            mCurrentFunction = selectFunction;
            switch (mCurrentFunction)
            {
                case FunctionID.TRANSCRIPT:
                    transcriptbtn.IsEnabled = false;
                    conceptbtn.IsEnabled = true;
                    sentimentbtn.IsEnabled = true;
                    entitybtn.IsEnabled = true;
                    break;
                case FunctionID.CONCEPTS:
                    transcriptbtn.IsEnabled = true;
                    conceptbtn.IsEnabled = false;
                    sentimentbtn.IsEnabled = true;
                    entitybtn.IsEnabled = true;
                    break;
                case FunctionID.OPINIONS:
                    transcriptbtn.IsEnabled = true;
                    conceptbtn.IsEnabled = true;
                    sentimentbtn.IsEnabled = false;
                    entitybtn.IsEnabled = true;
                    break;
                case FunctionID.INTERESTS:
                    transcriptbtn.IsEnabled = true;
                    conceptbtn.IsEnabled = true;
                    sentimentbtn.IsEnabled = true;
                    entitybtn.IsEnabled = false;
                    break;
            }
        }
        private void transcriptbtn_Click(object sender, RoutedEventArgs e)
        {
            if (mContentResponse == null || mContentResponse.documents == null)
                return;
            HandleFunctionButtons(FunctionID.TRANSCRIPT);
            
            web_view.Visibility = Visibility.Collapsed;
            transcript_view.Visibility = Visibility.Visible;

            mplayer_SeekCompleted(null, null);
            if (mDelayTimer.IsEnabled)
                mDelayTimer.Stop();
            if (mplayer.CurrentState == MediaElementState.Playing)
                mDelayTimer.Start();
        }

        private void sentmentbtn_Click(object sender, RoutedEventArgs e)
        {
            if (mDelayTimer.IsEnabled)
                mDelayTimer.Stop();
            transcript_view.Visibility = Visibility.Collapsed;
            web_view.Visibility = Visibility.Visible;
            HandleFunctionButtons(FunctionID.OPINIONS);
            if (mSaResponse != null)
            {
                parseSentimentAnalysis();
            }
            else
            {
                if (mContentResponse == null || mContentResponse.documents == null)
                    return;
                var lang = "eng";
                if (mContentResponse.documents[0].language != null)
                {
                    if (LanguageCollection.ContainsKey(mContentResponse.documents[0].language[0]))
                        lang = LanguageCollection[mContentResponse.documents[0].language[0]];
                    else
                    {
                        operation.Text = "Language is not supported for Sentiment Analysis!";
                        return;
                    }
                }
                var Params = new Dictionary<string, object>()
                {
                    {"text", mContentResponse.documents[0].content },
                    {"language", lang }
                };
                mHodApp = HODApps.ANALYZE_SENTIMENT;
                operation.Text = "Analyzing content.";
                indicator.Visibility = Visibility.Visible;
                mHodClient.PostRequest(ref Params, mHodApp, HODClient.REQ_MODE.SYNC);
            }
        }

        private void conceptbtn_Click(object sender, RoutedEventArgs e)
        {
            if (mContentResponse == null || mContentResponse.documents == null)
                return;
            HandleFunctionButtons(FunctionID.CONCEPTS);
            if (mDelayTimer.IsEnabled)
                mDelayTimer.Stop();
            transcript_view.Visibility = Visibility.Collapsed;
            web_view.Visibility = Visibility.Visible;

            var text = "<html><head/><body><div style=\"text-align:center\">";
            string concepts = "";
            foreach (GetContentResponse.Document doc in mContentResponse.documents)
            {
                for (int i = 0; i < doc.concepts.Count; i++)
                {
                    var concept = doc.concepts[i];
                    if (concept.Length > 2)
                    {
                        var occurrences = doc.occurrences[i];
                        if (occurrences > 20)
                            concepts += "<a href=\"hod_link:" + concept + "\"><span style=\"font-size:1.5em;color:blue\">" + concept + "</span></a> - ";
                        else if (occurrences > 15)
                            concepts += "<a href=\"hod_link:" + concept + "\"><span style=\"font-size:1.5em;color:orange\">" + concept + "</span></a> - ";
                        else if (occurrences > 10)
                            concepts += "<a href=\"hod_link:" + concept + "\"><span style=\"font-size:1.5em;color:blue\">" + concept + "</span></a> - ";
                        else if (occurrences > 5)
                            concepts += "<a href=\"hod_link:" + concept + "\"><span style=\"font-size:1.5em;color:orange \">" + concept + "</span></a> - ";
                        else if (occurrences >= 3)
                            concepts += "<a href=\"hod_link:" + concept + "\"><span style =\"font-size:1.8em;color:blue\">" + concept + "</span></a> - ";
                    }
                }
                if (concepts.Length > 3)
                    concepts = concepts.Substring(0, concepts.Length - 3);
                text += concepts;
                text += "</div></body></html>";
                processedcontent.NavigateToString(text);
            }
        }

        private void entitybtn_Click(object sender, RoutedEventArgs e)
        {
            if (mDelayTimer.IsEnabled)
                mDelayTimer.Stop();
            transcript_view.Visibility = Visibility.Collapsed;
            web_view.Visibility = Visibility.Visible;

            HandleFunctionButtons(FunctionID.INTERESTS);
            if (mEeResponse != null)
            {
                parseEntityExtraction();
            }
            else
            {
                if (mContentResponse == null || mContentResponse.documents == null)
                    return;
                var Params = new Dictionary<string, object>();

                Params.Add("text", mContentResponse.documents[0].content);
                var entity_type = new List<object>();
                entity_type.Add("people_eng");
                entity_type.Add("places_eng");
                entity_type.Add("companies_eng");

                Params.Add("entity_type", entity_type);
                Params.Add("unique_entities", "true");

                mHodApp = HODApps.ENTITY_EXTRACTION;
                operation.Text = "Extracting content.";
                indicator.Visibility = Visibility.Visible;
                mHodClient.PostRequest(ref Params, mHodApp, HODClient.REQ_MODE.SYNC);
            }
        }
        private void parseSentimentAnalysis()
        {
            String text = "<html><head/><body><div style=\"font-size:1.1em;color:gray\">";
            string posStatementStartTag = "<span style='color:green'><b>";
            string posStatementEndTag = "</b></span>";
            string posSentimentStartTag = "<span style='text-decoration:underline'>";
            string posSentimentEndTag = "</span>";

            string negStatementStartTag = "<span style='color:red'><b>";
            string negStatementEndTag = "</b></span>";
            string negSentimentStartTag = "<span style='text-decoration:underline'>";
            string negSentimentEndTag = "</span>";

            string body = mContentResponse.documents[0].content;
            if (mSaResponse.positive.Count > 0)
            {
                foreach (var item in mSaResponse.positive)
                {
                    body = body.Replace(item.original_text, posStatementStartTag + item.original_text + posStatementEndTag);
                    if (item.sentiment != null)
                        body = body.Replace(item.sentiment, posSentimentStartTag + item.sentiment + posSentimentEndTag);
                }
            }
            if (mSaResponse.negative.Count > 0)
            {
                foreach (var item in mSaResponse.negative)
                {
                    body = body.Replace(item.original_text, negStatementStartTag + item.original_text + negStatementEndTag);
                    if (item.sentiment != null)
                        body = body.Replace(item.sentiment, negSentimentStartTag + item.sentiment + negSentimentEndTag);
                }
            }
            text += body + "</div></body></html>";
            processedcontent.NavigateToString(text);
        }
        private void parseEntityExtraction()
        {
            String text = "<html><head/><body><div>";
            var highlightText = "";
            mUrlList.Clear();
            if (mEeResponse.entities.Count > 0)
            {
                foreach (var entity in mEeResponse.entities)
                {
                    highlightText += String.Format("\"{0}\",", entity.original_text);
                    if (entity.type == "companies_eng")
                    {
                        text += "<b>Company name:</b> " + entity.normalized_text + "</br>";

                        if (entity.additional_information != null)
                        {
                            string url = "";
                            if (entity.additional_information.wikipedia_eng != null)
                            {
                                text += "<b>Wiki page: </b><a href=\"";

                                if (!entity.additional_information.wikipedia_eng.Contains("http"))
                                {
                                    url = "http://" + entity.additional_information.wikipedia_eng;
                                }
                                else
                                {
                                    url = entity.additional_information.wikipedia_eng;
                                }
                                text += url + "\">";
                                text += url + "</a>";
                                mUrlList.Add(url);
                                text += "</br>";
                            }
                            if (entity.additional_information.url_homepage != null)
                            {
                                text += "<b>Home page: </b><a href=\"";
                                if (!entity.additional_information.url_homepage.Contains("http"))
                                {
                                    url = "http://" + entity.additional_information.url_homepage;
                                }
                                else
                                {
                                    url = entity.additional_information.url_homepage;
                                }
                                text += url + "\">";
                                text += url + "</a>";
                                mUrlList.Add(url);
                                text += "</br>";
                            }
                            if (entity.additional_information.company_wikipedia != null)
                            {
                                var wikiPage = "";
                                foreach (var p in entity.additional_information.company_wikipedia)
                                    wikiPage += p + ", ";
                                if (wikiPage.Length > 3)
                                    wikiPage = wikiPage.Substring(0, wikiPage.Length - 2);
                                text += "<b>Wikipedia:</b> " + wikiPage + "</br>";
                            }
                            if (entity.additional_information.company_ric != null)
                            {
                                var wikiPage = "";
                                foreach (var p in entity.additional_information.company_ric)
                                    wikiPage += p + ", ";
                                if (wikiPage.Length > 3)
                                    wikiPage = wikiPage.Substring(0, wikiPage.Length - 2);
                                text += "<b>RIC:</b> " + wikiPage + "</br>";
                            }
                        }
                    }
                    else if (entity.type == "places_eng")
                    {
                        text += "<b>Place name:</b> " + entity.normalized_text + "</br>";

                        if (entity.additional_information != null)
                        {
                            string url = "";
                            if (entity.additional_information.place_population != 0)
                            {
                                double pop = (double)entity.additional_information.place_population;
                                var population = pop.ToString();
                                if (pop > 1000000)
                                {
                                    pop /= 1000000;
                                    population = pop.ToString(".00") + " million";
                                }
                                text += "<b>Population:</b> " + population + "</br>";
                            }
                            if (entity.additional_information.image != null)
                            {
                                text += "<img src=\"";
                                text += entity.additional_information.image + "\" width=\"90%\"/>";
                                text += "</br>";
                            }
                            if (entity.additional_information.wikipedia_eng != null)
                            {
                                text += "<b>Wiki page: </b><a href=\"";
                                if (!entity.additional_information.wikipedia_eng.Contains("http"))
                                {
                                    url = "http://";
                                }
                                else
                                {
                                    url = entity.additional_information.wikipedia_eng;
                                }
                                text += url + "\">";
                                text += url + "</a>";
                                mUrlList.Add(url);
                                text += "</br>";
                            }
                            if (entity.additional_information.lat != 0.0 && entity.additional_information.lon != 0.0)
                            {

                                var zoom = "10z";
                                if (entity.additional_information.place_type != null)
                                {
                                    switch (entity.additional_information.place_type)
                                    {
                                        case "region1":
                                            zoom = ",6z";
                                            break;
                                        case "continent":
                                            zoom = ",5z";
                                            break;
                                        case "area":
                                            zoom = ",7z";
                                            break;
                                        case "country":
                                            zoom = ",4z";
                                            break;
                                        case "populated place":
                                            zoom = ",10z";
                                            break;
                                        default:
                                            zoom = ",12z";
                                            break;
                                    }
                                }
                                text += "<b>Map: </b><a href=\"https://www.google.com/maps/@" + entity.additional_information.lat + "," + entity.additional_information.lon + zoom + "\">";
                                text += "Show map</a></br>";
                            }

                        }
                    }
                    else if (entity.type == "people_eng")
                    {
                        text += "<b>People name:</b> " + entity.normalized_text + "</br>";

                        if (entity.additional_information != null)
                        {
                            if (entity.additional_information.person_profession != null)
                            {
                                var prof = "";
                                foreach (var p in entity.additional_information.person_profession)
                                    prof += p + ", ";
                                if (prof.Length > 3)
                                    prof = prof.Substring(0, prof.Length - 2);
                                text += "<b>Profession:</b> " + prof + "</br>";
                            }
                            if (entity.additional_information.person_date_of_birth != null)
                            {
                                text += "<b>DoB:</b> " + entity.additional_information.person_date_of_birth + "</br>";
                            }
                            if (entity.additional_information.person_date_of_death != null)
                            {
                                text += "<b>DoD:</b> " + entity.additional_information.person_date_of_death + "</br>";
                            }
                            if (entity.additional_information.image != null)
                            {
                                text += "<img src=\"";
                                text += entity.additional_information.image + "\" width=\"90%\"/>";
                                text += "</br>";
                            }
                            if (entity.additional_information.wikipedia_eng != null)
                            {
                                string url = "";
                                text += "<b>Wiki page: </b><a href=\"";
                                if (!entity.additional_information.wikipedia_eng.Contains("http"))
                                {
                                    url = "http://";
                                }
                                else
                                {
                                    url = entity.additional_information.wikipedia_eng;
                                }
                                text += url + "\">";
                                text += url + "</a>";
                                mUrlList.Add(url);
                                text += "</br>";
                            }
                        }
                    }
                    
                    text += "<div style=\"text-align:center\">@@@@@@@@@@@@@@@</div>";
                }
                text += "</div></body></html>";
                processedcontent.NavigateToString(text);
            }
            else
            {
                text = "Not found</div></body></html>";
                processedcontent.NavigateToString(text);
            }
        }
        private void nextwordbtn_Click(object sender, RoutedEventArgs e)
        {
            GetContentResponse.Document doc = mContentResponse.documents[0];

            mSearchIndex++;
            if (mSearchIndex >= mWordsIndex.Count)
                mSearchIndex = 0;
            int i = mWordsIndex[mSearchIndex];
            mplayer.Position = TimeSpan.FromMilliseconds(doc.offset[i]);
            resetReadText(i);
            int c = mSearchIndex + 1;
            searchwordcount.Text = String.Format("{0} / {1}", c, mWordsIndex.Count);
        }

        private void instantsearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (instantsearch.Text.Length > 0)
                instantsearch.Text = "";
        }

        private void mplayer_SeekCompleted(object sender, RoutedEventArgs e)
        {
            GetContentResponse.Document doc = mContentResponse.documents[0];
            if (doc == null)
                return;

            var pos = mplayer.Position.TotalMilliseconds;
            int max = doc.offset.Count - 1;
            for (int i = 0; i < max; i++)
            {
                var low = doc.offset[i];
                var high = doc.offset[i + 1];
                if (pos > low && pos < high)
                {
                    resetReadText(i);
                    break;
                }
            }
        }

        private void runningText_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (runningText.SelectedText.Length > 0)
                instantsearch.Text = runningText.SelectedText;
        }
    }
}