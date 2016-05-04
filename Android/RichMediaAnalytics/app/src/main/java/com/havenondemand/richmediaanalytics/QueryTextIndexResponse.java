package com.havenondemand.richmediaanalytics;

import java.util.List;

/**
 * Created by vanphongvu on 3/3/16.
 */
public class QueryTextIndexResponse {
    public List<Document> documents;

    public class Document {
        public String reference;
        public String index;
        public List<String> medianame;
        public List<String> mediatype;
        public List<String> filename;
    }
}
