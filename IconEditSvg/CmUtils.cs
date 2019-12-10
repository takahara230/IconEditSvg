using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Search;

namespace IconEditSvg
{
    class CmUtils
    {
        internal static async Task<StorageFile> FindFileAsync(string path, string name, string ext)
        {
            
            
            

            
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);

            if (folder != null)
            {
                string text = "^" + name + "\\."+ext+"$|^" + name + "\\(\\d+\\)\\." + ext + "$";
                System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(text,RegexOptions.Singleline);

                var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, new List<string>() { "." + ext });
                //var query = folder.CreateFileQuer
                var query = folder.CreateFileQueryWithOptions(queryOptions);
//                query.ApplyNewQueryOptions(new QueryOptions(CommonFileQuery.DefaultQuery, new List<string>() { "." + ext }));
                var list = await query.GetFilesAsync();

                foreach (var file in list)
                {
                    if (reg.IsMatch(file.Name)) {
                        System.Diagnostics.Debug.WriteLine(file.Name);
                    }
                }
            }


            return null;
        }
    }
}
