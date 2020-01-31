using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Windows.UI;
using static IconEditSvg.MainPage;

namespace IconEditSvg
{


    public class ItemEnumerator : IEnumerator
    {
        int blockIndex = 0;
        int currentIndex;
        List<List<SvgPathItem>> paths;

        public ItemEnumerator(List<List<SvgPathItem>> paths)
        {
            this.paths = paths;
            blockIndex = 0;
            currentIndex = -1;
        }

        public Object Current
        {
            get
            {
                if (blockIndex < 0 || paths.Count <= blockIndex || currentIndex < 0 || paths[blockIndex].Count <= currentIndex)
                    return null;
                return paths[blockIndex][currentIndex];
            }
        }
        public bool MoveNext()
        {
            if (paths == null) return false;

            currentIndex++;
            if (paths[blockIndex].Count <= currentIndex)
            {
                blockIndex++;
                currentIndex = 0;
                if (paths.Count <= blockIndex) return false;
                if (paths[blockIndex].Count <= currentIndex) return false;

            }
            if (currentIndex == 0)
            {
                var path = paths[blockIndex];
                if (path.Count >= 3)
                {
                    var item = path[path.Count - 1];
                    if (item.IsZ())
                    {
                        var item2 = path[path.Count - 2];
                        if (item2.IsC())
                        {
                            currentIndex = 1;
                        }
                    }
                }
            }
            return true;
        }
        public void Reset()
        {
            currentIndex = -1;
            blockIndex = 0;

        }

        public SvgPathData.SvgPathIndex GetPathIndex(int partIndex)
        {
            var pathIndex = new SvgPathData.SvgPathIndex();
            pathIndex.BlockIndex = blockIndex;
            pathIndex.ItemIndex = currentIndex;
            pathIndex.PartIndex = partIndex;

            return pathIndex;
        }
    }

    public class SvgPathData : IEnumerable
    {
        public class SvgPathIndex
        {

            public int BlockIndex { get; set; }
            public int ItemIndex { get; set; }
            public int PartIndex { get; set; }
            public SvgPathIndex()
            {
                BlockIndex = -1;
            }

            public SvgPathIndex(SvgPathIndex hoverIndex)
            {
                BlockIndex = hoverIndex.BlockIndex;
                ItemIndex = hoverIndex.ItemIndex;
                PartIndex = hoverIndex.PartIndex;

            }

            public bool IsValid()
            {
                return (BlockIndex >= 0 && ItemIndex >= 0 && PartIndex >= 0);

            }

            public static bool operator ==(SvgPathIndex a, SvgPathIndex b)
            {
                if (System.Object.ReferenceEquals(a, b))
                {
                    return true;
                }
                if (((object)a == null) || ((object)b == null))
                {
                    return false;
                }

                return (a.BlockIndex == b.BlockIndex && a.ItemIndex == b.ItemIndex && a.PartIndex == b.PartIndex);
            }
            public static bool operator !=(SvgPathIndex a, SvgPathIndex b)
            {
                return !(a == b);
            }

            public override bool Equals(object obj)
            {
                if (obj == null || this.GetType() != obj.GetType())
                    return false;

                var b = obj as SvgPathIndex;
                return (BlockIndex == b.BlockIndex && ItemIndex == b.ItemIndex && PartIndex == b.PartIndex);
            }
            /// <summary>
            /// よくわからん、すべての値をxorするのが一般的らしい。
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return BlockIndex ^ ItemIndex ^ PartIndex;
            }


        }



        SvgPathIndex CurrentIndex;


        List<List<SvgPathItem>> Paths;

        public SvgPathData()
        {

        }

        public SvgPathData(SvgEditData item, PolygonUnit polygonUnitValue)
        {
            CurrentIndex = new SvgPathIndex();
            var m_path = item.GetPathData();
            Paths = new List<List<SvgPathItem>>();
            List<SvgPathItem> path = null;
            if (m_path != null)
            {
                foreach (var p in m_path)
                {
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
        }

        internal void UpdatePolygonVlues(MainPage.PolygonUnit polygonUnitValue)
        {
            // データパース時に中心位置をあらかじめ計算していたが、今はその都度。
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m_path"></param>
        /// <returns></returns>
        static Vector2 CalcCenter(List<SvgPathItem> m_path)
        {
            int offset = IsLastC(m_path) ? 1 : 0;

            var n = (m_path.Count - (1 + offset));
            var item0 = m_path[0];
            var n1 = n / 2;
            var item1 = m_path[n1];
            var p0 = item0.GetPoint();
            var p1 = item1.GetPoint();
            float x = MathF.Round((float)(p0.X + p1.X) / 2);
            float y = MathF.Round((float)(p0.Y + p1.Y) / 2);
            System.Diagnostics.Debug.WriteLine("中心 {0:0.00},{1:0.00}", x, y);

            return new Vector2(x, y);

        }

        static bool IsLastC(List<SvgPathItem> path)
        {
            if (path.Count < 3) return false;
            var item = path[path.Count - 2];
            return item.IsC();
        }


        /// <summary>
        /// 直線と直線の交わりにベジェを挿入し角丸めにする。
        /// </summary>
        /// <returns></returns>
        internal bool InsRoundCorner()
        {
            if (!CurrentIndex.IsValid()) return false;


            var path = Paths[CurrentIndex.BlockIndex];
            if (path.Count <= CurrentIndex.ItemIndex + 1) return false;
            var item = path[CurrentIndex.ItemIndex];
            var next = path[CurrentIndex.ItemIndex + 1];

            if (!item.IsL() || !(next.IsL() || next.IsZ())) return false;


            if (next.IsZ())
            {
                next = path[0]; // M のはず！
            }
            var pn = next.GetPoint();
            SvgPathItem cp = item.CreateRoundCorner(pn);
            path.Insert(CurrentIndex.ItemIndex + 1, cp);
            next.SetBefor(cp);
            cp.SetBefor(item);

            return true;
        }

        private SvgPathItem GetItem(SvgPathIndex currentIndex)
        {
            if (!CurrentIndex.IsValid()) return null;


            var path = Paths[CurrentIndex.BlockIndex];
            var item = path[CurrentIndex.ItemIndex];

            return item;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        internal bool ValueChange(float x, float y)
        {
            if (CurrentIndex.IsValid())
            {
                var m_path = Paths[CurrentIndex.BlockIndex];
                var item = m_path[CurrentIndex.ItemIndex];
                item.ValueChange(CurrentIndex.PartIndex, x, y);
                if (item.IsC() && CurrentIndex.PartIndex==2)
                {
                    int nextindex = CurrentIndex.ItemIndex + 1;
                    if (nextindex >= m_path.Count)
                        nextindex = 0;
                    SvgPathItem next = m_path[nextindex];
                    if (next.IsZ())
                    {
                        next = m_path[0];
                        if (next.IsM())
                        {
                            next.ValueChange(0, x, y);
                            nextindex = 1;
                            next = m_path[nextindex];
                        }
                    }
                    if (next.IsC())
                    {
                        next.ValueChange(0, x, y);
                    }
                }

                return true;

            }
            return false;
        }


        internal bool PolygonChange(float r, float a, int unit, ViewInfo info)
        {
            if (!CurrentIndex.IsValid())
                return false;
            var m_path = Paths[CurrentIndex.BlockIndex];
            if (!IsConsistentAsPolygonData(unit, m_path))
                return false;
            var count = m_path.Count - 1;
            bool lastSame = false;
            if (IsSameLast(m_path))
            {
                lastSame = true;
                count--;
            }
            var index = CurrentIndex.ItemIndex;
            var partIndex = CurrentIndex.PartIndex;
            if (index >= 0)
            {
                var center = CalcCenter(m_path);
                var item0 = m_path[index];
                item0.PolygonChange(info, r, a, center);
                if (item0.IsC())
                {
                    // ベジェ曲線の場合隣のコントロールポイントを
                    int ni = GetNextIndex(m_path, index); // 隣のCのインデックスを返す。(
                    var item2 = m_path[ni];
                    item2.AdjustSymmetric(item0);
                }
                if (lastSame && index == count) {
                    var p = item0.GetPoint();
                    var item = m_path[0];
                    item.SetPoint(p);
                }


                int unitcount = count / unit;
                for (int ix = 1; ix < unitcount; ix++)
                {
                    index += unit;
                    if (lastSame)
                    {
                        if (index > count)
                        {
                            index = index - count;
                        }
                    }
                    else
                    {
                        if (index >= count)
                        {
                            index = index - count;
                        }
                    }
                    var item = m_path[index];
                    item.ApplyOtherValue(item0, info, (360.0f / count * unit) * ix, center);
                    if (item.IsC()) {
                        int ni = GetNextIndex(m_path,index); // 隣のCのインデックスを返す。(
                        var item2 = m_path[ni];
                        item2.AdjustSymmetric(item);
                    }
                    if (lastSame && index == count)
                    {
                        // M の位置を変更
                        var p = item.GetPoint();
                        item = m_path[0];
                        item.SetPoint(p);
                    }
                }

                return true;
            }
            return false;

        }

        int GetNextIndex(List<SvgPathItem> path, int index)
        {
            index++;
            if (path.Count-1> index) {
                return index;
            }
            if (path.Count <= index) {
                index = 1;
            }
            var item = path[index];
            if (item.IsZ())
            {
                return 1;
            }

            return index;
        }


        /// <summary>
        /// 角丸め
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        internal bool RoundCorner(float step)
        {
            if (!CurrentIndex.IsValid())
                return false;

            return false;
        }

        /// <summary>
        /// 多角形データとして矛盾が無いか
        /// </summary>
        /// <returns></returns>
        bool IsConsistentAsPolygonData(int unit, List<SvgPathItem> path)
        {
            if (path == null || path.Count < 3)
                return false;
            var item = path[path.Count - 1];
            if (item.Command != 'z' && item.Command != 'Z') return false;

            int count = path.Count - 1;
            if (IsSameLast(path))
                count--;


            if (count < unit * 2) return false;
            if (count % unit != 0) return false;


            return true;
        }

        bool IsSameLast(List<SvgPathItem> path)
        {
            if (path == null || path.Count < 3)
                return false;
            var item = path[path.Count - 2];
            var itemM = path[0];
            var p1 = item.GetPoint();
            var p0 = itemM.GetPoint();
            return p1 == p0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        internal void MovePath(float x, float y)
        {
            if (!CurrentIndex.IsValid()) return;

            var m_path = Paths[CurrentIndex.BlockIndex];
            foreach (var item in m_path)
            {
                item.MoveAll(x, y);
            }
        }

        internal bool NextHandle(bool IsShift)
        {

            if (CurrentIndex.IsValid())
            {
                var m_path = Paths[CurrentIndex.BlockIndex];
                var item = m_path[CurrentIndex.ItemIndex];
                if (item.NextHandle(CurrentIndex.PartIndex, IsShift))
                {
                    if (IsShift)
                        CurrentIndex.PartIndex--;
                    else
                        CurrentIndex.PartIndex++;
                }
                else
                {
                    if (IsShift)
                    {
                        NextItem(IsShift);
                        if (CurrentIndex.ItemIndex >= 0)
                        {
                            item = m_path[CurrentIndex.ItemIndex];
                            CurrentIndex.PartIndex = item.LastPartIndex();
                        }
                    }
                    else
                    {
                        NextItem(IsShift);
                        CurrentIndex.PartIndex = 0;
                    }
                }
                return true;
            }
            return false;
        }

        internal bool NextItem(bool IsShift)
        {
            if (CurrentIndex.ItemIndex >= 0)
            {
                var m_path = Paths[CurrentIndex.BlockIndex];
                var index = CurrentIndex.ItemIndex;
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
                CurrentIndex.ItemIndex = index;
                return true;
            }
            return false;
        }

        public bool SelectBlock(int i)
        {
            if (Paths == null || Paths.Count == 0) return false;

            if (Paths.Count <= i) return false;

            CurrentIndex.BlockIndex = i;

            return true;
        }


        float ToAngle(float radian)
        {
            return radian * 180 / MathF.PI;
        }


        internal string GetInfo(MainPage.PolygonUnit polygonUnitValue)
        {
            if (CurrentIndex.IsValid())
            {
                var m_path = Paths[CurrentIndex.BlockIndex];
                if (polygonUnitValue != MainPage.PolygonUnit.none)
                {
                    int unit = (int)polygonUnitValue;
                    var count = m_path.Count - 1;
                    if (IsSameLast(m_path))
                        count--;
                    if (count >= unit*2 && count % 2 == 0)
                    {
                        var PolygonCenter = CalcCenter(m_path);
                        string text = string.Format("中心：{0:0.0} {1:0.0}", PolygonCenter.X, PolygonCenter.Y);
                        var index = CurrentIndex.ItemIndex;//
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
                    var index = CurrentIndex.ItemIndex;//
                    var partIndex = CurrentIndex.PartIndex; // 
                    if (index >= 0)
                    {
                        var item = m_path[index];
                        string info = item.GetInfo(partIndex);

                        return info;
                    }
                }
            }
            return "選択されていません";
        }


        public IEnumerator GetEnumerator()
        {
            return new ItemEnumerator(Paths);
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

        internal void SelectHandle(SvgPathIndex pressIndex)
        {
            if (pressIndex != null)
            {
                CurrentIndex = new SvgPathIndex(pressIndex);
            }
            else
            {
                CurrentIndex = new SvgPathIndex();
            }

        }

        internal bool IsSelectHandle()
        {
            return CurrentIndex.IsValid();
        }



        internal bool IsExists()
        {
            if (Paths != null && Paths.Count > 0) return true;
            return false;
        }

        internal SvgPathIndex GetCurrentIndex()
        {
            return CurrentIndex;
        }

        internal void DrawPolygonCenter(ViewInfo info, CanvasDrawingSession win2d, PolygonUnit polygonUnitValue)
        {
            for (int bx = 0; bx < Paths.Count; bx++)
            {
                var path = Paths[bx];
                if (!IsConsistentAsPolygonData((int)polygonUnitValue, path))
                    continue;

                var center = CalcCenter(path);

                float xc = center.X * info.Scale;
                float yc = center.Y * info.Scale;
                win2d.FillEllipse(xc, yc, 4, 4, Colors.DodgerBlue);
            }
        }

        internal void DrawCurrentSelectPath(CanvasDrawingSession win2d, ViewInfo info)
        {
            if (!CurrentIndex.IsValid()) return;
            var item = Paths[CurrentIndex.BlockIndex][CurrentIndex.ItemIndex];
            item.DrawPart(win2d, info);
        }

    }
}
