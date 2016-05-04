package com.havenondemand.richmediaanalytics;

import android.app.ListActivity;
import android.content.Context;
import android.content.Intent;
import android.graphics.Color;
import android.media.MediaPlayer;
import android.net.Uri;
import android.os.Handler;
import android.os.Bundle;
import android.text.TextUtils;
import android.view.KeyEvent;
import android.view.LayoutInflater;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewGroup;
import android.view.inputmethod.InputMethodManager;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.ListView;
import android.widget.MediaController;
import android.widget.ProgressBar;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;
import android.widget.VideoView;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import hod.api.hodclient.HODApps;
import hod.api.hodclient.HODClient;
import hod.api.hodclient.IHODClientCallback;
import hod.response.parser.HODResponseParser;
import hod.response.parser.SentimentAnalysisResponse;

public class MainActivity extends ListActivity implements IHODClientCallback, MediaPlayer.OnPreparedListener, MediaController.MediaPlayerControl {

    class ItemObject {
        String reference;
        String index;
        String medianame;
        String mediatype;
        String filename;
    }

    private Map<String, String> languageCollection;

    private ListView mMediaList;
    private ProgressBar mPbloading;
    private LinearLayout mMediaListView;
    private LinearLayout mMediaPlayView;
    private WebView mMediaContent;
    private VideoView mMediaPlayer;
    private EditText mMediaSearch;
    private EditText mInstantSearch;
    private TextView mSearchCount;
    private Button mNextWordBtn;
    private Spinner mMediaType;

    private ArrayList<ItemObject> m_templates = null;
    private MediaItemTemplates m_adapter;

    private ItemObject mSelectedMedia;

    private HODClient mHodClient;
    private HODResponseParser mParser;
    private String mHodApp = "";
    private GetContentResponse.Document mMediaData;
    SentimentAnalysisResponse mSaResponse = null;
    EntityExtractionResponse mEeResponse = null;

    private Handler mRunningTextTimer = null;
    int mIndex = 0;

    String htmlStart = "<html><body><div>";
    String htmlEnd = "</div></body></html>";
    String readTextStart = "<span style=\"color:green\">";
    String wordTextStart = "<span style=\"color:red\">";
    String unreadTextStart = "<span style=\"color:gray\">";
    String spanEnd = "</span>";

    String mReadText = "";
    String mUnreadText = "";
    int mMaxReadingBlock = 100;
    int mReadBlockCount = 1;
    List<Integer> mSearchWordCount;
    int mCurrentInstantSearchIndex = 0;

    private MediaController mMcontroller;
    enum VIEW_ID { MEDIA_LIST, PLAY_VIEW }
    VIEW_ID viewID = VIEW_ID.MEDIA_LIST;

    private String mHodApiKey = ""; // place you apikey here

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        languageCollection = new HashMap<String, String>();
        languageCollection.put("en-US", "eng");
        languageCollection.put("en-GB", "eng");
        languageCollection.put("es-ES", "spa");
        languageCollection.put("de-DE", "ger");
        languageCollection.put("fr-FR", "fre");
        languageCollection.put("it-IT", "ita");
        languageCollection.put("zh-CN", "chi");

        mSearchWordCount = new ArrayList<>();
    }

    @Override
    protected void onPostCreate(Bundle savedInstanceState) {
        super.onPostCreate(savedInstanceState);
        mPbloading = (ProgressBar) findViewById(R.id.pbloading);
        mMediaListView = (LinearLayout) findViewById(R.id.medialistview);
        mMediaPlayView = (LinearLayout) findViewById(R.id.playmediaview);
        mMediaContent = (WebView) findViewById(R.id.media_content_wv);
        mMediaPlayer = (VideoView) findViewById(R.id.media_player_vv);

        mMediaSearch = (EditText) findViewById(R.id.search_media_et);
        mInstantSearch = (EditText) findViewById(R.id.instant_search_et);
        mSearchCount = (TextView) findViewById(R.id.search_count);
        mNextWordBtn = (Button) findViewById(R.id.next_word);
        mMediaType = (Spinner) findViewById(R.id.media_type);

        m_templates = new ArrayList<ItemObject>();
        mMediaList = (ListView) findViewById(android.R.id.list);
        this.m_adapter = new MediaItemTemplates(this, R.layout.row, m_templates);
        setListAdapter(this.m_adapter);
        mMediaList.setOnItemClickListener(new AdapterView.OnItemClickListener() {
            public void onItemClick(AdapterView<?> adapter, View myView, int myItemInt, long mylng) {
                mMediaListView.setVisibility(View.GONE);
                mMediaPlayView.setVisibility(View.VISIBLE);
                viewID = VIEW_ID.PLAY_VIEW;
                mSelectedMedia = m_templates.get(myItemInt);

                String[] ext = mSelectedMedia.mediatype.split("/");
                if (ext[0].equals("audio"))
                    mMediaPlayer.setBackgroundResource(R.drawable.audio_background);
                else
                    mMediaPlayer.setBackgroundColor(Color.TRANSPARENT);

                GetMediaContent();
            }
        });

        mMediaType.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener()
        {
            @Override
            public void onItemSelected(AdapterView<?> arg0, View arg1,int position, long id) {
                // TODO Auto-generated method stub
                SearchMedia();
            }

            @Override
            public void onNothingSelected(AdapterView<?> arg0) {
                // TODO Auto-generated method stub
            }
        });

        mHodClient = new HODClient(mHodApiKey, this);
        mParser = new HODResponseParser();

        mMediaPlayer.setOnCompletionListener(new MediaPlayer.OnCompletionListener() {
            public void onCompletion(MediaPlayer mp) {
                mMediaPlayer.seekTo(0);
                mIndex = 0;
                mReadText = "";
                mCurrentInstantSearchIndex = -1;
                mReadBlockCount = 1;
                if (mRunningTextTimer != null)
                    mRunningTextTimer.removeCallbacks(runningText);
                mRunningTextTimer = null;
                List<String> sub = mMediaData.text.subList(0, mMaxReadingBlock);
                String paragraph = TextUtils.join(" ", sub) + "...";
                mMediaContent.loadData("<html><body><div>" + paragraph + "</div></body></html>", "text/html; charset=utf-8", null);
            }
        });

        mMediaPlayer.setOnPreparedListener(new MediaPlayer.OnPreparedListener() {
            @Override
            public void onPrepared(MediaPlayer mp) {
                if (viewID == VIEW_ID.PLAY_VIEW)
                    start();
            }
        });
        mRunningTextTimer = new Handler();

        mInstantSearch.setOnFocusChangeListener(new View.OnFocusChangeListener() {
            @Override
            public void onFocusChange(View v, boolean hasFocus) {
                if (hasFocus) {
                    mInstantSearch.setText("");
                } else {
                    mInstantSearch.setText("");
                }
            }
        });

        mInstantSearch.setOnEditorActionListener(new TextView.OnEditorActionListener() {
            @Override
            public boolean onEditorAction(TextView v, int actionId, KeyEvent event) {
                boolean handled = false;
                if (actionId == 6) {
                    searchText();
                    InputMethodManager in = (InputMethodManager) getSystemService(Context.INPUT_METHOD_SERVICE);
                    in.hideSoftInputFromWindow(mInstantSearch.getApplicationWindowToken(), InputMethodManager.HIDE_NOT_ALWAYS);
                    handled = true;
                }
                return handled;
            }
        });
        mMediaSearch.setOnEditorActionListener(new TextView.OnEditorActionListener() {
            @Override
            public boolean onEditorAction(TextView v, int actionId, KeyEvent event) {
                boolean handled = false;
                if (actionId == 5 || actionId == 6) {
                    SearchMedia();
                    InputMethodManager in = (InputMethodManager) getSystemService(Context.INPUT_METHOD_SERVICE);
                    in.hideSoftInputFromWindow(mInstantSearch.getApplicationWindowToken(), InputMethodManager.HIDE_NOT_ALWAYS);
                    handled = true;
                }
                return handled;
            }
        });
        mMediaContent.setWebViewClient(new WebViewClient() {
            // you tell the webclient you want to catch when a url is about to load
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, String url) {
                boolean handled = false;
                if (url.contains("hod_link")) {
                    String[] concept = url.split(":");
                    String temp = concept[2].substring(0, concept[2].length()-1);
                    CallFindSimilar(temp);
                    handled = true;
                } else {
                    view.getContext().startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse(url)));
                    handled = true;
                }
                return handled;
            }

            // here you execute an action when the URL you want is about to load
            @Override
            public void onLoadResource(WebView view, String url) {
                if (url.equals("http://cnn.com")) {
                    // do whatever you want
                }
            }
            @Override
            public void onPageFinished(WebView view, String url) {
                mPbloading.setVisibility(View.INVISIBLE);
            }
        });

        mMcontroller = new MediaController(this);
        mMcontroller.setMediaPlayer(this);
        mMcontroller.setAnchorView(findViewById(R.id.media_player_vv));
        mMcontroller.hide();
    }
    private void CallFindSimilar(String concept) {
        String lang = "en-US";
        if (mMediaData.language != null)
            lang = mMediaData.language.get(0);

        Map params = new HashMap<String, Object>();
        List<String> indexes = new ArrayList<String>();
        if (lang.equals("en-US") || lang.equals("en-GB")) {
            indexes.add("wiki_eng");
            indexes.add("news_eng");
        } else if (lang.equals("it-IT")) {
            indexes.add("wiki_ita");
            indexes.add("news_ita");
        } else if (lang.equals("fr-FR")) {
            indexes.add("wiki_fra");
            indexes.add("news_fra");
        } else if (lang.equals("de-DE")) {
            indexes.add("wiki_ger");
            indexes.add("news_ger");
        } else if (lang.equals("es-ES")) {
            indexes.add("wiki_spa");
        } else if (lang.equals("zh-CN")) {
            indexes.add("wiki_chi");
        }
        params.put("indexes", indexes);
        params.put("text", concept);
        params.put("print_fields", "title,reference,summary,weight");

        mHodApp = HODApps.FIND_SIMILAR;
        StopRunningText();
        mPbloading.setVisibility(View.VISIBLE);
        mHodClient.PostRequest(params, mHodApp, HODClient.REQ_MODE.SYNC);
    }
    private void searchText() {
        String searchTerm = mInstantSearch.getText().toString();
        mNextWordBtn.setVisibility(View.GONE);
        if (searchTerm.length() > 0) {
            mSearchWordCount.clear();
            mSearchWordCount = indexOfAll(searchTerm);
            if (mSearchWordCount.size() > 0) {
                mCurrentInstantSearchIndex = -1;
                SearchNextWord(null);
                if (mSearchWordCount.size() > 1)
                    mNextWordBtn.setVisibility(View.VISIBLE);
            }
        }
    }
    private ArrayList<Integer> indexOfAll(String word){
        ArrayList<Integer> indexList = new ArrayList<Integer>();

        for (int i = 0; i < mMediaData.text.size(); i++) {
            String temp = mMediaData.text.get(i);
            if (temp.endsWith(".") || temp.endsWith(","))
                temp = temp.substring(0, temp.length() - 1);
            if (word.equalsIgnoreCase(temp))
                indexList.add(i);
        }
        return indexList;
    }
    public void SearchNextWord(View v) {
        mCurrentInstantSearchIndex++;
        if (mCurrentInstantSearchIndex >= mSearchWordCount.size()) {
            mCurrentInstantSearchIndex = 0;
        }
        int i = mSearchWordCount.get(mCurrentInstantSearchIndex);
        int offset = mMediaData.offset.get(i);
        mMediaPlayer.seekTo(offset);
        resetReadText(i);
        int count = mCurrentInstantSearchIndex + 1;
        mSearchCount.setText(String.format("%d/%d", count, mSearchWordCount.size()));
    }
    private void resetReadText(int pos) {
        if (mRunningTextTimer != null)
            mRunningTextTimer.removeCallbacks(runningText);

        mIndex = pos;
        mReadText = "";
        mReadBlockCount = 1;
        int max = pos - 1;

        max = (max < 0) ? 0 : max;

        mReadText += mMediaData.text.get(max) + " ";

        if (mMediaPlayer.isPlaying()) {
            if (mRunningTextTimer == null)
                mRunningTextTimer = new Handler();
            mRunningTextTimer.postDelayed(runningText, 100);
        }
    }
    private Runnable runningText = new Runnable() {
        @Override
        public void run() {
            int count = mMediaData.offset.size();

            if (mIndex < count) {
                int pos = getCurrentPosition();
                int check = mMediaData.offset.get(mIndex);
                String word = "";
                if (pos > check) {
                    word = mMediaData.text.get(mIndex);
                    mIndex++;

                    if (mReadBlockCount > mMaxReadingBlock) {
                        mReadText = "";
                        mReadBlockCount = 1;
                    }

                    mUnreadText = " ";
                    int max = mMaxReadingBlock - mReadBlockCount;
                    if (max + mIndex > count) {
                        max = count - mIndex;
                        List<String> sub = mMediaData.text.subList(mIndex, mIndex + max);
                        mUnreadText += TextUtils.join(" ", sub);
                        mUnreadText += ".";
                    } else {
                        List<String> sub = mMediaData.text.subList(mIndex, mIndex + max);
                        mUnreadText += TextUtils.join(" ", sub);
                        mUnreadText += "...";
                    }

                    String htmlDoc = htmlStart + readTextStart + mReadText + spanEnd;
                    htmlDoc += wordTextStart + word + spanEnd;
                    htmlDoc += unreadTextStart + mUnreadText + spanEnd;
                    htmlDoc += htmlEnd;

                    mMediaContent.loadData(htmlDoc, "text/html; charset=utf-8",null);
                    mReadText += word + " ";
                    mReadBlockCount++;
                }
            }
            mRunningTextTimer.postDelayed(this, 200);
        }
    };

    @Override
    public void onBackPressed() {
        if (mMediaPlayView.getVisibility() == View.VISIBLE) {
            if (mMediaPlayer.isPlaying())
                mMediaPlayer.stopPlayback();
            mMediaPlayer.setVideoURI(null);
            ReturnToListView();
            return;
        } else {
            finish();
            super.onBackPressed();
        }
    }

    public void ReturnToListView() {
        mReadText = "";
        mMediaContent.loadData("", "","");
        mIndex = 0;
        mReadBlockCount = 1;
        mSaResponse = null;
        mEeResponse = null;

        mSearchWordCount.clear();
        mSearchCount.setText("0/0");
        mNextWordBtn.setVisibility(View.GONE);
        mMediaListView.setVisibility(View.VISIBLE);
        mMediaPlayView.setVisibility(View.GONE);
        mMediaPlayer.setVisibility(View.GONE);
        mMediaPlayer.setVisibility(View.VISIBLE);
        viewID = VIEW_ID.MEDIA_LIST;
    }

    @Override
    public void onPrepared(MediaPlayer mediaplayer) {
        this.start();
    }

    @Override
    public boolean onTouchEvent(MotionEvent event) {
        if (viewID == VIEW_ID.PLAY_VIEW)
            mMcontroller.show();
        return false;
    }

    @Override
    public void start() {
        mMediaPlayer.start();
        if (mRunningTextTimer == null)
            mRunningTextTimer = new Handler();
        mRunningTextTimer.postDelayed(runningText, 200);
        mMcontroller.show();
    }
    @Override
    public void pause() {
        mMediaPlayer.pause();
        if (mRunningTextTimer != null)
            mRunningTextTimer.removeCallbacks(runningText);
        mRunningTextTimer = null;

    }
    @Override
    public int getCurrentPosition() {
        return mMediaPlayer.getCurrentPosition();
    }
    @Override
    public void seekTo(int pos) {
        mMediaPlayer.seekTo(pos);
        if (mMediaData != null) {
            int max = mMediaData.offset.size() - 1;
            for (int i = 0; i < max; i++) {
                int low = mMediaData.offset.get(i);
                int high = mMediaData.offset.get(i+1);
                if (pos > low && pos < high) {
                    resetReadText(i);
                    break;
                }
            }
        }
    }
    @Override
    public boolean isPlaying() {
        return mMediaPlayer.isPlaying();
    }
    @Override
    public int getBufferPercentage() {
        return 0;
    }
    @Override
    public boolean canPause() {
        return true;
    }
    @Override
    public boolean canSeekBackward() {
        return true;
    }
    @Override
    public boolean canSeekForward() {
        return true;
    }

    @Override
    public int getDuration() {
        return mMediaPlayer.getDuration();
    }

    @Override
    public int getAudioSessionId() {
        return 0;
    }

    @Override
    public void onErrorOccurred(String errorMessage) {
        Toast.makeText(this, errorMessage, Toast.LENGTH_LONG).show();
        mPbloading.setVisibility(View.INVISIBLE);
    }

    @Override
    public void requestCompletedWithJobID(String response) {
        String jobID = mParser.ParseJobID(response);
        if (jobID.length() > 0)
            mHodClient.GetJobStatus(jobID);
    }

    @Override
    public void requestCompletedWithContent(String response) {
        mPbloading.setVisibility(View.INVISIBLE);
        if (mHodApp.equals(HODApps.GET_CONTENT)) {
            GetContentResponse resp = (GetContentResponse) mParser.ParseCustomResponse(GetContentResponse.class, response);
            if (resp != null) {
                for (GetContentResponse.Document doc : resp.documents) {
                    mMediaData = doc;
                    break;
                }
                // punctuation manipulation
                int count = mMediaData.text.size();
                int start = mMediaData.text.size() > 10 ? 10 : 0;
                int end = mMediaData.text.size() > 200 ? 150 : mMediaData.text.size();
                int total = 0;

                for (int i = start; i < end; i++) {
                    total += mMediaData.offset.get(i) - mMediaData.offset.get(i - 1);
                }
                int average = total / (end - 1);
                int para = average + 1500;
                int dot = average + 600;
                int commas = average + 400;
                int max = count-1;
                int len = 0;
                // capitalize the first word
                for (int i = 0; i < count; i++) {
                    if (!mMediaData.text.get(i).equals("<Music/Noise>")) {
                        mMediaData.text.set(i, firstCharacterToUpperCase(mMediaData.text.get(i)));
                        break;
                    }
                }

                for (int i = 1; i < max; i++) {
                    if (!mMediaData.text.get(i).equals("<Music/Noise>") && !mMediaData.text.get(i-1).equals("<Music/Noise>"))
                    {
                        int diff = mMediaData.offset.get(i) - mMediaData.offset.get(i - 1);
                        if (diff > para && len > 1)
                        {
                            String word = mMediaData.text.get(i - 1) + ".";
                            mMediaData.text.set(i - 1, word);
                            mMediaData.text.set(i, firstCharacterToUpperCase(mMediaData.text.get(i)));
                            len = 0;
                        }
                        else if (diff > dot && len > 1)
                        {
                            String word = mMediaData.text.get(i - 1) + ".";
                            mMediaData.text.set(i - 1, word);
                            mMediaData.text.set(i, firstCharacterToUpperCase(mMediaData.text.get(i)));
                            len = 0;
                        }
                        else if (diff > commas && len > 1)
                        {
                            String word = mMediaData.text.get(i - 1) + ",";
                            mMediaData.text.set(i - 1, word);
                            len = 0;
                        } else {
                            len++;
                        }
                    }
                }
                //
                mMediaData.content = TextUtils.join(" ", mMediaData.text);
                if (mMediaData.text.size() < mMaxReadingBlock)
                    mMaxReadingBlock = mMediaData.text.size();
                List<String> sub = mMediaData.text.subList(0, mMaxReadingBlock);
                String paragraph = TextUtils.join(" ", sub) + "...";
                mMediaContent.loadData("<html><body><div>"+paragraph+"</div></body></html>", "text/html; charset=utf-8",null);
                // play media
                String VideoURL = "http://www.hodshowcase.com/media/" + mSelectedMedia.filename;
                Uri video = Uri.parse(VideoURL);
                mMediaPlayer.setVideoURI(video);

            }
        } else if (mHodApp.equals(HODApps.QUERY_TEXT_INDEX)) {
            QueryTextIndexResponse resp = (QueryTextIndexResponse) mParser.ParseCustomResponse(QueryTextIndexResponse.class, response);
            if (resp != null) {
                for (QueryTextIndexResponse.Document doc : resp.documents) {
                    ItemObject item = new ItemObject();
                    item.reference = doc.reference;
                    item.index = doc.index;

                    item.medianame = doc.medianame.get(0);
                    item.mediatype = doc.mediatype.get(0);
                    if (doc.filename != null)
                        item.filename = doc.filename.get(0);

                    m_templates.add(item);
                }
                m_adapter.notifyDataSetChanged();
            }
        } else if (mHodApp.equals(HODApps.ANALYZE_SENTIMENT)) {
            mSaResponse = mParser.ParseSentimentAnalysisResponse(response);
            if (mSaResponse != null) {
                displayOpinions();
            } else {
                mMediaContent.loadData("<html><body><div>Error!</div></body></html>", "text/html; charset=utf-8",null);
            }
        } else if (mHodApp.equals(HODApps.ENTITY_EXTRACTION)) {
            mEeResponse = (EntityExtractionResponse) mParser.ParseCustomResponse(EntityExtractionResponse.class, response);
            if (mEeResponse != null) {
                displayInterests();
            } else {
                mMediaContent.loadData("<html><body><div>Error!</div></body></html>", "text/html; charset=utf-8",null);
            }
        } else if (mHodApp.equals(HODApps.FIND_SIMILAR)) {
            FindSimilarResponse fsResponse = (FindSimilarResponse) mParser.ParseCustomResponse(FindSimilarResponse.class, response);
            if (fsResponse != null)
                displayReferenceReading(fsResponse);
            else {
                mMediaContent.loadData("<html><body><div>Error!</div></body></html>", "text/html; charset=utf-8",null);
            }
        }
    }
    private String firstCharacterToUpperCase(String word) {
        StringBuilder rackingSystemSb = new StringBuilder(word.toLowerCase());
        rackingSystemSb.setCharAt(0, Character.toUpperCase(rackingSystemSb.charAt(0)));
        word = rackingSystemSb.toString();
        return word;
    }
    private void displayReferenceReading(FindSimilarResponse fsResponse) {
        if (fsResponse.documents.size() > 0) {
            String text = "<html><head/><body><div style=\"font-size:1.0em\">";
            text += "<div style=\"font-size:1.2em;color:gray;margin-bottom:4px;text-align:center\"><b>Reference pages</b></div>";
            for (FindSimilarResponse.Document document : fsResponse.documents) {
                text += String.format("<div><b>Title: </b>%s </div>", document.title);
                text += String.format("<div><b>Relevance: </b>%.2f%s</div>", document.weight, "%");
                if (document.summary != null) {
                    text += String.format("<div><b>Summary: </b>&s</div>", document.summary);
                }
                if (document.reference != null) {
                    text += String.format("<div><b>Content: </b><a href=\"%s\">website</a></div>", document.reference);
                }
                text += "</br>";
            }
            text += "</div></body></html>";
            mMediaContent.loadData(text, "text/html; charset=utf-8", null);
        }
    }
    private void displayOpinions() {
        String text = "<html><head/><body><div style=\"font-size:1.0em\">";
        String posStatementStartTag = "<span style='color:green'><b>";
        String posStatementEndTag = "</b></span>";
        String posSentimentStartTag = "<span style='text-decoration:underline'>";
        String posSentimentEndTag = "</span>";

        String negStatementStartTag = "<span style='color:red'><b>";
        String negStatementEndTag = "</b></span>";
        String negSentimentStartTag = "<span style='text-decoration:underline'>";
        String negSentimentEndTag = "</span>";

        String body = mMediaData.content;
        if (mSaResponse.positive.size() > 0) {
            for (SentimentAnalysisResponse.Entity ent : mSaResponse.positive) {
                String replace = posStatementStartTag + ent.original_text + posStatementEndTag;
                body = body.replace(ent.original_text, replace);
                if (ent.sentiment != null) {
                    replace = posSentimentStartTag + ent.sentiment + posSentimentEndTag;
                    body = body.replace(ent.sentiment, replace);
                }
            }
        }
        if (mSaResponse.negative.size() > 0) {
            for (SentimentAnalysisResponse.Entity ent : mSaResponse.negative) {
                String replace = negStatementStartTag + ent.original_text + negStatementEndTag;
                body = body.replace(ent.original_text, replace);
                if (ent.sentiment != null) {
                    replace = negSentimentStartTag + ent.sentiment + negSentimentEndTag;
                    body = body.replace(ent.sentiment, replace);
                }
            }
        }
        text += body + "</div></body></html>";
        mMediaContent.loadData(text, "text/html; charset=utf-8",null);
    }
    private void displayInterests() {
        String text = "<html><head/><body><div>";
        if (mEeResponse.entities.size() > 0) {
            for (EntityExtractionResponse.Entity entity : mEeResponse.entities) {
                if (entity.type.equals("companies_eng")) {
                    text += "<b>Company name:</b> " + entity.normalized_text + "</br>";
                    if (entity.additional_information != null) {
                        String url = "";
                        if (entity.additional_information.wikipedia_eng != null) {
                            text += "<b>Wiki page: </b><a href=\"";
                            if (!entity.additional_information.wikipedia_eng.contains("http")) {
                                url = "http://" + entity.additional_information.wikipedia_eng;
                            } else {
                                url = entity.additional_information.wikipedia_eng;
                            }
                            text += String.format("%s\">%s</a></br>", url, url);
                        }
                        if (entity.additional_information.url_homepage != null) {
                            text += "<b>Home page: </b><a href=\"";
                            if (!entity.additional_information.url_homepage.contains("http")) {
                                url = "http://" + entity.additional_information.url_homepage;
                            } else {
                                url = entity.additional_information.url_homepage;
                            }
                            text += String.format("%s\">%s</a></br>", url, url);
                        }
                        if (entity.additional_information.company_wikipedia != null) {
                            String wikiPage = "";
                            for (String p : entity.additional_information.company_wikipedia) {
                                wikiPage += p + ", ";
                            }
                            if (wikiPage.length() > 3) {
                                int index = wikiPage.length() - 2;
                                wikiPage = wikiPage.substring(0, index);
                            }
                            text += String.format("<b>Wikipedia:</b> %s</br>", wikiPage);
                        }
                        if (entity.additional_information.company_ric != null) {
                            String wikiPage = "";
                            for (String p : entity.additional_information.company_ric) {
                                wikiPage += p + ", ";
                            }
                            if (wikiPage.length() > 3) {
                                int index = wikiPage.length() - 2;
                                wikiPage = wikiPage.substring(0, index);
                            }
                            text += String.format("<b>RIC:</b>  %s</br>", wikiPage);
                        }
                    }
                } else if (entity.type.equals("places_eng")) {
                    text += "<b>Place name:</b> " + entity.normalized_text + "</br>";

                    if (entity.additional_information != null) {
                        String url = "";
                        if (entity.additional_information.place_population != null) {
                            long pop = entity.additional_information.place_population;
                            String population = String.valueOf(pop);
                            if (pop > 1000000) {
                                Double temp = (double) pop / 1000000;
                                population = String.format("%.02f millions", temp);
                            }
                            text += "<b>Population:</b> " + population + "</br>";
                        }
                        if (entity.additional_information.image != null) {
                            text += String.format("<img src=\"%s\" %s/></br>", entity.additional_information.image, "width=\"90%\"");
                        }
                        if (entity.additional_information.wikipedia_eng != null) {
                            text += "<b>Wiki page: </b><a href=\"";
                            if (!entity.additional_information.wikipedia_eng.contains("http")) {
                                url = "http://";
                            } else {
                                url = entity.additional_information.wikipedia_eng;
                            }
                            text += String.format("%s\">%s</a></br>", url, url);
                        }
                    }
                } else if (entity.type.equals("people_eng")) {
                    text += "<b>People name:</b> " + entity.normalized_text + "</br>";

                    if (entity.additional_information != null) {
                        if (entity.additional_information.person_profession != null) {
                            String prof = "";
                            for (String p : entity.additional_information.person_profession) {
                                prof += p + ", ";
                            }
                            if (prof.length() > 3) {
                                int index = prof.length() - 2;
                                prof = prof.substring(0, index);
                            }
                            text += "<b>Profession:</b> " + prof + "</br>";
                        }
                        if (entity.additional_information.person_date_of_birth != null) {
                            text += "<b>DoB:</b> " + entity.additional_information.person_date_of_birth + "</br>";
                        }
                        if (entity.additional_information.person_date_of_death != null) {
                            text += "<b>DoD:</b> " + entity.additional_information.person_date_of_death + "</br>";
                        }
                        if (entity.additional_information.image != null) {
                            text += String.format("<img src=\"%s\" %s/></br>", entity.additional_information.image, "width=\"90%\"");
                        }
                        if (entity.additional_information.wikipedia_eng != null) {
                            String url = "";
                            text += "<b>Wiki page: </b><a href=\"";
                            if (!entity.additional_information.wikipedia_eng.contains("http")) {
                                url = "http://";
                            } else {
                                url = entity.additional_information.wikipedia_eng;
                            }
                            text += String.format("%s\">%s</a></br>", url, url);
                        }
                    }
                }
                text += "<div style=\"text-align:center\">@@@@@</div>";
            }
            text += "</div></body></html>";
            mMediaContent.loadData(text, "text/html; charset=utf-8", null);
        } else {
            text = "Not found</div></body></html>";
            mMediaContent.loadData(text, "text/html; charset=utf-8", null);
        }
    }

    public void SearchMedia() {
        if (mHodApiKey.length() == 0) {
            mPbloading.setVisibility(View.GONE);
            Toast.makeText(this, "Please provide your apikey to mHodApiKey then recompile the project!", Toast.LENGTH_LONG).show();
            return;
        }
        String searchArg = mMediaSearch.getText().toString();
        if (searchArg.length() == 0)
            searchArg = "*";

        if (m_templates.size() > 0) {
            m_templates.clear();
            m_adapter.notifyDataSetChanged();
        }
        mHodApp = HODApps.QUERY_TEXT_INDEX;

        Map<String, Object> params = new HashMap<String, Object>();
        params.put("text", searchArg);
        params.put("indexes", "speechindex");
        params.put("print_fields", "medianame,filename,mediatype");
        params.put("absolute_max_results", "100");
        int pos = mMediaType.getSelectedItemPosition();
        switch (pos){
            case 0:
                params.put("field_text", "MATCH{video/mp4}:mediatype");
                break;
            case 1:
                params.put("field_text", "MATCH{audio/mpeg,audio/mp4}:mediatype");
                break;
            default:
                break;
        }
        mPbloading.setVisibility(View.VISIBLE);
        mHodClient.PostRequest(params, mHodApp, HODClient.REQ_MODE.SYNC);
    }

    public void GetMediaContent() {
        mHodApp = HODApps.GET_CONTENT;
        Map<String, Object> params = new HashMap<String, Object>();
        params.put("index_reference", mSelectedMedia.reference);
        params.put("indexes", mSelectedMedia.index);
        params.put("print_fields", "offset,text,concepts,occurrences,language");

        mPbloading.setVisibility(View.VISIBLE);
        mHodClient.PostRequest(params, mHodApp, HODClient.REQ_MODE.SYNC);
    }

    public void TranscriptBtnClicked(View v) {
        mReadBlockCount = 1;
        if (mMediaPlayer.isPlaying()) {
            int pos = mMediaPlayer.getCurrentPosition();
            int max = mMediaData.offset.size() - 1;
            for (int i = 0; i < max; i++) {
                int low = mMediaData.offset.get(i);
                int high = mMediaData.offset.get(i+1);
                if (pos > low && pos < high) {
                    resetReadText(i);
                    break;
                }
            }
        } else {
            resetReadText(0);
        }
    }

    public void ConceptsBtnClicked(View v) {
        if (mMediaData == null) {
            return;
        }
        mMediaContent.loadData("","",null);
        StopRunningText();

        String text = "<html><head/><body><div style=\"text-align:center\">";
        String concepts = "";
        // href must contain http://, otherwise the url in the overrideUrlLoading will be broken.
        int max = mMediaData.concepts.size() - 1;
        for (int i = 0; i < max; i++) {
            String concept = mMediaData.concepts.get(i);
            if (concept.length() > 2) {
                int occurrences = mMediaData.occurrences.get(i);
                if (occurrences > 20) {
                    concepts += String.format("<a href=\"http://hod_link:%s\"><span style=\"font-size:1.5em;color:gray\">%s</span></a> - ", concept, concept);
                } else if (occurrences > 15) {
                    concepts += String.format("<a href=\"http://hod_link:%s\"><span style=\"font-size:1.5em;color:orange\">%s</span></a> - ", concept, concept);
                } else if (occurrences > 10) {
                    concepts += String.format("<a href=\"http://hod_link:%s\"><span style=\"font-size:1.5em;color:blue\">%s</span></a> - ", concept, concept);
                } else if (occurrences > 5) {
                    concepts += String.format("<a href=\"http://hod_link:%s\"><span style=\"font-size:1.5em;color:orange\">%s</span></a> - ", concept, concept);
                } else if (occurrences >= 3) {
                    concepts += String.format("<a href=\"http://hod_link:%s\"><span style=\"font-size:1.5em;color:blue\">%s</span></a> - ", concept, concept);
                }
            }
        }
        if (concepts.length() > 3) {
            int index = concepts.length() - 3;
            concepts = concepts.substring(0, index);
            text += concepts;
            text += "</div></body></html>";
            mMediaContent.loadData(text, "text/html; charset=utf-8",null);
        } else {
            text += "Not Found</div></body></html>";
            mMediaContent.loadData(text, "text/html; charset=utf-8", null);
        }
    }

    public void OpinionsBtnClicked(View v) {
        if (mMediaData == null) {
            return;
        }
        mMediaContent.loadData("","",null);
        StopRunningText();

        if (mSaResponse != null) {
            displayOpinions();
        } else {
            if (languageCollection.containsKey(mMediaData.language.get(0))) {
                String lang = "eng";
                lang = languageCollection.get(mMediaData.language.get(0));
                mHodApp = HODApps.ANALYZE_SENTIMENT;
                Map<String, Object> params = new HashMap<String, Object>();
                params.put("text", mMediaData.content);
                params.put("language", lang);
                mPbloading.setVisibility(View.VISIBLE);
                mHodClient.PostRequest(params, mHodApp, HODClient.REQ_MODE.SYNC);
            } else {
                Toast.makeText(this, "Sentiment analysis is not supported for this language",Toast.LENGTH_LONG).show();
            }
        }
    }

    public void InterestsBtnClicked(View v) {
        if (mMediaData == null)
            return;
        mMediaContent.loadData("", "",null);
        StopRunningText();

        if (mEeResponse != null) {
            displayInterests();
        } else {
            mHodApp = HODApps.ENTITY_EXTRACTION;
            Map<String, Object> params = new HashMap<String, Object>();
            params.put("text", mMediaData.content);
            List<String> entities = new ArrayList<String>();
            entities.add("people_eng");
            entities.add("places_eng");
            entities.add("companies_eng");
            params.put("entity_type", entities);
            params.put("unique_entities", "true");
            mPbloading.setVisibility(View.VISIBLE);
            mHodClient.GetRequest(params, mHodApp, HODClient.REQ_MODE.SYNC);
        }
    }
    private void StopRunningText() {
        if (mRunningTextTimer != null)
            mRunningTextTimer.removeCallbacks(runningText);
        mRunningTextTimer = null;
    }
    public class MediaItemTemplates extends ArrayAdapter<ItemObject> {

        private ArrayList<ItemObject> items;

        public MediaItemTemplates(Context context, int textViewResourceId, ArrayList<ItemObject> items) {
            super(context, textViewResourceId, items);
            this.items = items;
        }

        @Override
        public View getView(int position, View convertView, ViewGroup parent) {
            View v = convertView;
            if (v == null) {
                LayoutInflater vi = (LayoutInflater) getSystemService(Context.LAYOUT_INFLATER_SERVICE);
                v = vi.inflate(R.layout.row, parent, false);
            }
            ItemObject o = items.get(position);
            if (o != null) {
                ImageView template_icon = (ImageView) v.findViewById(R.id.p_icon);
                if (template_icon != null) {
                    if (o.mediatype.contains("video"))
                        template_icon.setImageResource(R.drawable.video_icon);
                    else
                        template_icon.setImageResource(R.drawable.audio_icon);
                }
                TextView template_name = (TextView) v.findViewById(R.id.p_title);
                if (template_name != null) {
                    template_name.setText(o.medianame);
                }
            }
            return v;
        }
    }
}