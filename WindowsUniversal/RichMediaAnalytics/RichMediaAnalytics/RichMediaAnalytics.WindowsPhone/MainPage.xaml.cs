using HOD.Client;
using HOD.Response.Parser;
using RichMediaAnalytics.DataModel;
using RichMediaAnalytics.DataModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace RichMediaAnalytics
{
    public sealed partial class MainPage : Page
    {
        Windows.UI.Core.CoreDispatcher messageDispatcher = Window.Current.CoreWindow.Dispatcher;

        HODClient mHodClient = null;
        string jobID = "";
        HODResponseParser parser = new HODResponseParser();
        QueryTextIndexResponse mediaList = null;
        MetaDataModel infoText = new MetaDataModel();

        ContentListViewModel listViewModel = null;
        bool inprogress = false;
        bool mInitialized = false;
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            listViewModel = App.GetMediaListView();
            videoList.DataContext = listViewModel;
            searchVideos.KeyDown += SearchVideos_KeyDown;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
            if (mHodClient == null)
            {
                mHodClient = App.GetHODClient();
            }
            mHodClient.onErrorOccurred += HodClient_onErrorOccurred;
            mHodClient.requestCompletedWithContent += HodClient_requestCompletedWithContent;
            mHodClient.requestCompletedWithJobID += HodClient_requestCompletedWithJobID;
            mInitialized = true;
            if (listViewModel.Items.Count == 0)
                searchMedia_Click(null, null);
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            mHodClient.onErrorOccurred -= HodClient_onErrorOccurred;
            mHodClient.requestCompletedWithContent -= HodClient_requestCompletedWithContent;
            mHodClient.requestCompletedWithJobID -= HodClient_requestCompletedWithJobID;
            base.OnNavigatedFrom(e);
        }
        private void HodClient_requestCompletedWithJobID(string response)
        {
            jobID = parser.ParseJobID(response);
            if (jobID != "")
                mHodClient.GetJobStatus(jobID);
        }
        
        async private void HodClient_requestCompletedWithContent(string response)
        {
            await messageDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                indicator.Visibility = Visibility.Collapsed;
                inprogress = false;
                mediaList = (QueryTextIndexResponse)parser.ParseCustomResponse<QueryTextIndexResponse>(ref response);
                    if (mediaList != null)
                    {
                        listViewModel.ClearData();
                        foreach (QueryTextIndexResponse.Document doc in mediaList.documents)
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

                            listViewModel.Items.Add(item);
                        }
                    }
            });
        }

        async private void HodClient_onErrorOccurred(string errorMessage)
        {
            await messageDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                operation.Text = errorMessage;
                indicator.Visibility = Visibility.Collapsed;
                loadingindicator.IsEnabled = false;
            });
        }

        private void SearchVideos_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (inprogress)
                    return;
                Windows.UI.ViewManagement.InputPane.GetForCurrentView().TryHide();
                searchMedia_Click(null, null);
            }
        }
        private void media_type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mInitialized)
            {
                media_type_container.Visibility = Visibility.Collapsed;
                ListBoxItem type = (ListBoxItem) (sender as ListBox).SelectedItem;
                change_media_type_btn.Content = type.Content;
                searchMedia_Click(null, null);
            }
        }
        private void searchMedia_Click(object sender, RoutedEventArgs e)
        {
            if (App.mHodApiKey.Length == 0)
            {
                info_dlg.IsOpen = true;
                infoMessage.Text = "Please provide your apikey to mHodApiKey then recompile the project!";
                return;
            }
            if (inprogress)
                return;
            var arg = searchVideos.Text;
            if (arg.Length == 0)
                arg = "*";
            else
                arg = "\"" + arg + "\"";

            var Params = new Dictionary<string, object>()
            {
                {"indexes", "speechindex" },
                {"text", arg },
                {"print_fields", "medianame,mediatype,filename" },
                {"absolute_max_results", 100 }
            };
            var item = (ListBoxItem)media_type.SelectedItem;
            var media = (string)item.Tag;
            if (media == "video")
            {
                Params.Add("field_text", "MATCH{video/mp4}:mediatype");
            }
            else if (media == "audio")
            {
                Params.Add("field_text", "MATCH{audio/mpeg,audio/mp4}:mediatype");
            }
            if (mediaList != null && mediaList.documents != null)
                mediaList.documents.Clear();
            searchVideos.SelectAll();
            inprogress = true;
            string hodApp = HODApps.QUERY_TEXT_INDEX;
            indicator.Visibility = Visibility.Visible;
            mHodClient.PostRequest(ref Params, hodApp, HODClient.REQ_MODE.SYNC);
        }
        private void videoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = (sender as ListView).SelectedIndex;
            if (index != -1)
            {
                var item = listViewModel.Items[index];
                this.Frame.Navigate(typeof(Playback), item);
            }
        }

        private void change_media_type_btn_Click(object sender, RoutedEventArgs e)
        {
            media_type_container.Visibility = Visibility.Visible;
        }
    }
}
