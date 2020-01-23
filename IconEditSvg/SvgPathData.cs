using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static IconEditSvg.MainPage;

namespace IconEditSvg
{
    public class SvgPathData : IEnumerable
    {
        int TargetItemIndex;
        int TargetItemPartIndex;
        Vector2 PolygonCenter;
        int blockIndex = 0;
        List<List<SvgPathItem>> Paths;

        public SvgPathData()
        {
            
        }

        public SvgPathData(SvgEditData item,PolygonUnit polygonUnitValue)
        {
            TargetItemPartIndex = -1;
            TargetItemIndex = -1;
            var m_path = item.GetPathData();
            Paths = new List<List<SvgPathItem>>();
            List<SvgPathItem> path = null;
            foreach (var p in m_path) {
                if (p.Command == 'm' || p.Command == 'M')
                {
                    path = new List<SvgPathItem>();
                    path.Add(p);
                    Paths.Add(path);
                }
                else if (path != null)
                {
                    path.Add(p);
                }
            }
            UpdatePolygonVlues(polygonUnitValue);
        }

        internal void UpdatePolygonVlues(MainPage.PolygonUnit polygonUnitValue)
        {
            var m_path = Paths[blockIndex];
            if (m_path != null && polygonUnitValue != PolygonUnit.none)
            {
                var n = (m_path.Count - 1);
                var item0 = m_path[0];
                var n1 = n / 2;
                var item1 = m_path[n1];
                var p0 = item0.GetPoint();
                var p1 = item1.GetPoint();
                float x = MathF.Round((float)(p0.X + p1.X) / 2);
                float y = MathF.Round((float)(p0.Y + p1.Y) / 2);
                System.Diagnostics.Debug.WriteLine("中心 {0:0.00},{1:0.00}", x, y);

                PolygonCenter = new Vector2(x, y);
            }
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        internal bool ValueChange(float x, float y)
        {
            if (TargetItemIndex >= 0)
            {
                var m_path = Paths[blockIndex];
                var item = m_path[TargetItemIndex];
                item.ValueChange(TargetItemPartIndex, x, y);

                return true;

            }
            return false;
        }

        internal bool PolygonChange(float r, float a,int unit, ViewInfo info)
        {
            if (!IsConsistentAsPolygonData(unit))
                return false;
            var m_path = Paths[blockIndex];
            var count = m_path.Count - 1;
            var index = TargetItemIndex;//
            var partIndex = TargetItemPartIndex; // 
            if (index >= 0)
            {
                var item0 = m_path[index];
                item0.PolygonChange(info, r, a);


                int unitcount = count / unit;
                for (int ix = 1; ix < unitcount; ix++)
                {
                    index += unit;
                    if (index >= count)
                    {
                        index = index - count;
                    }
                    var item = m_path[index];
                    item.ApplyOtherValue(item0, info, (360.0f / count * unit) * ix);
                }

                return true;
            }
            return false;

        }

        /// <summary>
        /// 多角形データとして矛盾が無いか
        /// </summary>
        /// <returns></returns>
        bool IsConsistentAsPolygonData(int unit)
        {
            var m_path = Paths[blockIndex];
            if (m_path == null)
                return false;
            int count = m_path.Count - 1; // z があるぜんてい
            if (count < unit * 2) return false;
            if (count % unit != 0) return false;


            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        internal void MovePath(float x, float y)
        {
            var m_path = Paths[blockIndex];
            foreach (var item in m_path)
            {
                item.MoveAll(x, y);
            }
        }

        internal bool NextHandle(bool IsShift)
        {
            if (TargetItemIndex >= 0 && TargetItemPartIndex >= 0)
            {
                var m_path = Paths[blockIndex];
                var item = m_path[TargetItemIndex];
                if (item.NextHandle(TargetItemPartIndex, IsShift))
                {
                    if (IsShift)
                        TargetItemPartIndex--;
                    else
                        TargetItemPartIndex++;
                }
                else
                {
                    if (IsShift)
                    {
                        NextItem(IsShift);
                        if (TargetItemIndex >= 0)
                        {
                            item = m_path[TargetItemIndex];
                            TargetItemPartIndex = item.LastPartIndex();
                        }
                    }
                    else
                    {
                        NextItem(IsShift);
                        TargetItemPartIndex = 0;
                    }
                }
                return true;
            }
            return false;
        }

        internal bool NextItem(bool IsShift)
        {
            if (TargetItemIndex >= 0)
            {
                var m_path = Paths[blockIndex];
                var index = TargetItemIndex;
                int step = IsShift ? -1 : 1;
                int ix = 0;
                for (; ix < 2; ix++)
                {
                    index += step;
                    if (index < 0)
                    {
                        index = m_path.Count - 1;
                    }
                    else if (index >= m_path.Count)
                    {
                        index = 0;
                    }


                    var item = m_path[index];
                    if (item.NotZ())
                    {
                        break;
                    }
                }
                if (ix == 2)
                {
                    index = -1;
                }
                TargetItemIndex = index;
                return true;
            }
            return false;
        }

        public bool SelectBlock(int i)
        {
            if (Paths == null || Paths.Count==0) return false;

            if (Paths.Count <= i) return false;

            blockIndex = i;
            
            return true;
        }

        public IEnumerator GetEnumerator()
        {
            return new ItemEnumerator(Paths,blockIndex);
        }

        float ToAngle(float radian)
        {
            return radian * 180 / MathF.PI;
        }


        internal string GetInfo(MainPage.PolygonUnit polygonUnitValue)
        {
            var m_path = Paths[blockIndex];
            if (polygonUnitValue != MainPage.PolygonUnit.none)
            {
                var count = m_path.Count - 1;
                if (count >= 8 && count % 2 == 0)
                {
                    string text = string.Format("中心：{0:0.0} {1:0.0}", PolygonCenter.X, PolygonCenter.Y);
                    var index = TargetItemIndex;//
                    if (index >= 0)
                    {
                        var item = m_path[index];
                        var p = item.GetPoint();
                        var v = PolygonCenter;
                        var ofy = p.Y - v.Y;
                        var ofx = p.X - v.X;
                        float r = MathF.Sqrt(MathF.Pow(ofx, 2) + MathF.Pow(ofy, 2));

                        var a = MathF.Atan2(ofy, ofx);

                        text += string.Format(" 半径 {0:0.00} 角度 {1:0.00}", r, ToAngle(a));


                    }
                    return text;
                }
            }
            else
            {
                var index = TargetItemIndex;//
                var partIndex = TargetItemPartIndex; // 
                if (index >= 0)
                {
                    var item = m_path[index];
                    string info = item.GetInfo(partIndex);

                    return info;
                }
            }

            return "選択されていません";
        }





        public class ItemEnumerator : IEnumerator
        {
            int blockIndex = 0;
            int currentIndex;
            List<List<SvgPathItem>> paths;

            public ItemEnumerator(List<List<SvgPathItem>> paths, int blockIndex)
            {
                this.paths = paths;
                this.blockIndex = blockIndex;
                currentIndex = -1;
            }

            public Object Current
            {
                get
                {
                    if (currentIndex < 0 || paths[blockIndex].Count <= currentIndex)
                        return null;
                    return paths[blockIndex][currentIndex];
                }
            }
            public bool MoveNext()
            {
                if (paths == null) return false;

                currentIndex++;
                if (paths[blockIndex].Count <= currentIndex)
                    return false;
                return true;
            }
            public void Reset()
            {
                currentIndex = -1;

            }
        }

        internal List<SvgPathItem> GetPathList()
        {
            var pathlist = new List<SvgPathItem>();
            foreach (var list in Paths)
            {
                foreach (var item in list)
                {
                    pathlist.Add(item);
                }
            }
            return pathlist;
        }

        internal void SelectHandle(int index, int partindex)
        {
            TargetItemIndex = index;
            TargetItemPartIndex = partindex;
        }

        internal bool IsSelectHandle()
        {
            return (TargetItemIndex >= 0);
        }

        internal bool IsSameIndex(int itemIndex, int partIndex)
        {
            if (TargetItemIndex < 0) return false;

            return (TargetItemIndex == itemIndex && TargetItemPartIndex == partIndex);
        }
    }
}
