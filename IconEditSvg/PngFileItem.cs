using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Xaml.Media.Imaging;

namespace IconEditSvg
{
    public class PngFileItem
    {
        public string FullPath { get; set; }
        public string CompositionName { get; set; }
        public DateTime ReleaseDateTime { get; set; }
        public BitmapImage BitmapImage { get; set; }
        public PngFileItem()
        {
            this.FullPath = "Wolfgang Amadeus Mozart";
            this.CompositionName = "Andante in C for Piano";
            this.ReleaseDateTime = new DateTime(1761, 1, 1);
        }
        public string OneLineSummary
        {
            get
            {
                return $"{this.CompositionName} by {this.FullPath}, released: "
                    + this.ReleaseDateTime.ToString("d");
            }
        }
    }
    public class RecordingViewModel
    {
        private PngFileItem defaultRecording = new PngFileItem();
        public PngFileItem DefaultRecording { get { return this.defaultRecording; } }


        private ObservableCollection<PngFileItem> recordings = new ObservableCollection<PngFileItem>();
        public ObservableCollection<PngFileItem> Recordings { get { return this.recordings; } }
        public RecordingViewModel()
        {
            this.recordings.Add(new PngFileItem()
            {
                FullPath = "Johann Sebastian Bach",
                CompositionName = "Mass in B minor",
                ReleaseDateTime = new DateTime(1748, 7, 8)
            });
            this.recordings.Add(new PngFileItem()
            {
                FullPath = "Ludwig van Beethoven",
                CompositionName = "Third Symphony",
                ReleaseDateTime = new DateTime(1805, 2, 11)
            });
            this.recordings.Add(new PngFileItem()
            {
                FullPath = "George Frideric Handel",
                CompositionName = "Serse",
                ReleaseDateTime = new DateTime(1737, 12, 3)
            });
        }

        /// <summary>
        /// フォルダにあるファイル一覧取得
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        internal async Task UpdateAsync(StorageFolder folder)
        {
            recordings.Clear();
#if true
            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, new List<string>() { ".png" });
            //var query = folder.CreateFileQuer
            var query = folder.CreateFileQueryWithOptions(queryOptions);
            //                query.ApplyNewQueryOptions(new QueryOptions(CommonFileQuery.DefaultQuery, new List<string>() { "." + ext }));
            var fileList = await query.GetFilesAsync();
#else

            IReadOnlyList<StorageFile> fileList = await folder.GetFilesAsync();
#endif

            //System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("\\.scale-100.png");

            List<string> pngs = new List<string>();
            List<string> png100s = new List<string>();
            List<string> png200s = new List<string>();


            foreach (StorageFile file in fileList)
            {
                string name = file.Name;
                if (!name.EndsWith(".png"))
                    continue;
                if (name.EndsWith(".scale-100.png"))
                {
                    png100s.Add(name);
                }
                else if (name.EndsWith(".scale-200.png")) {
                    png200s.Add(name);
                } else {
                    pngs.Add(name);
                }
            }

            foreach (string name in png100s) {
                string name200 = name.Replace(".scale-100.png", ".scale-200.png");
                png200s.Remove(name200);
            }

            foreach (string name in png100s)
            {
                StorageFile file = null;
                try
                {
                    file = await StorageFile.GetFileFromPathAsync(folder.Path + "\\" + name);


                    var bitmap = new BitmapImage();
                    using (var s = await file.OpenReadAsync())
                    {
                        bitmap.SetSource(s);
                    }

                    this.recordings.Add(new PngFileItem()
                    {
                        FullPath = file.Path,
                        CompositionName = file.DisplayName + file.FileType,
                        ReleaseDateTime = file.DateCreated.LocalDateTime,
                        BitmapImage = bitmap,
                    });
                }
                catch (Exception)
                {

                    file = null;
                }

            }
#if false
            foreach (StorageFile file in fileList)
            { 
                //System.Diagnostics.Debug.WriteLine(file.Name);

                var bitmap = new BitmapImage();
                using (var s = await file.OpenReadAsync())
                {
                    bitmap.SetSource(s);
                }

                this.recordings.Add(new PngFileItem()
                {
                    ArtistName = file.Path,
                    CompositionName = file.DisplayName + file.FileType,
                    ReleaseDateTime = file.DateCreated.LocalDateTime,
                    BitmapImage = bitmap,
                });
            }

#endif

        }
    }
}
