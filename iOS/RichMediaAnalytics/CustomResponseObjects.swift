//
//  CustomResponseObjects.swift
//  RichMediaPlayer
//
//  Created by Van Phong Vu on 3/12/16.
//  Copyright © 2016 Van Phong Vu. All rights reserved.
//

import Foundation
import havenondemand

public class EntityExtractionResponse:NSObject {
    var entities:NSMutableArray = [];
    init(json : NSDictionary) {
        super.init()
        for (key, value) in json {
            let keyName:String = (key as? String)!
            if let _ = value as? NSArray {
                let keyValue:NSArray = (value as? NSArray)!
                if keyName == "entities" {
                    for item in keyValue {
                        let p = Entity(json: item as! NSDictionary)
                        self.entities.addObject(p)
                    }
                }
            }
        }
    }
    public class AdditionalInformation:NSObject {
        var person_profession:NSMutableArray = []
        var person_date_of_birth:String = ""
        var wikidata_id:Int = 0
        var wikipedia_eng:String = ""
        var url_homepage:String = ""
        var company_wikipedia:NSMutableArray = []
        var company_ric:NSMutableArray = []
        var image:String = ""
        var person_date_of_death:String = ""
        var lon:Double = 0.0
        var lat:Double = 0.0
        var place_population:Int = 0
        var place_country_code:String = ""
        var place_region1:String = ""
        var place_region2:String = ""
        var place_elevation:Double = 0.0
        init(json:NSDictionary) {
            super.init()
            for (key, value) in json {
                let keyName:String = (key as? String)!
                if let _ = value as? NSArray {
                    let keyValue:NSArray = (value as? NSArray)!
                    for item in keyValue {
                        if (self.respondsToSelector(NSSelectorFromString(keyName))) {
                            let c = item as! String
                            if keyName == "person_profession" {
                                self.person_profession.addObject(c)
                            } else if keyName == "company_wikipedia" {
                                self.company_wikipedia.addObject(c)
                            } else if keyName == "company_ric" {
                                self.company_ric.addObject(c)
                            }
                        }
                    }
                } else if let v = checkValue(value) {
                    if (self.respondsToSelector(NSSelectorFromString(keyName))) {
                        self.setValue(v, forKey: keyName)
                    }
                }
            }
        }
    }
    public class Components:NSObject {
        var original_length: Int64 = 0
        var original_text: String = ""
        var type: String = ""
        init(json:NSDictionary) {
            super.init()
            for (key, value) in json {
                let keyName:String = (key as? String)!
                if let v = checkValue(value) {
                    if (self.respondsToSelector(NSSelectorFromString(keyName))) {
                        self.setValue(v, forKey: keyName)
                    }
                }
            }
        }
    }
    public class Entity:NSObject {
        var normalized_text:String = ""
        var original_text:String = ""
        var type:String = ""
        var normalized_length:Int = 0
        var original_length:Int = 0
        var score:Double = 0.0
        var additional_information:AdditionalInformation?
        var components:NSMutableArray = []
        init(json: NSDictionary) {
            super.init()
            for (key, value) in json {
                let keyName:String = (key as? String)!
                if let _ = value as? NSDictionary {
                    let keyValue:NSDictionary = (value as? NSDictionary)!
                    if (self.respondsToSelector(NSSelectorFromString(keyName))) {
                        self.additional_information = AdditionalInformation(json:keyValue)
                    }
                } else if let _ = value as? NSArray {
                    let keyValue:NSArray = (value as? NSArray)!
                    for item in keyValue {
                        if (self.respondsToSelector(NSSelectorFromString(keyName))) {
                            let c = Components(json:item as! NSDictionary)
                            self.components.addObject(c)
                        }
                    }
                } else if let v = checkValue(value) {
                    if (self.respondsToSelector(NSSelectorFromString(keyName))) {
                        self.setValue(v, forKey: keyName)
                    }
                }
            }
        }
    }
}

public class QueryTextIndexResponse : NSObject{
    var documents:NSMutableArray = [] // Document
    init(json : NSDictionary) {
        super.init()
        for (key, value) in json {
            let keyName:String = (key as? String)!
            if let _ = value as? NSArray {
                let keyValue:NSArray = (value as? NSArray)!
                if keyName == "documents" {
                    for item in keyValue {
                        let p = Document(json: item as! NSDictionary)
                        self.documents.addObject(p)
                    }
                }
            }
        }
    }
    public class Document:NSObject {
        var reference:String = ""
        var index:String = ""
        var medianame:NSMutableArray = []
        var mediatype:NSMutableArray = []
        var filename:NSMutableArray = []
        init(json:NSDictionary) {
            super.init()
            for (key, value) in json {
                let keyName:String = (key as? String)!
                if let _ = value as? NSArray {
                    let keyValue:NSArray = (value as? NSArray)!
                    if keyName == "medianame" {
                        for item in keyValue {
                            let c = item as! String
                            self.medianame.addObject(c)
                        }
                    } else if keyName == "mediatype" {
                        for item in keyValue {
                            let c = item as! String
                            self.mediatype.addObject(c)
                        }
                    } else if keyName == "filename" {
                        for item in keyValue {
                            let c = item as! String
                            self.filename.addObject(c)
                        }
                    }
                }
                else if let v = checkValue(value) {
                    if (self.respondsToSelector(NSSelectorFromString(keyName))) {
                        self.setValue(v, forKey: keyName)
                    }
                }
            }
        }
    }
}

public class GetContentResponse : NSObject{
    var documents:NSMutableArray = [] // Document
    init(json : NSDictionary) {
        super.init()
        for (key, value) in json {
            let keyName:String = (key as? String)!
            if let _ = value as? NSArray {
                let keyValue:NSArray = (value as? NSArray)!
                if keyName == "documents" {
                    for item in keyValue {
                        let p = Document(json: item as! NSDictionary)
                        self.documents.addObject(p)
                    }
                }
            }
        }
    }
    public class Document:NSObject {
        var language = [String]()
        var content:String = ""
        var offset = [Int]()
        var text = [String]()
        var concepts = [String]()
        var occurrences = [Int]()
        
        init(json:NSDictionary) {
            super.init()
            for (key, value) in json {
                let keyName:String = (key as? String)!
                if let _ = value as? NSArray {
                    let keyValue:NSArray = (value as? NSArray)!
                    if keyName == "offset" {
                        for item in keyValue {
                            let c = item as! String
                            if let myNumber = NSNumberFormatter().numberFromString(c) {
                                let myInt = myNumber.integerValue
                                self.offset.append(myInt)
                            }
                        }
                    } else if keyName == "text" {
                        for item in keyValue {
                            let c = item as! String
                            self.text.append(c)
                        }
                    } else if keyName == "language" {
                        for item in keyValue {
                            let c = item as! String
                            self.language.append(c)
                        }
                    } else if keyName == "concepts" {
                        for item in keyValue {
                            let c = item as! String
                            self.concepts.append(c)
                        }
                    } else if keyName == "occurrences" {
                        for item in keyValue {
                            let c = item as! String
                            if let myNumber = NSNumberFormatter().numberFromString(c) {
                                let myInt = myNumber.integerValue
                                self.occurrences.append(myInt)
                            } else {
                                self.occurrences.append(0)
                            }
                        }
                    }
                }
                else if let v = checkValue(value) {
                    if (self.respondsToSelector(NSSelectorFromString(keyName))) {
                        self.setValue(v, forKey: keyName)
                    }
                }
            }
        }
    }
}

public class FindSimilarResponse : NSObject{
    public var documents:NSMutableArray = [] // Document
    init(json : NSDictionary) {
        super.init()
        for (key, value) in json {
            let keyName:String = (key as? String)!
            if let _ = value as? NSArray {
                let keyValue:NSArray = (value as? NSArray)!
                if keyName == "documents" {
                    for item in keyValue {
                        let p = Document(json: item as! NSDictionary)
                        self.documents.addObject(p)
                    }
                }
            }
        }
    }
    public class Document:NSObject {
        public var reference:String = ""
        public var summary:String = ""
        public var title:String = ""
        public var weight:Double = 0.0
        
        init(json:NSDictionary) {
            super.init()
            for (key, value) in json {
                let keyName:String = (key as? String)!
                if let v = checkValue(value) {
                    if (self.respondsToSelector(NSSelectorFromString(keyName))) {
                        self.setValue(v, forKey: keyName)
                    }
                }
            }
        }
    }
}
