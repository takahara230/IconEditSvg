﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
                string text = "^" + name + "\\."+ext+"$|^" + name + " \\(\\d+\\)\\." + ext + "$";
                System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(text,RegexOptions.Singleline);

                var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, new List<string>() { "." + ext });
                //var query = folder.CreateFileQuer
                var query = folder.CreateFileQueryWithOptions(queryOptions);
//                query.ApplyNewQueryOptions(new QueryOptions(CommonFileQuery.DefaultQuery, new List<string>() { "." + ext }));
                var list = await query.GetFilesAsync();

                StorageFile atlast = null;
                if (list.Count > 0)
                {
                    foreach (var file in list)
                    {
                        if (reg.IsMatch(file.Name))
                        {
                            System.Diagnostics.Debug.WriteLine(file.Name);
                            if (atlast == null)
                                atlast = file;
                            else
                            {
                                if (DateTimeOffset.Compare(file.DateCreated, atlast.DateCreated) > 0) {
                                    atlast = file;
                                }
                            }
                        }
                    }
                    return atlast;
                }
            }


            return null;
        }

        internal static float Length(Vector2 pb, Vector2 pc)
        {
            return MathF.Sqrt(MathF.Pow(pb.X-pc.X, 2) + MathF.Pow(pb.Y-pc.Y, 2));
        }

        internal static void DebugWriteLine(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }

        internal static float ToAngle(float radian)
        {
            return radian * 180 / MathF.PI;
        }

        internal static Vector2 Coordinate(Vector2 p,float l, float rad)
        {
            float x = MathF.Cos(rad) * l;
            float y = MathF.Sin(rad) * l;
            return new Vector2(p.X+x, p.Y+y);

        }
    }
}
