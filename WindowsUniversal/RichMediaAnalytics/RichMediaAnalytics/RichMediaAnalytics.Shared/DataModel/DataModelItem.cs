using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RichMediaAnalytics.DataModels
{
    public class TextViewModel : INotifyPropertyChanged
    {
        private string _readtext;
        public string ReadText
        {
            get
            {
                return _readtext;
            }
            set
            {
                if (value != _readtext)
                {
                    _readtext = value;
                    NotifyPropertyChanged("ReadText");
                }
            }
        }
        private string _word;
        public string Word
        {
            get
            {
                return _word;
            }
            set
            {
                if (value != _word)
                {
                    _word = value;
                    NotifyPropertyChanged("Word");
                }
            }
        }
        private string _unreadtext;
        public string UnreadText
        {
            get
            {
                return _unreadtext;
            }
            set
            {
                if (value != _unreadtext)
                {
                    _unreadtext = value;
                    NotifyPropertyChanged("UnreadText");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class MetaDataModel : INotifyPropertyChanged
    {
        private string _metadata = "";
        public string MetaData
        {
            get
            {
                return _metadata;
            }
            set
            {
                if (value != _metadata)
                {
                    _metadata = value;
                    NotifyPropertyChanged("MetaData");
                }
            }
        }
        private string _negstatement = "";
        public string NegStatement
        {
            get
            {
                return _negstatement;
            }
            set
            {
                if (value != _negstatement)
                {
                    _negstatement = value;
                    NotifyPropertyChanged("NegStatement");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class ContentModel : INotifyPropertyChanged
    {
        public string reference { get; set; }
        public string index { get; set; }
        public string filename { get; set; }
        private string _title = "";
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                if (value != _title)
                {
                    _title = value;
                    NotifyPropertyChanged("Title");
                }
            }
        }
        private string _type = "";
        public string Type
        {
            get
            {
                return _type;
            }
            set
            {
                if (value != _type)
                {
                    _type = value;
                    NotifyPropertyChanged("Type");
                }
            }
        }
        private string _icon { get; set; }
        public string Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                if (value != _icon)
                {
                    _icon = value;
                    NotifyPropertyChanged("Icon");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
