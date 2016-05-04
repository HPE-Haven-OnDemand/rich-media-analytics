package com.havenondemand.richmediaanalytics;

import java.util.List;

/**
 * Created by vanphongvu on 3/3/16.
 */
public class FindSimilarResponse {
    public List<Document> documents; // Document

    public class Document {
        public String reference;
        public String summary;
        public String title;
        public Double weight;
    }
}
