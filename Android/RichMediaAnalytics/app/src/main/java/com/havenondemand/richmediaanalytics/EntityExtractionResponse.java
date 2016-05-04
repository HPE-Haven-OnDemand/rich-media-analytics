package com.havenondemand.richmediaanalytics;

import java.util.List;

/**
 * Created by vanphongvu on 3/3/16.
 */
public class EntityExtractionResponse {
    public List<Entity> entities;

    public class AdditionalInformation {
        public List<String> person_profession;
        public String person_date_of_birth;
        public String person_date_of_death;
        public String image;
        public String wikipedia_eng;
        public String url_homepage;
        public List<String> company_wikipedia;
        public List<String> company_ric;
        public Long place_population;
    }

    public class Entity {
        public String normalized_text;
        public String type;
        public AdditionalInformation additional_information;
    }
}
