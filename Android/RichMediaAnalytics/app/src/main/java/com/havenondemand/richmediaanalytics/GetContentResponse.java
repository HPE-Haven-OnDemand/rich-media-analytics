package com.havenondemand.richmediaanalytics;

import java.util.List;

/**
 * Created by vanphongvu on 3/3/16.
 */
public class GetContentResponse {
    public List<Document> documents;

    public class Document {
        public List<String> language;
        public String content;
        public List<Integer> offset;
        public List<String> text;
        public List<String> concepts;
        public List<Integer> occurrences;
    }
}
