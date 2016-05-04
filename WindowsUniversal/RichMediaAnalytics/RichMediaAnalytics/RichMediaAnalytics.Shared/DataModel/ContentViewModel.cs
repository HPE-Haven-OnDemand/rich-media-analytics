using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RichMediaAnalytics.DataModels
{
    public class ContentListViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<ContentModel> Items { get; set; }

        public ContentListViewModel()
        {
            this.Items = new ObservableCollection<ContentModel>();
        }
        public bool IsDataLoaded
        {
            get;
            private set;
        }

        public void ClearData()
        {
            if (this.Items.Count > 0)
                this.Items.Clear();
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
