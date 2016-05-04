using System;
using System.Collections.Generic;
using System.Text;

namespace RichMediaAnalytics.DataModel
{
    public class EntityExtractionResponse
    {
        public List<Entity> entities { get; set; }

        public class Entity
        {
            public string normalized_text { get; set; }
            public string original_text { get; set; }
            public string type { get; set; }
            public long normalized_length { get; set; }
            public long original_length { get; set; }
            public double score { get; set; }
            public string normalized_date { get; set; }
            public EntityAdditionalInformation additional_information { get; set; }
            public List<object> components { get; set; }
        }

        public class EntityAdditionalInformation
        {
            public List<string> person_profession { get; set; }
            public string person_date_of_birth { get; set; }
            public long wikidata_id { get; set; }
            public string wikipedia_eng { get; set; }
            public string image { get; set; }
            public string person_date_of_death { get; set; }
            public double lon { get; set; }
            public double lat { get; set; }
            public long place_population { get; set; }
            public string place_country_code { get; set; }
            public string place_region1 { get; set; }
            public string place_region2 { get; set; }
            public string place_type { get; set; }
            public string url_homepage { get; set; }
            public List<string> company_wikipedia { get; set; }
            public List<string> company_ric { get; set; }
            public List<string> disease_icd10 { get; set; }
            public List<string> disease_diseasesdb { get; set; }

        }

    }
    /*
    public class SpeechRecognition
    {
        public List<Document> document { get; set; }
        public class Document
        {
            public string content { set; get; }
            public long offset { set; get; }
        }
    }
    */
    public class ContentIndex // for GeTContent
    {
        public List<Document> document { get; set; }

        public class Document
        {
            public string mediatype { get; set; }
            public string filename { get; set; }
            public string medianame { get; set; }
            public string language { get; set; }
            public string content { set; get; }
            public List<long> offset { set; get; }
            public List<string> text { get; set; }
            public List<string> concepts { get; set; }
            public List<int> occurrences { get; set; }
        }
    }

    public class QueryTextIndexResponse
    {
        public List<Document> documents { get; set; }

        public class Document
        {
            public string reference { get; set; }
            public string index { get; set; }
            public List<string> medianame { get; set; }
            public List<string> filename { get; set; }
            public List<string> mediatype { get; set; }
        }
    }

    public class GetContentResponse
    {
        public List<Document> documents { get; set; }

        public class Document
        {
            public List<double> offset { get; set; }
            public List<string> text { get; set; }
            public List<string> concepts { get; set; }
            public List<int> occurrences { get; set; }
            public string content { get; set; }
            public string summary { get; set; }
            public List<string> language { get; set; }
        }
    }

    public class FindSimilarResponse
    {
        public List<Document> documents { get; set; }

        public class Document
        {
            public string reference { get; set; }
            public string summary { get; set; }
            public string title { get; set; }
            public double weight { get; set; }
        }
    }

    public struct MediaMetadata
    {
        public string contentName { get; set; }
        public string contentType { get; set; }
        public string fileName { get; set; }
        public string mediaLanguage { get; set; }
    }

}
