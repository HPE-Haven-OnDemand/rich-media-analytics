//
//  ViewController.swift
//  RichMediaPlayer
//
//  Created by Van Phong Vu on 2/16/16.
//  Copyright © 2016 Van Phong Vu. All rights reserved.
//

import UIKit
import AVKit
import AVFoundation
import havenondemand

class MediaItem:NSObject
{
    var reference:String?
    var index:String?
    var medianame:String?
    var mediatype:String?
    var filename:String?
}

class ViewController: UIViewController, HODClientDelegate, UICollectionViewDelegateFlowLayout, UICollectionViewDataSource, UITextFieldDelegate, UIWebViewDelegate, UIPickerViewDelegate, UIPickerViewDataSource {
    
    let mMaxViewWidth: CGFloat = UIScreen.mainScreen().bounds.size.width;
    let mMaxViewHeight: CGFloat = UIScreen.mainScreen().bounds.size.height
    
    var languageCollection:Dictionary<String,String>?
    
    @IBOutlet weak var msgItem: UILabel!
    @IBOutlet weak var msgDlg: UIView!
    @IBOutlet weak var searchContainer: UIView!
    
    @IBOutlet weak var functionsContainer: UIView!
    @IBOutlet weak var mediaTypeSelContainer: UIView!
    @IBOutlet weak var selectMediaTypeBtn: UIButton!
    @IBOutlet weak var mediaTypePicker: UIPickerView!
    @IBOutlet weak var loadingIndicator: UIActivityIndicatorView!
    @IBOutlet weak var instantSearchFT: UITextField!
    @IBOutlet weak var mediaSearchFT: UITextField!
    
    @IBOutlet weak var mediaContent: UIWebView!
    @IBOutlet weak var mediaCollectionView: UICollectionView!
    
    @IBOutlet weak var mediaListView: UIView!
    @IBOutlet weak var mediaPlayerView: UIView!
    
    @IBOutlet weak var interestsBtn: UIButton!
    @IBOutlet weak var opinionsBtn: UIButton!
    @IBOutlet weak var conceptsBtn: UIButton!
    @IBOutlet weak var transcriptBtn: UIButton!
    
    @IBOutlet weak var nextwordBtn: UIButton!
    @IBOutlet weak var wordcountLabel: UILabel!
    
    var mSaResponse : SentimentAnalysisResponse!
    var mEeResponse : EntityExtractionResponse!
    
    var mUpdateTextTimer: NSTimer?
    
    var mPlayerLayer:AVPlayerLayer?
    
    var mHodClient:HODClient!
    var mParser:HODResponseParser!
    var mMoviePlayer: AVPlayer?
    var mHodApp = ""
    var mMediaList: Array<MediaItem>!
    var mDomain:String = "http://www.hodshowcase.com/media/"

    var mSelectedMediaItem : MediaItem!
    
    var mMediaData: GetContentResponse.Document!
    var mIndex = 0
    var mReadText = ""
    var mUnreadText = ""
    
    let htmlStart = "<html><body><div>"
    let htmlEnd = "</div></body></html>"
    let readTextStart = "<span style=\"color:green\">"
    let wordTextStart = "<span style=\"color:red\">"
    let unreadTextStart = "<span style=\"color:gray\">"
    let spanEnd = "</span>"
    
    enum ViewMode { case MEDIALIST, MEDIAPLAY }
    var viewID = ViewMode.MEDIALIST
    
    var mMaxReadingBlock = 100
    var mReadBlockCount = 1
    var mSearchWordCount:Array<Int> = Array<Int>();
    var mCurrentInstantSearchIndex = -1
    
    
    var btnPressedColor:UIImage?
    var btnNormalColor:UIImage?
    
    var mMediaTypeData: NSArray = ["Video", "Audio", "Vid/Aud"]
    var mSelectedMediaType:Int = 0

    let mHodApiKey = "" // place your apikey here
    
    override func viewDidLoad() {
        super.viewDidLoad()
        mHodClient = HODClient(apiKey: mHodApiKey)
        mHodClient.delegate = self
        mParser = HODResponseParser()
        mediaCollectionView.dataSource = self
        mediaCollectionView.delegate   = self
        instantSearchFT.delegate = self
        mediaSearchFT.delegate = self
        mediaContent.delegate = self
        mediaTypePicker.dataSource = self
        mediaTypePicker.delegate = self
        
        
        languageCollection = Dictionary<String,String>()
        languageCollection!["en-US"] = "eng"
        languageCollection!["en-GB"] = "eng"
        languageCollection!["es-ES"] = "spa"
        languageCollection!["de-DE"] = "ger"
        languageCollection!["fr-FR"] = "fre"
        languageCollection!["it-IT"] = "ita"
        languageCollection!["zh-CN"] = "chi"
        languageCollection!["ru-RU"] = "rus"
        languageCollection!["pt-BR"] = "por"
        
        var color = UIColorFromRGB(0x01a982)
        UIGraphicsBeginImageContext(CGSize(width: 1, height: 1))
        CGContextSetFillColorWithColor(UIGraphicsGetCurrentContext(), color.CGColor)
        CGContextFillRect(UIGraphicsGetCurrentContext(), CGRect(x: 0, y: 0, width: 1, height: 1))
        btnNormalColor = UIGraphicsGetImageFromCurrentImageContext()
        UIGraphicsEndImageContext()
        
        selectMediaTypeBtn.layer.cornerRadius = 4;
        selectMediaTypeBtn.layer.masksToBounds = true;
        selectMediaTypeBtn.layer.borderWidth = 1
        selectMediaTypeBtn.layer.borderColor = color.CGColor
        
        color = UIColorFromRGB(0x7e9b97)
        UIGraphicsBeginImageContext(CGSize(width: 1, height: 1))
        CGContextSetFillColorWithColor(UIGraphicsGetCurrentContext(), color.CGColor)
        CGContextFillRect(UIGraphicsGetCurrentContext(), CGRect(x: 0, y: 0, width: 1, height: 1))
        btnPressedColor = UIGraphicsGetImageFromCurrentImageContext()
        UIGraphicsEndImageContext()

        mediaTypeSelContainer.layer.cornerRadius = 8;
        mediaTypeSelContainer.layer.masksToBounds = true;
        mediaTypeSelContainer.layer.borderWidth = 4
        mediaTypeSelContainer.layer.borderColor = color.CGColor
        
        setButtonsState()
        
        if (UIDevice.currentDevice().userInterfaceIdiom == .Phone)
        {
            if mMaxViewHeight == 568 {
                mMaxReadingBlock = 90
            } else if mMaxViewHeight == 667 {
                mMaxReadingBlock = 180
            } else if mMaxViewHeight == 480 {
                mMaxReadingBlock = 70
            } else {
                mMaxReadingBlock = 110
            }
        } //if iPad
        else if (UIDevice.currentDevice().userInterfaceIdiom == .Pad)
        {
            mMaxReadingBlock = 600
        }
        let constraints = mediaContent.constraints
        mediaContent.removeConstraints(constraints)
        
        mediaContent.frame.origin = CGPointMake(-10, 254)
        let height = mMaxViewHeight - 260
        mediaContent.frame.size = CGSizeMake(mMaxViewWidth, height)
        
        mediaContent.translatesAutoresizingMaskIntoConstraints = true
    }
    func UIColorFromRGB(rgbValue: UInt) -> UIColor {
        return UIColor(
            red: CGFloat((rgbValue & 0xFF0000) >> 16) / 255.0,
            green: CGFloat((rgbValue & 0x00FF00) >> 8) / 255.0,
            blue: CGFloat(rgbValue & 0x0000FF) / 255.0,
            alpha: CGFloat(1.0)
        )
    }
    
    override func shouldAutorotate() -> Bool {
        return false
    }
    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }
    override func viewDidAppear(animated: Bool) {
        super.viewDidDisappear(true)

        if (mMediaList == nil) {
            mMediaList = Array<MediaItem>()
        }

        loadingIndicator.hidesWhenStopped = true
        SearchMedia("*")
    }
    override func viewDidDisappear(animated: Bool) {
        super.viewDidDisappear(true)
    }
    func numberOfComponentsInPickerView(pickerView: UIPickerView) -> Int {
        return 1;
    }
    
    func pickerView(pickerView: UIPickerView, numberOfRowsInComponent component: NSInteger) -> Int {
        return mMediaTypeData.count;
    }
    
    func pickerView(pickerView: UIPickerView, titleForRow row: NSInteger, forComponent component: NSInteger) -> String? {
        return mMediaTypeData[row] as? String;
    }
    func pickerView(pickerView: UIPickerView, didSelectRow row: NSInteger, inComponent component: NSInteger)
    {
        mSelectedMediaType = row
        selectMediaTypeBtn.setTitle(mMediaTypeData[mSelectedMediaType] as? String, forState: UIControlState.Normal)
        
        mediaTypeSelContainer.hidden = true
        mediaCollectionView.alpha = 1.0
        SearchMedia("")
    }
    @IBAction func selectMediaTypeBtnClicked(sender: UIButton) {
        mediaCollectionView.alpha = 0.5
        mediaTypeSelContainer.hidden = false
    }
    @IBAction func cancelMediaTypeSelectionBtnClicked(sender: UIButton) {
        mediaTypeSelContainer.hidden = true
        mediaCollectionView.alpha = 1.0
    }
    
    func SearchMedia(var searcgArg:String)
    {
        if mHodApiKey == "" {
            displayMessageDialog("Provide your apikey to mHodApiKey then recompile the project!")
            return
        }
        if searcgArg.characters.count == 0 {
            searcgArg = "*"
        }
        if mMediaList.count > 0 {
            mMediaList.removeAll(keepCapacity: false)
            mediaCollectionView.reloadData()
        }
        var params = Dictionary<String, AnyObject>()
        mHodApp = HODApps.QUERY_TEXT_INDEX
        params["indexes"] = "speechindex"
        params["text"] = searcgArg
        params["print_fields"] = "medianame,mediatype,filename"
        params["absolute_max_results"] = "100"
        switch (mSelectedMediaType) {
            case 0:
                params["field_text"] = "MATCH{video/mp4}:mediatype"
                break
            case 1:
                params["field_text"] = "MATCH{audio/mpeg,audio/mp4}:mediatype"
                break
            default:
                break
        }
        loadingIndicator.hidden = false
        loadingIndicator.startAnimating()
        
        mHodClient.GetRequest(&params, hodApp: mHodApp, requestMode: HODClient.REQ_MODE.SYNC)
    }

    func numberOfSectionsInCollectionView(collectionView: UICollectionView) -> Int {
        var returnVal = 0
        if (collectionView == self.mediaCollectionView) {
            if (mMediaList != nil) {
                returnVal = 1
            }
        }
        return returnVal
    }
    
    func collectionView(collectionView: UICollectionView, numberOfItemsInSection section: Int) -> Int {
        var returnVal = 0
        if (collectionView == self.mediaCollectionView) {
            if (mMediaList != nil) {
                returnVal = mMediaList.count
            }
        }
        return returnVal
    }
    func collectionView(collectionView: UICollectionView, didDeselectItemAtIndexPath indexPath: NSIndexPath) {
        if (collectionView == self.mediaCollectionView) {
            
        }
    }
    
    func collectionView(collectionView: UICollectionView, didSelectItemAtIndexPath indexPath: NSIndexPath) {
        if (collectionView == self.mediaCollectionView) {
            // media item selected
            StopVideoPlayer()
            mSelectedMediaItem = mMediaList![indexPath.row]
            GetMediaMetaContent()
        }
    }
    
    func collectionView(collectionView: UICollectionView, cellForItemAtIndexPath indexPath: NSIndexPath) -> UICollectionViewCell {
        var cell : UICollectionViewCell!
        
        if (collectionView == self.mediaCollectionView) {
            let identifier : NSString = "cell0Thumbnail";
            cell = collectionView.dequeueReusableCellWithReuseIdentifier(identifier as String, forIndexPath: indexPath) as UICollectionViewCell
            
            if (mMediaList != nil) {
                let imageView:UIImageView = cell.viewWithTag(4) as! UIImageView
                let mType = mMediaList[indexPath.row].mediatype!
                if mType.containsString("video") == true {
                    imageView.image = UIImage(named: "VideoIcon")
                } else {
                    imageView.image = UIImage(named: "AudioIcon")
                }
                
                imageView.userInteractionEnabled = true
                let mediaName = mMediaList![indexPath.row].medianame
                if (mediaName != nil) {
                    let mediaTitle : UILabel = cell.viewWithTag(1) as! UILabel
                    mediaTitle.text = mediaName
                    
                }
            }
        }
        return cell
    }
    
    func onErrorOccurred(errorMessage: String) {
        hideMessageDialog()
        loadingIndicator.stopAnimating()
    }
    func requestCompletedWithContent(var response: String) {
        hideMessageDialog()
        loadingIndicator.stopAnimating()
        if mHodApp == HODApps.QUERY_TEXT_INDEX {
            if let dic = mParser.ParseCustomResponse(&response) {
                let obj = QueryTextIndexResponse(json:dic)
                mMediaList.removeAll()
                for doc in obj.documents as NSArray as! [QueryTextIndexResponse.Document] {
                    let item:MediaItem = MediaItem()
                    item.reference = doc.reference as String
                    item.index = doc.index as String
                    item.medianame =  doc.medianame[0] as? String
                    item.mediatype = doc.mediatype[0] as? String
                    if doc.filename.count > 0 {
                        item.filename = doc.filename[0] as? String
                    }
                    mMediaList.append(item)
                }
                dispatch_async(dispatch_get_main_queue(), {
                    self.mediaCollectionView.reloadData()
                })
            } else {
            
            }
        } else if mHodApp == HODApps.GET_CONTENT {
            if let dic = mParser.ParseCustomResponse(&response) {
                let obj = GetContentResponse(json:dic)

                for doc in obj.documents as NSArray as! [GetContentResponse.Document] {
                    mMediaData = doc
                    break
                }
                // work around punctuation
                let count = mMediaData.text.count;
                let start = mMediaData.text.count > 10 ? 10 : 0;
                let end = mMediaData.text.count > 200 ? 150 : mMediaData.text.count - 1;
                var total = 0
                var timestamp = mMediaData.offset
                for i in start...end {
                    total += timestamp[i] - timestamp[i - 1]
                }
                let average:Double = Double(total / end - 1)
                let para:Double = average + 1500
                let dot:Double = average + 550
                let commas = average + 400

                mMediaData.text[0] = firstCharacterUppercaseString(mMediaData.text[0])
                let max = count-1
                var len = 0
                for i in 1...max
                {
                    if (mMediaData.text[i] != "<Music/Noise>" && mMediaData.text[i-1] != "<Music/Noise>")
                    {
                        let diff = Double(timestamp[i] - timestamp[i - 1])
                        if (diff > para && len > 1)
                        {
                            mMediaData.text[i - 1] += ".";
                            mMediaData.text[i] = firstCharacterUppercaseString(mMediaData.text[i]);
                            len = 0
                        }
                        else if (diff > dot && len > 1)
                        {
                            mMediaData.text[i - 1] += ".";
                            mMediaData.text[i] = firstCharacterUppercaseString(mMediaData.text[i]);
                            len = 0
                        }
                        else if (diff > commas && len > 1)
                        {
                            mMediaData.text[i - 1] += ",";
                            len = 0
                        } else {
                            len++
                        }
                    }
                }
                mMediaData.content = mMediaData.text.joinWithSeparator(" ")
                mUnreadText = mMediaData.content
                var htmlDoc = htmlStart
                htmlDoc += unreadTextStart + mUnreadText + spanEnd
                htmlDoc += htmlEnd
                
                mediaContent.loadHTMLString(htmlDoc, baseURL: nil)

                dispatch_async(dispatch_get_main_queue(), {
                    self.PlaybackMedia()
                })
                
            } else {
                let text = "<html><head/><body><div>Error!</div></body></html>";
                mediaContent.loadHTMLString(text, baseURL: nil)
            }
        } else if (mHodApp == HODApps.ANALYZE_SENTIMENT) {
            mSaResponse = mParser.ParseSentimentAnalysisResponse(&response)
            if mSaResponse != nil {
                displayOpinions()
            }  else {
                let text = "<html><head/><body><div>Error!</div></body></html>";
                mediaContent.loadHTMLString(text, baseURL: nil)
            }
        } else if (mHodApp == HODApps.ENTITY_EXTRACTION) {
            if let dic = mParser.ParseCustomResponse(&response) {
                mEeResponse = EntityExtractionResponse(json:dic)
                if mEeResponse != nil {
                    displayInterests()
                } else {
                    let text = "<html><head/><body><div>Error!</div></body></html>";
                    mediaContent.loadHTMLString(text, baseURL: nil)
                }
            }
        } else if mHodApp == HODApps.FIND_SIMILAR {
            if let dic = mParser.ParseCustomResponse(&response) {
                let obj = FindSimilarResponse(json:dic)

                if obj.documents.count > 0 {
                    var text:String = "<html><head/><body><div style=\"font-size:1.0em\">";
                    /*
                    text += "<div style=\"text-align:right\"><a href=\"hod_home\">Back to Concepts</a></div>";
                    */
                    for document in obj.documents {
                        let doc = document as! FindSimilarResponse.Document
                        text += String(format: "<div><b>Title: </b>%@ </div>", arguments: [doc.title])
                        text += String(format: "<div><b>Relevance: </b>%.2f%@</div>", arguments: [doc.weight,"%"])
                        if (doc.summary.characters.count > 0) {
                            text += String(format: "<div><b>Summary: </b>&@</div>", arguments: [doc.summary]);
                        }
                        if doc.reference.characters.count > 0 {
                            text += String(format: "<div><b>Content: </b><a href=\"hod_ref:%@\">website</a></div>", arguments: [doc.reference])
                        }
                    text += "</br>";
                }
                text += "</div></body></html>";
                mediaContent.loadHTMLString(text, baseURL: nil)
                }
            }
        }
    }
    func firstCharacterUppercaseString(string: String) -> String {
        let str = string as NSString
        let firstUppercaseCharacter = str.substringToIndex(1).uppercaseString
        let firstUppercaseCharacterString = str.stringByReplacingCharactersInRange(NSMakeRange(0, 1), withString: firstUppercaseCharacter)
        return firstUppercaseCharacterString
    }
    func updateTextFunction() {
        let count = mMediaData.offset.count
        if mIndex < count {
            let cmtime = mMoviePlayer!.currentTime()
            var pos = Double(CMTimeGetSeconds(cmtime))
            pos *= 1000
            let check:Double = Double(mMediaData.offset[mIndex])
            if pos > check {
                let word = mMediaData.text[mIndex]
                mIndex++
                if (mReadBlockCount > mMaxReadingBlock) {
                    mReadText = "";
                    mReadBlockCount = 1
                }
                mUnreadText = " "
                var max = mMaxReadingBlock - mReadBlockCount
                if (max + mIndex > count) {
                    let sub = mMediaData.text[mIndex..<count]
                    mUnreadText += sub.joinWithSeparator(" ")
                    mUnreadText += ".";
                } else {
                    max += mIndex
                    let sub = mMediaData.text[mIndex..<max]
                    mUnreadText += sub.joinWithSeparator(" ")
                    mUnreadText += "...";
                }
                var htmlDoc = htmlStart + readTextStart + mReadText + spanEnd
                htmlDoc += wordTextStart + word + spanEnd
                htmlDoc += unreadTextStart + mUnreadText + spanEnd
                htmlDoc += htmlEnd
                
                mediaContent.loadHTMLString(htmlDoc, baseURL: nil)
                mReadText += word + " "
                mReadBlockCount++
            }
        }
    }
    
    func displayInterests() {
        var text = "<html><head/><body><div>";
        if mEeResponse.entities.count > 0 {
            for entity in mEeResponse.entities as NSArray as! [EntityExtractionResponse.Entity] {
                if (entity.type == "companies_eng") {
                    text += "<b>Company name:</b> " + entity.normalized_text + "</br>";
                        
                    if (entity.additional_information != nil) {
                        var url = "";
                        if (entity.additional_information!.wikipedia_eng != "") {
                            text += "<b>Wiki page: </b><a href=\"";
                            if (!entity.additional_information!.wikipedia_eng.containsString("http")) {
                                url = "http://" + entity.additional_information!.wikipedia_eng;
                            } else {
                                url = entity.additional_information!.wikipedia_eng;
                            }
                            text += url + "\">";
                            text += url + "</a>";
                            text += "</br>";
                        }
                        if (entity.additional_information!.url_homepage != "") {
                            text += "<b>Home page: </b><a href=\"";
                            if (!entity.additional_information!.url_homepage.containsString("http")) {
                                url = "http://" + entity.additional_information!.url_homepage;
                            } else {
                                url = entity.additional_information!.url_homepage;
                            }
                            text += url + "\">";
                            text += url + "</a>";
                            text += "</br>";
                        }
                        if (entity.additional_information!.company_wikipedia.count > 0) {
                            var wikiPage = "";
                            for p in entity.additional_information!.company_wikipedia {
                                wikiPage += (p as! String) + ", ";
                            }
                            if (wikiPage.characters.count > 3) {
                                let index = wikiPage.characters.count - 2
                                wikiPage = (wikiPage as NSString).substringToIndex(index)
                            }
                            text += "<b>Wikipedia:</b> " + wikiPage + "</br>";
                        }
                        if (entity.additional_information!.company_ric.count > 0) {
                            var wikiPage = "";
                            for p in entity.additional_information!.company_ric {
                                wikiPage += (p as! String) + ", ";
                            }
                            if wikiPage.characters.count > 3 {
                                let index = wikiPage.characters.count - 2
                                wikiPage = (wikiPage as NSString).substringToIndex(index)
                            }
                            text += "<b>RIC:</b> " + wikiPage + "</br>";
                        }
                    }
                } else if (entity.type == "places_eng") {
                    text += "<b>Place name:</b> " + entity.normalized_text + "</br>";
                    
                    if (entity.additional_information != nil) {
                        var url = "";
                        if (entity.additional_information!.place_population != 0) {
                            var pop = entity.additional_information!.place_population;
                            var population = String(pop);
                            if (pop > 1000000){
                                pop /= 1000000;
                                population = String(pop) + " million";
                            }
                            text += "<b>Population:</b> " + population + "</br>";
                        }
                        if (entity.additional_information!.image != "") {
                            text += "<img src=\"";
                            text += entity.additional_information!.image + "\" width=\"90%\"/>";
                            text += "</br>";
                        }
                        if (entity.additional_information!.wikipedia_eng != "") {
                            text += "<b>Wiki page: </b><a href=\"";
                            if (!entity.additional_information!.wikipedia_eng.containsString("http")) {
                                url = "http://";
                            } else {
                                url = entity.additional_information!.wikipedia_eng;
                            }
                            text += url + "\">";
                            text += url + "</a>";
                            text += "</br>";
                        }
                    }
                } else if (entity.type == "people_eng") {
                    text += "<b>People name:</b> " + entity.normalized_text + "</br>";
                    
                    if (entity.additional_information != nil) {
                        if (entity.additional_information!.person_profession.count > 0) {
                            var prof = "";
                            for p in entity.additional_information!.person_profession {
                                prof += (p as! String) + ", ";
                            }
                            if prof.characters.count > 3 {
                                let index = prof.characters.count - 2
                                prof = (prof as NSString).substringToIndex(index)
                            }
                            text += "<b>Profession:</b> " + prof + "</br>";
                        }
                        if (entity.additional_information!.person_date_of_birth != "") {
                            text += "<b>DoB:</b> " + entity.additional_information!.person_date_of_birth + "</br>";
                        }
                        if (entity.additional_information!.person_date_of_death != "") {
                            text += "<b>DoD:</b> " + entity.additional_information!.person_date_of_death + "</br>";
                        }
                        if (entity.additional_information!.image != "") {
                            text += "<img src=\"";
                            text += entity.additional_information!.image + "\" width=\"90%\"/>";
                            text += "</br>";
                        }
                        if (entity.additional_information!.wikipedia_eng != "") {
                            var url = "";
                            text += "<b>Wiki page: </b><a href=\"";
                            if (!entity.additional_information!.wikipedia_eng.containsString("http")) {
                                url = "http://";
                            } else {
                                url = entity.additional_information!.wikipedia_eng;
                            }
                            text += url + "\">";
                            text += url + "</a>";
                            text += "</br>";
                        }
                    }
                }
                text += "<div style=\"text-align:center\">@@@@@</div>";
            }
            text += "</div></body></html>";
            mediaContent.loadHTMLString(text, baseURL: nil)
        } else {
            text = "Not found</div></body></html>";
            mediaContent.loadHTMLString(text, baseURL: nil)
        }
    }
    
    func displayOpinions() {
        var text = "<html><head/><body><div style=\"font-size:1.0em\">";
        let posStatementStartTag = "<span style='color:green'><b>";
        let posStatementEndTag = "</b></span>";
        let posSentimentStartTag = "<span style='text-decoration:underline'>";
        let posSentimentEndTag = "</span>";
        
        let negStatementStartTag = "<span style='color:red'><b>";
        let negStatementEndTag = "</b></span>";
        let negSentimentStartTag = "<span style='text-decoration:underline'>";
        let negSentimentEndTag = "</span>";
            
        var body = mMediaData.content;
        if (mSaResponse.positive.count > 0) {
            for item in mSaResponse.positive {
                let pos = item as! SentimentAnalysisResponse.Entity
                var replace = posStatementStartTag + pos.original_text + posStatementEndTag
                    
                body = (body as NSString).stringByReplacingOccurrencesOfString(pos.original_text, withString: replace)
                if (pos.sentiment != "") {
                    replace = posSentimentStartTag + pos.sentiment + posSentimentEndTag
                    body = (body as NSString).stringByReplacingOccurrencesOfString(pos.sentiment, withString: replace)
                }
            }
        }
        if (mSaResponse.negative.count > 0) {
            for item in mSaResponse.negative {
                let neg = item as! SentimentAnalysisResponse.Entity
                var replace = negStatementStartTag + neg.original_text + negStatementEndTag
                body = (body as NSString).stringByReplacingOccurrencesOfString(neg.original_text, withString: replace)
                    
                if (neg.sentiment != "") {
                    replace = negSentimentStartTag + neg.sentiment + negSentimentEndTag
                    body = (body as NSString).stringByReplacingOccurrencesOfString(neg.sentiment, withString: replace)
                }
            }
        }
        text += body + "</div></body></html>";
        mediaContent.loadHTMLString(text, baseURL: nil)
    }
    
    func StopRunningText() {
        mUpdateTextTimer?.invalidate()
        mReadText = ""
        mUnreadText = ""
        mIndex = 0
        mReadBlockCount = 0
        let htmlDoc = htmlStart + htmlEnd
        mediaContent.loadHTMLString(htmlDoc, baseURL: nil)
        NSNotificationCenter.defaultCenter().removeObserver(self)
    }
    func requestCompletedWithJobID(response: String) {
        let jobID:String? = mParser.ParseJobID(response)
        if jobID != nil {
            mHodClient.GetJobStatus(jobID!)
        }
    }
    func GetMediaMetaContent() {
        mSaResponse = nil
        mEeResponse = nil
        wordcountLabel.text = "0/0"
        instantSearchFT.text = ""
        
        mHodApp = HODApps.GET_CONTENT
        var params = Dictionary<String, AnyObject>()
        params["index_reference"] = mSelectedMediaItem.reference
        params["indexes"] = mSelectedMediaItem.index
        params["print_fields"] = "offset,text,concepts,occurrences,language"
        ShowMediaPlayView(true)
        loadingIndicator.hidden = false
        loadingIndicator.startAnimating()
        displayMessageDialog("Loading transcript. Please wait...")
        mHodClient.GetRequest(&params, hodApp: mHodApp, requestMode: HODClient.REQ_MODE.SYNC)
    }
    func ShowMediaPlayView(show:Bool) {
        if show {
            mediaListView.hidden = true
            viewID = ViewMode.MEDIAPLAY
            mediaPlayerView.alpha = 0.0
            mediaPlayerView.hidden = false
            setFunctionButton(transcriptBtn)
            UIView.animateWithDuration(1.0, animations: {
                self.mediaPlayerView.alpha = 1.0
                }, completion: { (value: Bool) in
                    self.view.bringSubviewToFront(self.mediaPlayerView)
            })
        } else {
            nextwordBtn.hidden = true
            mediaPlayerView.hidden = true
            functionsContainer.hidden = true
            viewID == ViewMode.MEDIALIST
            mediaListView.alpha = 0.0
            mediaListView.hidden = false
            UIView.animateWithDuration(1.0, animations: {
                self.mediaListView.alpha = 1.0
                }, completion: { (value: Bool) in
                    self.view.bringSubviewToFront(self.mediaListView)
            })
        }
    }
    func StopVideoPlayer() {
        if mMoviePlayer != nil {
            StopRunningText()
            if (mMoviePlayer!.rate != 0 && mMoviePlayer!.error == nil) {
                mMoviePlayer!.pause()
                mMoviePlayer = nil
                
            }
            mPlayerLayer!.removeFromSuperlayer()
        }
    }
    @IBAction func BackToMediaList(sender: UIButton) {
        StopVideoPlayer()
        ShowMediaPlayView(false)
    }
    
    func PlaybackMedia() {
        let temp = mSelectedMediaItem.filename!.stringByAddingPercentEncodingWithAllowedCharacters(.URLHostAllowedCharacterSet())
        let mediaLink = (mDomain as NSString).stringByAppendingPathComponent(temp!)
        let videoURL = NSURL(string: mediaLink)
        
        let item = AVPlayerItem(URL: videoURL!)
        mMoviePlayer = AVPlayer(playerItem: item)
        
        NSNotificationCenter.defaultCenter().addObserver(self, selector: "playerDidFinishPlaying:", name: AVPlayerItemDidPlayToEndTimeNotification, object: item)
        
        mPlayerLayer = AVPlayerLayer(player: mMoviePlayer)
        
        mPlayerLayer!.frame = CGRect(x: 0, y: 50, width: mMaxViewWidth, height: 200)
        self.view.layer.addSublayer(mPlayerLayer!)
        mMoviePlayer!.play()
        
        self.mIndex = 0
        self.mUpdateTextTimer = NSTimer.scheduledTimerWithTimeInterval(0.2, target: self, selector: Selector("updateTextFunction"), userInfo: nil, repeats: true)

        functionsContainer.alpha = 0.0
        functionsContainer.hidden = false
        UIView.animateWithDuration(1.0, animations: {
            self.functionsContainer.alpha = 1.0
            }, completion: { (value: Bool) in
                self.view.bringSubviewToFront(self.functionsContainer)
        })
    }
    func playerDidFinishPlaying(note: NSNotification) {
        mUpdateTextTimer?.invalidate()
        
        NSNotificationCenter.defaultCenter().removeObserver(self)
        mIndex = 0;
        mReadText = ""
        updateTextFunction()
    }
    func textFieldDidBeginEditing(textField: UITextField) {
        instantSearchFT.text = ""
    }
    func textFieldShouldReturn(textField: UITextField) -> Bool {
        textField.resignFirstResponder()
        if viewID == ViewMode.MEDIALIST {
            SearchMedia(textField.text!)
        } else {
            let searchTerm = textField.text
            searchText(searchTerm!)
        }
        return true
    }
    func searchText(searchTerm:String) {
        nextwordBtn.hidden = true
        mSearchWordCount.removeAll(keepCapacity: false)
        if searchTerm.characters.count > 0 {
            let count = mMediaData.text.count
            for var i=0; i < count; i++ {
                if mMediaData.text[i] == searchTerm {
                    mSearchWordCount.append(i)
                }
            }
            if mSearchWordCount.count > 0 {
                mCurrentInstantSearchIndex = -1
                moveToInstantSearchWord()
            }
            if mSearchWordCount.count > 1 {
                nextwordBtn.hidden = false
            }
        }
    }
    func moveToInstantSearchWord() {
        mCurrentInstantSearchIndex++
        if mCurrentInstantSearchIndex >= mSearchWordCount.count {
            mCurrentInstantSearchIndex = 0
        }
        let pos = mSearchWordCount[mCurrentInstantSearchIndex]
        let offset = mMediaData.offset[pos]
        let seconds = Int64(offset/1000)
        let preferredTimeScale : Int32 = 1
        let goto = CMTimeMake(seconds, preferredTimeScale)
        mMoviePlayer!.seekToTime(goto)
        resetReadText(pos)
        let w = mCurrentInstantSearchIndex + 1
        wordcountLabel.text = String(format: "%d/%d", arguments: [w, mSearchWordCount.count])
    }
    @IBAction func NextWordBtnClicked(sender: UIButton) {
        moveToInstantSearchWord()
    }
    @IBAction func InterestsBtnClicked(sender: UIButton) {
        if mMediaData == nil {
            return
        }
        setFunctionButton(interestsBtn)
        
        StopRunningText()
        if mEeResponse != nil {
            displayInterests()
        } else {
            var params = Dictionary<String, AnyObject>()
        
            params["text"] = mMediaData.content
            params["entity_type"] = ["people_eng","places_eng","companies_eng"]
        
            params["unique_entities"] = "true"
        
            mHodApp = HODApps.ENTITY_EXTRACTION;
            displayMessageDialog("Extracting content. Please wait...")
            mHodClient.PostRequest(&params, hodApp: mHodApp, requestMode: HODClient.REQ_MODE.SYNC);
        }
    }
    @IBAction func OpinionsBtnClicked(sender: UIButton) {
        if mMediaData == nil {
            return
        }
        setFunctionButton(opinionsBtn)

        StopRunningText()
        if mSaResponse != nil {
            displayOpinions()
        } else {
            if let lang = languageCollection![mMediaData.language[0]] {
                var params = Dictionary<String, AnyObject>()
                params["text"] = mMediaData.content
                params["language"] = lang
                mHodApp = HODApps.ANALYZE_SENTIMENT
                
                displayMessageDialog("Analyzing content. Please wait...")
                mHodClient.PostRequest(&params, hodApp: mHodApp, requestMode: HODClient.REQ_MODE.SYNC)
            } else {
                displayMessageDialog("Sentiment analysis is not supported for this language")
            }
        }
    }
    @IBAction func ConceptsBtnClicked(sender: UIButton) {
        if mMediaData == nil {
            return
        }
        setFunctionButton(conceptsBtn)

        StopRunningText()
        var text = "<html><head/><body><div style=\"text-align:center\">";
        var concepts = "";
        
        let max = mMediaData.concepts.count - 1
        for i in 0...max {
            let concept = mMediaData.concepts[i]
            if (concept.characters.count > 2) {
                let occurrences = mMediaData.occurrences[i];
                if occurrences > 20 {
                    concepts += "<a href=\"hod_link:" + concept + "\"><span style=\"font-size:1.5em;color:blue\">" + concept + "</span></a> - "
                } else if occurrences > 15 {
                    concepts += "<a href=\"hod_link:" + concept + "\"><span style=\"font-size:1.5em;color:orange\">" + concept + "</span></a> - "
                } else if occurrences > 10 {
                    concepts += "<a href=\"hod_link:" + concept + "\"><span style=\"font-size:1.5em;color:blue\">" + concept + "</span></a> - "
                } else if occurrences > 5 {
                    concepts += "<a href=\"hod_link:" + concept + "\"><span style=\"font-size:1.5em;color:orange \">" + concept + "</span></a> - "
                } else if occurrences >= 3 {
                    concepts += "<a href=\"hod_link:" + concept + "\"><span style =\"font-size:1.8em;color:blue\">" + concept + "</span></a> - "
                }
            }
        }
        if concepts.characters.count > 3 {
            let index = concepts.characters.count - 3
            concepts = (concepts as NSString).substringToIndex(index)
            text += concepts;
            text += "</div></body></html>";
            mediaContent.loadHTMLString(text, baseURL: nil)
        } else {
            text += "Not Found</div></body></html>";
            mediaContent.loadHTMLString(text, baseURL: nil)
        }
        
    }
    @IBAction func TranscriptBtnClicked(sender: UIButton) {
        if mMediaData == nil {
            return
        }
        setFunctionButton(transcriptBtn)

        if mMoviePlayer!.rate != 0 {
            let cmtime = mMoviePlayer!.currentTime()
            var pos = Double(CMTimeGetSeconds(cmtime))
            pos *= 1000
            let max = mMediaData.offset.count - 1
            for i in 0...max {
                let low = Double(mMediaData.offset[i])
                let high = Double(mMediaData.offset[i+1])
                if (pos > low && pos < high) {
                    resetReadText(i)
                    break
                }
            }
        } else {
            resetReadText(0)
        }
    }
    func setFunctionButton(selectedBtn:UIButton)
    {
        interestsBtn.selected = false
        transcriptBtn.selected = false
        conceptsBtn.selected = false
        opinionsBtn.selected = false
        selectedBtn.selected = true
    }
    func resetReadText(pos:Int) {
        mUpdateTextTimer!.invalidate()
        
        mReadBlockCount = 1
        mIndex = pos
        let sub = mMediaData.text[0..<pos]
        mReadText = sub.joinWithSeparator(" ") + " "
        if mMoviePlayer!.rate != 0 {
            mUpdateTextTimer = NSTimer.scheduledTimerWithTimeInterval(0.2, target: self, selector: Selector("updateTextFunction"), userInfo: nil, repeats: true)
        }
    }
    func webView(webView: UIWebView, shouldStartLoadWithRequest request: NSURLRequest, navigationType: UIWebViewNavigationType) -> Bool {
        let link = (request.URL?.absoluteString)! as NSString
        if link.containsString("hod_link") {
            var concept = link.componentsSeparatedByString("hod_link:")
            CallFindSimilar(concept[1])
            return false
        } else if link.containsString("hod_ref") {
            let site = link.componentsSeparatedByString("hod_ref:")
            if let requestUrl = NSURL(string: site[1]) {
                UIApplication.sharedApplication().openURL(requestUrl)
            }
            return false
        } else {
            return true;
        }
    }
    
    func CallFindSimilar(concept:String) {
        var lang = "en-US";
        if (mMediaData.language.count > 0 && mMediaData.language[0] != ""){
            lang = mMediaData.language[0]
        }
        var params = Dictionary<String, AnyObject>()
        if lang == "en-US" || lang == "en-GB" {
            params["indexes"] = ["wiki_eng","news_eng"]
        } else if lang == "it-IT" {
            params["indexes"] = ["wiki_ita","news_ita"]
        } else if lang == "fr-FR" {
            params["indexes"] = ["wiki_fra","news_fra"]
        } else if lang == "de-DE" {
            params["indexes"] = ["wiki_ger","news_ger"]
        } else if lang == "es-ES" {
            params["indexes"] = ["wiki_spa"]
        } else if lang == "zh-CN" {
            params["indexes"] = ["wiki_chi"]
        } else {
            displayMessageDialog("No public database for this language")
            return
        }
        
        params["text"] = concept
        params["print_fields"] = "title,reference,summary,weight"
        
        mHodApp = HODApps.FIND_SIMILAR
        StopRunningText()
        displayMessageDialog("Analyzing content. Please wait...")
        mHodClient.PostRequest(&params, hodApp: mHodApp, requestMode: HODClient.REQ_MODE.SYNC)
    }
    func setButtonsState() {
        
        transcriptBtn.setBackgroundImage(btnNormalColor, forState: .Normal)
        transcriptBtn.setBackgroundImage(btnPressedColor, forState: .Selected)
        
        conceptsBtn.setBackgroundImage(btnNormalColor, forState: .Normal)
        conceptsBtn.setBackgroundImage(btnPressedColor, forState: .Selected)
        
        opinionsBtn.setBackgroundImage(btnNormalColor, forState: .Normal)
        opinionsBtn.setBackgroundImage(btnPressedColor, forState: .Selected)
        
        interestsBtn.setBackgroundImage(btnNormalColor, forState: .Normal)
        interestsBtn.setBackgroundImage(btnPressedColor, forState: .Selected)
    }
    func displayMessageDialog(message:String) {
        msgDlg.hidden = false
        msgItem.text = message
    }
    func hideMessageDialog() {
        msgDlg.hidden = true
    }
}

