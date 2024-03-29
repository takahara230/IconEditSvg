﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Windows.Foundation;
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
            if (paths == null || paths.Count == 0) return false;

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
                // Mの非表示判定処理
                var list = paths[blockIndex];
                if (list.Count >= 3)
                {
                    var item = list[list.Count - 1];
                    if (item.IsZ())
                    {
                        var item2 = list[list.Count - 2];
                        if (item2.IsC())
                        {
                            var m = list[0];
                            if (m.GetPoint() == item2.GetPoint())
                            {
                                // 最後がベジェでベジェの終点とMの位置が同じならMを非表示に
                                currentIndex = 1;
                            }
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
                return BlockIndex >= 0 && ItemIndex >= 0 && PartIndex >= 0;

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

            internal void DeSelected()
            {
                ItemIndex = -1;
                PartIndex = -1;
            }
        }



        SvgPathIndex CurrentIndex;


        List<List<SvgPathItem>> Paths;

        public SvgPathData(SvgEditData item, PolygonUnit polygonUnitValue)
        {
            CurrentIndex = new SvgPathIndex();
            var m_path = item?.GetPathData();
            Paths = new List<List<SvgPathItem>>();
            List<SvgPathItem> path = null;
            if (m_path != null)
            {
                SvgPathItem top = null;
                SvgPathItem befor = null;
                foreach (var p in m_path)
                {
                    if (p.Command == 'm' || p.Command == 'M')
                    {
                        befor = null;
                        top = p;
                        path = new List<SvgPathItem>();
                        path.Add(p);
                        Paths.Add(path);
                    }
                    else if (path != null)
                    {
                        path.Add(p);
                    }
                    if (befor != null)
                    {
                        befor.Next = p;
                    }
                    befor = p;
                }
            }
        }

        bool RulerEnabled;
        bool RulerVisible;

        internal void RulerShow(Vector2 startPoint, Vector2 endPoint)
        {
            if (!RulerEnabled)
            {
                List<SvgPathItem> path = new List<SvgPathItem>();
                SvgPathItem p1 = new SvgPathItem('M', null);
                p1.SetPoint(startPoint);
                SvgPathItem p2 = new SvgPathItem('L', p1);
                p2.SetPoint(endPoint);
                p1.Next = p2;
                path.Add(p1);
                path.Add(p2);
                Paths.Add(path);
            }
            RulerEnabled = true;
            RulerVisible = true;
        }
        internal void RulerHide()
        {
            RulerVisible = false;
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
        /// 線対称
        /// </summary>
        /// <returns></returns>
        internal bool InvertOnRuler()
        {
            if (RulerEnabled) {
                MainPage.CurrentInstance()?.UndoReg();
                var ruler = Paths[Paths.Count - 1];
                var start = ruler[0].GetPoint();
                var end = ruler[1].GetPoint();
                for (int index = 0; index < Paths.Count - 1; index++) 
                {
                    var list = Paths[index];
                    foreach (var item in list)
                    {
                        item.InvertOnRuler(start,end);

                    }

                }
                return true;
            }
            return false;
        }


        /// <summary>
        /// 制御点を対称にする
        /// </summary>
        /// <returns></returns>
        internal bool MakeSymmetrical(bool fromCommand = true)
        {
            if (Paths.Count <= CurrentIndex.BlockIndex) return false;   // カレントがおかしい
            var path = Paths[CurrentIndex.BlockIndex];
            if (path.Count <= CurrentIndex.ItemIndex) return false;

            var current = new PathIt(path, CurrentIndex.ItemIndex);
            var item = current.GetItem();
            if (!item.IsC()) return false;



            if (CurrentIndex.PartIndex == SvgPathItem.POS_C_END || CurrentIndex.PartIndex == SvgPathItem.POS_C_CONTROLPOINT2)
            {
                var nextc = current.NextC(false);
                if (nextc == null)
                {
                    _ = MainPage.MessageAsync("となりがCと違う");
                    return false;
                }
                var p0 = item.GetPoint(SvgPathItem.POS_C_END);  // 中点
                var p1 = item.GetPoint(SvgPathItem.POS_C_CONTROLPOINT2);  // 制御点
                var x = p0.X + (p0.X - p1.X);
                var y = p0.Y + (p0.Y - p1.Y);

                var next = nextc.GetItem();

                MainPage.CurrentInstance()?.UndoReg();
                next.SetPoint(new Vector2(x, y), SvgPathItem.POS_C_CONTROLPOINT1);
                return true;

            }
            else if (CurrentIndex.PartIndex == SvgPathItem.POS_C_CONTROLPOINT1)
            {
                var nextc = current.NextC(true);
                if (nextc == null)
                {
                    _ = MainPage.MessageAsync("となりがCと違う");
                    return false;
                }
                var next = nextc.GetItem();
                var p0 = next.GetPoint(SvgPathItem.POS_C_END);  // 中点
                var p1 = item.GetPoint(SvgPathItem.POS_C_CONTROLPOINT1);  // 制御点
                var x = p0.X + (p0.X - p1.X);
                var y = p0.Y + (p0.Y - p1.Y);


                MainPage.CurrentInstance()?.UndoReg();
                next.SetPoint(new Vector2(x, y), SvgPathItem.POS_C_CONTROLPOINT2);
                return true;
            }
            return false;
        }

        private SvgPathItem FindSymmetricalItem(SvgPathIndex targetIndex)
        {
            if (Paths.Count <= targetIndex.BlockIndex) return null;   // カレントがおかしい
            var path = Paths[targetIndex.BlockIndex];
            if (path.Count <= targetIndex.ItemIndex) return null;

            var current = new PathIt(path, targetIndex.ItemIndex);
            var item = current.GetItem();
            if (!item.IsC()) return null;


            if (targetIndex.PartIndex == SvgPathItem.POS_C_END || targetIndex.PartIndex == SvgPathItem.POS_C_CONTROLPOINT2)
            {
                return current.NextC(false)?.GetItem();
            }
            else if (targetIndex.PartIndex == SvgPathItem.POS_C_CONTROLPOINT1)
            {
                return current.NextC(true)?.GetItem();
            }
            return null;
        }



        /// <summary>
        /// 直線と直線の交わりにベジェを挿入し角丸めにする。
        /// </summary>
        /// <returns></returns>
        internal bool InsRoundCorner(float r)
        {
            if (!CurrentIndex.IsValid())
            {
                _ = MainPage.MessageAsync("選択されてないかも？");
                return false;
            }


            var path = Paths[CurrentIndex.BlockIndex];
            if (path.Count <= CurrentIndex.ItemIndex + 1) return false;
            var item = path[CurrentIndex.ItemIndex];
            var next = GetNext(path, CurrentIndex.ItemIndex, false, true);
            var befor = GetNext(path, CurrentIndex.ItemIndex, true, true);
            if (next == null || befor == null)
            {
                _ = MainPage.MessageAsync("前か次がが無い！");
                return false;
            }

            if (item.IsC())
            {
                if (!((befor.IsL() || befor.IsM()) && (next.IsL() || next.IsM())))
                {
                    _ = MainPage.MessageAsync("Cの前後はLである必要があります。");
                }
                return CreateRoundCorner2(path, CurrentIndex.ItemIndex, r);
            }
            else if (item.IsL() || (item.IsM() && befor.IsL()))
            {
                return CreateRoundCorner(path, CurrentIndex.ItemIndex, r);
            }
            else
            {
                _ = MainPage.MessageAsync("ここは面取りできません。");
                return false;
            }
        }


        internal static bool CreateRoundCorner(List<SvgPathItem> path, int index, float r)
        {
            var current = path[index];
            var next = GetNext(path, index, false, true);
            var befor = GetNext(path, index, true, true);   // z 以外なら何でも大丈夫
            var pc = current.GetPoint();
            var pb = befor.GetPoint();
            var pn = next.GetPoint();


            var a1 = MathF.Atan2(pb.Y - pc.Y, pb.X - pc.X);//傾き
            var a2 = MathF.Atan2(pn.Y - pc.Y, pn.X - pc.X);//次の線の傾き
            var a = (a1 + a2) / 2;
            a = MathF.Abs(a);   // マイナスって無いのでは？
            if (MathF.Abs(a - a1) > Math.PI)
            {
                a -= MathF.PI;
            }

            float v = MathF.Abs(r / MathF.Tan(a)); // 半径r円の接点の頂点からの距離
            float l1 = CmUtils.Length(pb, pc); // 自分の線の長さ
            float l2 = CmUtils.Length(pc, pn); // 次の線の長さ
            if (l1 < v || l2 < v)
            {
                _ = MainPage.MessageAsync("線の長さがRより短い");
                return false;
            }

            if (current.IsM())
            {
                var l_item = new SvgPathItem('L', befor);       // befor z m l
                l_item.SetPoint(pc);

                InserItem(path, path.Count - 1, l_item);

                befor = l_item;
            }
            else
            {
                befor = current;
            }


            var citem = new SvgPathItem('C', befor);

            var x = MathF.Cos(a1) * v;
            var y = MathF.Sin(a1) * v;
            var p1 = new Vector2(pc.X + x, pc.Y + y);
            x = MathF.Cos(a2) * v;
            y = MathF.Sin(a2) * v;
            var p2 = new Vector2(pc.X + x, pc.Y + y);


            var c = r * 0.5522847f; // 半径rの円弧に近似するためのコントロールポイントの長さ

            x = MathF.Cos(a1 + MathF.PI) * c;
            y = MathF.Sin(a1 + MathF.PI) * c;
            var c1 = new Vector2(p1.X + x, p1.Y + y);
            x = MathF.Cos(a2 + MathF.PI) * c;
            y = MathF.Sin(a2 + MathF.PI) * c;
            var c2 = new Vector2(p2.X + x, p2.Y + y);

            var points = new List<Vector2>();
            points.Add(c1);
            points.Add(c2);
            points.Add(p2);
            citem.SetPoints(points);

            if (current.IsM())
            {
                InserItem(path, path.Count - 1, citem);
            }
            else
            {
                InserItem(path, index + 1, citem);
            }
            if (current.IsM())
            {
                current.SetPoint(p2);
                befor.SetPoint(p1);
            }
            else
            {
                current.SetPoint(p1);
            }
            return true;
        }

        private static Vector2? CrossPoint(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            Vector2 ap1 = new Vector2();

            var dev = (p2.Y - p1.Y) * (p4.X - p3.X) - (p2.X - p1.X) * (p4.Y - p3.Y);
            if (dev == 0)
            {
                return null;
            }


            var d1 = (p3.Y * p4.X - p3.X * p4.Y);
            var d2 = (p1.Y * p2.X - p1.X * p2.Y);


            ap1.X = d1 * (p2.X - p1.X) - d2 * (p4.X - p3.X);
            ap1.X /= dev;
            ap1.Y = d1 * (p2.Y - p1.Y) - d2 * (p4.Y - p3.Y);
            ap1.Y /= dev;

            //return isRange(p1 , p2, ap1) ? 1 : 0;
            return ap1;
        }

        /// <summary>
        /// C の面取りをととのえる
        /// </summary>
        /// <param name="path"></param>
        /// <param name="index"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        internal static bool CreateRoundCorner2(List<SvgPathItem> path, int index, float r)
        {
            var current = new PathIt(path, index);
            var next1 = current.GetNext(PathIt.SearchType.NOT_Z_M, false);
            var befor0 = current.GetNext(PathIt.SearchType.NOT_Z_M, true);
            var befor1 = befor0.GetNext(PathIt.SearchType.NOT_Z_M, true);

            var pn0 = current.GetItem().GetPoint();
            var pn = next1.GetItem().GetPoint();

            var pb = befor1.GetItem().GetPoint();
            var pb0 = befor0.GetItem().GetPoint();

            var vv = CrossPoint(pb0, pb, pn0, pn);
            if (vv == null)
            {
                _ = MainPage.MessageAsync("交点計算エラー");
                return false;
            }
            var pc = vv.Value;


            var a1 = MathF.Atan2(pb.Y - pb0.Y, pb.X - pb0.X);//傾き
            var a2 = MathF.Atan2(pn.Y - pn0.Y, pn.X - pn0.X);//次の線の傾き
            var a = (a1 + a2) / 2;
            a = MathF.Abs(a);   // マイナスって無いのでは？
            if (MathF.Abs(a - a1) > Math.PI)
            {
                a -= MathF.PI;
            }

            float v = MathF.Abs(r / MathF.Tan(a)); // 半径r円の接点の頂点からの距離
            float l1 = CmUtils.Length(pb, pc); // 自分の線の長さ
            float l2 = CmUtils.Length(pc, pn); // 次の線の長さ
            if (l1 < v || l2 < v)
            {
                _ = MainPage.MessageAsync("線の長さがRより短い");
                return false;
            }

            var citem = current.GetItem();

            var x = MathF.Cos(a1) * v;
            var y = MathF.Sin(a1) * v;
            var p1 = new Vector2(pc.X + x, pc.Y + y);
            x = MathF.Cos(a2) * v;
            y = MathF.Sin(a2) * v;
            var p2 = new Vector2(pc.X + x, pc.Y + y);


            var c = r * 0.5522847f; // 半径rの円弧に近似するためのコントロールポイントの長さ

            x = MathF.Cos(a1 + MathF.PI) * c;
            y = MathF.Sin(a1 + MathF.PI) * c;
            var c1 = new Vector2(p1.X + x, p1.Y + y);
            x = MathF.Cos(a2 + MathF.PI) * c;
            y = MathF.Sin(a2 + MathF.PI) * c;
            var c2 = new Vector2(p2.X + x, p2.Y + y);

            var points = new List<Vector2>();
            points.Add(c1);
            points.Add(c2);
            points.Add(p2);
            citem.SetPoints(points);

            var next = current.GetNext(PathIt.SearchType.NOT_Z, false);
            if (next.GetItem().IsM())
            {
                next.GetItem().SetPoint(p2);
            }
            befor0.GetItem().SetPoint(p1);
            return true;
        }

        static void InserItem(List<SvgPathItem> path, int index, SvgPathItem item)
        {
            if (index > 0)
            {
                var cp = path[index - 1];
                cp.Next = item;
                item.SetBefor(cp);
            }
            var next = path[index];
            path.Insert(index, item);
            next.SetBefor(item);
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

            var path = Paths[CurrentIndex.BlockIndex];
            if (path.Count <= CurrentIndex.ItemIndex + 1) return false;
            var item = path[CurrentIndex.ItemIndex];
            if (!(item.IsC() && CurrentIndex.PartIndex == 2)) return false;

            return item.RoundCorner(path, CurrentIndex.ItemIndex, step);
        }


        internal SvgPathItem GetItem(SvgPathIndex currentIndex)
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
                if (item.IsC() && CurrentIndex.PartIndex == 2)
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

        /// <summary>
        /// キーボード操作
        /// </summary>
        /// <param name="keyCmd"></param>
        /// <param name="polygonUnitValue"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        internal bool PointChange(KeyCommand keyCmd, PolygonUnit polygonUnitValue, ViewInfo info)
        {
            var item = GetItem(CurrentIndex);
            if (item == null) return false;

            int unit = (int)polygonUnitValue;
            var m_path = Paths[CurrentIndex.BlockIndex];
            switch (polygonUnitValue)
            {
                case PolygonUnit.unit1:
                case PolygonUnit.unit2:
                case PolygonUnit.unit3:
                case PolygonUnit.unit4:
                    if (!IsConsistentAsPolygonData(unit, m_path))
                        return false;
                    break;
            }
            switch (keyCmd)
            {
                case KeyCommand.Home:
                case KeyCommand.End:
                case KeyCommand.PageUp:
                case KeyCommand.PageDown:
                    if (!(polygonUnitValue != PolygonUnit.none || item.IsC() && (CurrentIndex.PartIndex == 0 || CurrentIndex.PartIndex == 1)))
                    {
                        if (item.IsC() && CurrentIndex.PartIndex == 2)
                        {
                            if (keyCmd == KeyCommand.PageUp)
                                return RoundCorner(1);
                            else if (keyCmd == KeyCommand.PageDown)
                                return RoundCorner(-1);
                            // 面取り
                        }
                        return false;
                    }
                    break;
            }

            bool res = false;

            float moveunit = 0.1f;
            switch (info.MoveUnit)
            {
                case MoveUnitDef.normal:
                    moveunit = 1.0f;
                    break;
                case MoveUnitDef.rough:
                    moveunit = 5.0f;
                    break;
            }
            if (keyCmd == KeyCommand.PageUp || keyCmd == KeyCommand.PageDown)
            {
                moveunit = 1f;
                switch (info.MoveUnit)
                {
                    case MoveUnitDef.normal:
                        moveunit = 5.0f;
                        break;
                    case MoveUnitDef.rough:
                        moveunit = 45.0f;
                        break;
                }
            }

            float dr = 0;
            float da = 0;
            float dx = 0;
            float dy = 0;
            switch (keyCmd)
            {
                case KeyCommand.Home:
                    dr = moveunit;
                    break;
                case KeyCommand.End:
                    dr = -moveunit;
                    break;
                case KeyCommand.PageUp:
                    da = moveunit;
                    break;
                case KeyCommand.PageDown:
                    da = -moveunit;
                    break;
                case KeyCommand.Up:
                    dy = -moveunit;
                    break;
                case KeyCommand.Down:
                    dy = moveunit;
                    break;
                case KeyCommand.Left:
                    dx = -moveunit;
                    break;
                case KeyCommand.Right:
                    dx = moveunit;
                    break;
            }
            Vector2 center = new Vector2(0, 0);
            if (polygonUnitValue == PolygonUnit.RulerOrigin)
            {
                if (RulerVisible)
                {
                    {
                        var ruler = Paths[Paths.Count - 1];
                        center = ruler[0].GetPoint();
                    }
                }
            }
            else if (polygonUnitValue == PolygonUnit.Symmetry)
            {
                if (CurrentIndex.BlockIndex == Paths.Count - 1)
                {
                    var ruler = Paths[Paths.Count - 1];
                    if (CurrentIndex.ItemIndex == 0)
                    {
                        center = ruler[1].GetPoint();
                    }
                    else
                    {
                        center = ruler[0].GetPoint();
                    }
                }
            }
            else if (polygonUnitValue != PolygonUnit.none)
            {
                center = CalcCenter(m_path);
            }
            var oldp = item.GetPoint(CurrentIndex.PartIndex);
            var next = FindSymmetricalItem(CurrentIndex);
            // ↓↓ここで位置変更
            res = item.PointChange(info, polygonUnitValue, CurrentIndex.PartIndex, dx, dy, da, dr, center);
            if (res && polygonUnitValue == PolygonUnit.Symmetry && Paths.Count - 1 != CurrentIndex.BlockIndex)
            {
                // 線対称
                int pc = m_path.Count;
                if (m_path.Count < 2) return res;
                var i1 = m_path[0];
                var i2 = m_path[m_path.Count - 1];
                if (i2.IsZ())
                {
                    if (m_path.Count < 3) return res;
                    i2 = m_path[m_path.Count - 2];
                    pc--;
                }

                int ti = 0;
                var ci = pc / 2;
                if (CurrentIndex.ItemIndex == ci && pc % 2 == 1) return res;
                if (pc % 2 == 1)
                {
                    ti = ci + (ci - CurrentIndex.ItemIndex);
                }
                else
                {
                    ti = ci + (ci - 1 - CurrentIndex.ItemIndex);
                }
                var rulerlist = Paths[Paths.Count - 1];
                var start = rulerlist[0].GetPoint();
                var end = rulerlist[1].GetPoint();
                m_path[ti].ApplyOtherValue(item, CurrentIndex.PartIndex, start, end);
            }
            else if (res && !info.ControlPointIndependent && next != null)
            {
                if (item.IsC() && CurrentIndex.PartIndex != SvgPathItem.POS_C_END)
                {
                    if (CurrentIndex.PartIndex == SvgPathItem.POS_C_CONTROLPOINT1)
                    {
                        if (IsSymmetry(next.GetPoint(SvgPathItem.POS_C_END), next.GetPoint(SvgPathItem.POS_C_CONTROLPOINT2), oldp))
                        {
                            var p0 = next.GetPoint(SvgPathItem.POS_C_END);  // 中点
                            var p1 = item.GetPoint(SvgPathItem.POS_C_CONTROLPOINT1);  // 制御点
                            var x = p0.X + (p0.X - p1.X);
                            var y = p0.Y + (p0.Y - p1.Y);
                            next.SetPoint(new Vector2(x, y), SvgPathItem.POS_C_CONTROLPOINT2);
                        }
                    }
                    else
                    {
                        if (IsSymmetry(item.GetPoint(SvgPathItem.POS_C_END), next.GetPoint(SvgPathItem.POS_C_CONTROLPOINT1), oldp))
                        {
                            var p0 = item.GetPoint(SvgPathItem.POS_C_END);  // 中点
                            var p1 = item.GetPoint(SvgPathItem.POS_C_CONTROLPOINT2);  // 制御点
                            var x = p0.X + (p0.X - p1.X);
                            var y = p0.Y + (p0.Y - p1.Y);
                            next.SetPoint(new Vector2(x, y), SvgPathItem.POS_C_CONTROLPOINT1);
                        }
                    }
                }
            }
            return res;
        }

        private bool IsSymmetry(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            if (p1.X == p0.X - (p2.X - p0.X) && p1.Y == p0.Y - (p2.Y - p0.Y))
                return true;

            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="index"></param>
        /// <param name="befor">前を探す</param>
        /// <param name="visibleOnly">z を飛ばす</param>
        /// <returns></returns>
        private static SvgPathItem GetNext(List<SvgPathItem> path, int index, bool befor, bool visibleOnly)
        {
            if (path.Count <= index) return null; // カレントが変！

            SvgPathItem item = null;
            var step = befor ? -1 : 1;
            while (true)
            {
                index += step;
                if (index < 0)
                {
                    index = path.Count - 1;
                    item = path[index];
                    if (!item.IsZ())
                    {
                        return null;
                    }
                }
                else if (index >= path.Count)
                {
                    if (!item.IsZ())
                        return null;

                    index = 0;
                    item = path[index];
                }
                else
                {
                    item = path[index];
                }
                if (!visibleOnly)
                {
                    return item;
                }
                else if (!(item.IsZ()))
                {
                    return item;
                }
            }
        }


        int GetNextIndex(List<SvgPathItem> path, int index, bool befor, bool visibleOnly)
        {
            if (path.Count <= index) return -1; // カレントが変！

            SvgPathItem item = null;
            var step = befor ? -1 : 1;
            while (true)
            {
                index += step;
                if (index < 0)
                {
                    index = path.Count - 1;
                    item = path[index];
                    if (!item.IsZ())
                    {
                        return -1;
                    }
                }
                else if (index >= path.Count)
                {
                    if (!item.IsZ())
                        return -1;

                    index = 0;
                    item = path[index];
                }
                else
                {
                    item = path[index];
                }
                if (!visibleOnly)
                {
                    return index;
                }
                else if (!(item.IsZ()))
                {
                    return index;
                }
            }
        }



        /// <summary>
        /// 有効な1つ前のインデックスを返す
        /// </summary>
        /// <param name="path"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        int GetPreviousIndex(List<SvgPathItem> path, int index)
        {
            index--;
            if (index < 0)
            {
                index = path.Count - 1;
            }
            var item = path[index];
            if (index == path.Count - 1)
            {
                if (item.IsZ())
                {
                    return path.Count - 2;
                }
                else
                {
                    return -1;
                }
            }
            else if (index == 0)
            {
                if (path[path.Count - 1].IsZ())
                {
                    if (IsSameLast(path))
                    {
                        return path.Count - 2;
                    }
                }
            }
            return index;
        }






        /// <summary>
        /// 多角形データとして矛盾が無いか
        /// </summary>
        /// <returns></returns>
        internal static bool IsConsistentAsPolygonData(int unit, List<SvgPathItem> path)
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

        internal static bool IsSameLast(List<SvgPathItem> path)
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
        internal bool MovePath(float x, float y)
        {
            if (Paths == null || Paths.Count == 0) return false;
            for (int i = 0; i < Paths.Count; i++)
            {
                if (CurrentIndex.IsValid())
                {
                    if (i != CurrentIndex.BlockIndex) continue;
                }
                var m_path = Paths[i];
                if (m_path.Count == 0) continue;
                foreach (var item in m_path)
                {
                    item.MoveAll(x, y);
                }
            }
            return true;
        }


        /// <summary>
        /// マウスによる移動
        /// </summary>
        /// <param name="pressIndex"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        internal bool MovePos(SvgPathIndex pressIndex, Vector2 pos, ViewInfo info)
        {
            var item = Paths[pressIndex.BlockIndex][pressIndex.ItemIndex];
            var oldp = item.GetPoint(CurrentIndex.PartIndex);
            var res = item.MovePos(pressIndex.PartIndex, pos);
            if (res && !info.ControlPointIndependent && item.IsC() && pressIndex.PartIndex != SvgPathItem.POS_C_END)
            {
                var next = FindSymmetricalItem(pressIndex);
                if (next == null)
                    return false;
                if (CurrentIndex.PartIndex == SvgPathItem.POS_C_CONTROLPOINT1)
                {
                    if (IsSymmetry(next.GetPoint(SvgPathItem.POS_C_END), next.GetPoint(SvgPathItem.POS_C_CONTROLPOINT2), oldp))
                    {
                        var p0 = next.GetPoint(SvgPathItem.POS_C_END);  // 中点
                        var p1 = item.GetPoint(SvgPathItem.POS_C_CONTROLPOINT1);  // 制御点
                        var x = p0.X + (p0.X - p1.X);
                        var y = p0.Y + (p0.Y - p1.Y);
                        next.SetPoint(new Vector2(x, y), SvgPathItem.POS_C_CONTROLPOINT2);
                    }
                }
                else
                {
                    if (IsSymmetry(item.GetPoint(SvgPathItem.POS_C_END), next.GetPoint(SvgPathItem.POS_C_CONTROLPOINT1), oldp))
                    {
                        var p0 = item.GetPoint(SvgPathItem.POS_C_END);  // 中点
                        var p1 = item.GetPoint(SvgPathItem.POS_C_CONTROLPOINT2);  // 制御点
                        var x = p0.X + (p0.X - p1.X);
                        var y = p0.Y + (p0.Y - p1.Y);
                        next.SetPoint(new Vector2(x, y), SvgPathItem.POS_C_CONTROLPOINT1);
                    }
                }

            }

            return res;
        }


        internal bool ResizePath(float ratio)
        {
            if (Paths == null || Paths.Count == 0) return false;
            Vector2 center = new Vector2(0, 0);
            for (int i = 0; i < Paths.Count; i++)
            {
                if (CurrentIndex.IsValid())
                {
                    if (i != CurrentIndex.BlockIndex) continue;
                }
                var m_path = Paths[i];
                if (m_path.Count == 0) continue;
                foreach (var item in m_path)
                {
                    item.ResizePath(ratio, center);
                }
            }
            return true;
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
                for (; ix < 3; ix++)
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
                    if (item.IsM())
                    {
                        if (IsSameLast(m_path))
                        {
                            continue;
                        }
                    }
                    if (item.NotZ())
                    {
                        break;
                    }
                }
                if (ix == 3)
                {
                    // エラー
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
                if (polygonUnitValue == PolygonUnit.Symmetry)
                {
                    if (CurrentIndex.BlockIndex == Paths.Count - 1)
                    {
                        Vector2 start = m_path[0].GetPoint();
                        Vector2 end = m_path[1].GetPoint();

                        var v1 = end - start;
                        var a1 = MathF.Atan2(v1.Y, v1.X);


                        string text = string.Format("始点({0:0.00},{1:0.00}) 終点({2:0.00},{3:0.00}) 角度({4:0.0}) ", start.X, start.Y, end.X, end.Y, CmUtils.ToAngle(a1));


                        return text;
                    }
                    else
                    {
                        Vector2 start = new Vector2();
                        Vector2 end = new Vector2();
                        var item = m_path[CurrentIndex.ItemIndex];
                        var p = item.GetPoint(CurrentIndex.PartIndex);
                        if (RulerEnabled)
                        {
                            var ruler = Paths[Paths.Count - 1];
                            start = ruler[0].GetPoint();
                            end = ruler[1].GetPoint();
                        }
                        else
                        {
                            CalcReferenceLine(ref start, ref end);
                        }

                        /*

                        */

                        var v1 = end - start;
                        var a1 = MathF.Atan2(v1.Y, v1.X);
                        var v2 = p - start;
                        var a2 = MathF.Atan2(v2.Y, v2.X);
                        var l = MathF.Abs(MathF.Sin(a2 - a1) * MathF.Sqrt(MathF.Pow(v2.X, 2) + MathF.Pow(v2.Y, 2)));
                        var l2 = MathF.Cos(a2 - a1) * MathF.Sqrt(MathF.Pow(v2.X, 2) + MathF.Pow(v2.Y, 2));


                        string text = string.Format("基準から：{1:0.0} 中心線から：{0:0.0} $$  ", l, l2);

                        string info = item.GetInfo(CurrentIndex.PartIndex, true);
                        return text + info;
                    }

                }
                else if (polygonUnitValue == PolygonUnit.RulerOrigin)
                {
                    if (RulerVisible)
                    {
                        var ruler = Paths[Paths.Count - 1];
                        var v = ruler[0].GetPoint();

                        var item = m_path[CurrentIndex.ItemIndex];
                        string info = item.GetInfo(CurrentIndex.PartIndex, true);


                        string text = string.Format("原点：{0:0.0} {1:0.0}", v.X, v.Y);

                        var p = item.GetPoint();
                        var ofy = p.Y - v.Y;
                        var ofx = p.X - v.X;
                        float r = MathF.Sqrt(MathF.Pow(ofx, 2) + MathF.Pow(ofy, 2));

                        var a = MathF.Atan2(ofy, ofx);

                        text += string.Format(" 半径 {0:0.00} 角度 {1:0.00}", r, ToAngle(a));


                        return text + info;
                    }
                }
                else if (polygonUnitValue != MainPage.PolygonUnit.none)
                {
                    int unit = (int)polygonUnitValue;
                    var count = m_path.Count - 1;
                    if (IsSameLast(m_path))
                        count--;
                    if (count >= unit * 2 && count % 2 == 0)
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


                            var partIndex = CurrentIndex.PartIndex; // 
                            string info = item.GetInfo(partIndex, true);
                            text += info;


                        }
                        return text;
                    }
                }
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
            for (int ix = 0; ix < Paths.Count; ix++)
            {
                if (RulerEnabled && ix == Paths.Count - 1)
                {
                    break;
                }
                var list = Paths[ix];
                foreach (var item in list)
                {
                    pathlist.Add(item);
                }
            }
            return pathlist;
        }

        internal void GetRulerPos(ref Vector2 rulerStartPoint, ref Vector2 rulerEndPoint)
        {
            if (Paths.Count == 0 || !RulerEnabled) return;

            var list = Paths[Paths.Count - 1];
            if (list == null || list.Count < 2) return;

            rulerStartPoint = list[0].GetPoint();
            rulerEndPoint = list[1].GetPoint();
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

        /// <summary>
        /// 中心線を計算
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        internal void CalcReferenceLine(ref Vector2 start, ref Vector2 end)
        {
            bool notset = true;
            var bc = Paths.Count;
            if (RulerVisible)
            {
                bc--;
            }
            Vector2 pn = new Vector2();
            for (int bx = 0; bx < bc; bx++)
            {

                var path = Paths[bx];
                int pc = path.Count;
                if (path.Count < 2) continue;
                var i1 = path[0];
                var i2 = path[path.Count - 1];
                if (i2.IsZ())
                {
                    if (path.Count < 3) return;
                    i2 = path[path.Count - 2];
                    pc--;
                }
                var p1 = i1.GetPoint();
                var p2 = i2.GetPoint();
                pn = (p1 + p2) / 2;
                if (notset)
                {
                    notset = false;
                    start = pn;
                    end = pn;
                }
                else
                {
                    if (pn.X < start.X || (pn.X == start.X && pn.Y < start.Y))
                    {
                        start = pn;
                    }
                    else if (pn.X > end.X || (pn.X == end.X && pn.Y > end.Y))
                    {
                        end = pn;
                    }
                }
                if (pc >= 3)
                {

                    if (pc % 2 != 0)
                    {
                        int t = pc / 2;
                        var i3 = path[t];
                        pn = i3.GetPoint();
                    }
                    else
                    {
                        int t = pc / 2;
                        var i3 = path[t];
                        var i4 = path[t - 1];
                        var p3 = i3.GetPoint();
                        var p4 = i4.GetPoint();
                        pn = (p4 + p3) / 2;

                    }
                    if (pn.X < start.X || (pn.X == start.X && pn.Y < start.Y))
                    {
                        start = pn;
                    }
                    else if (pn.X > end.X || (pn.X == end.X && pn.Y > end.Y))
                    {
                        end = pn;
                    }

                }

            }
        }

        internal void DrawPolygonCenter(ViewInfo info, CanvasDrawingSession win2d, PolygonUnit polygonUnitValue)
        {
            if (polygonUnitValue == PolygonUnit.RulerOrigin) { }
            else if (polygonUnitValue == PolygonUnit.Symmetry)
            {
                /*
                Vector2 start = new Vector2();
                Vector2 end = new Vector2();
                CalcReferenceLine(ref start, ref end);

                start *= info.Scale;
                end *= info.Scale;
                var style = new CanvasStrokeStyle();
                win2d.DrawLine(start, end, Colors.DodgerBlue,2,style);
                */
            }
            else
            {
                var count = Paths.Count;
                if (RulerVisible)
                {
                    count--; // 念のため　表示されてもあまり問題ないけど
                }
                for (int bx = 0; bx < count; bx++)
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
        }

        internal void DrawCurrentSelectPath(CanvasDrawingSession win2d, ViewInfo info)
        {
            if (RulerVisible)
            {
                var item = Paths[Paths.Count - 1][1];
                item.DrawPart(win2d, info);
            }

            if (!CurrentIndex.IsValid()) return;
            {
                var item = Paths[CurrentIndex.BlockIndex][CurrentIndex.ItemIndex];
                item.DrawPart(win2d, info);
            }
        }

        internal static void SetPreviusPoint(List<SvgPathItem> path, int index, Vector2 p1)
        {
            SvgPathItem pre = null;
            Vector2? pm = null;
            index--;
            if (index < 0)
            {
                index = path.Count - 1;
            }
            pre = path[index];
            if (pre.IsM())
            {
                pm = pre.GetPoint();
                pre.SetPoint(p1);
            }
            if (pre.IsZ())
            {
                index--;
                pre = path[index];
            }
            if (pm == null)
            {
                pre.SetPoint(p1);
            }
            else
            {
                var p = pre.GetPoint();
                if (p == pm)
                {
                    pre.SetPoint(p1);
                }
            }
        }

        internal static void SetSameNextPoint(List<SvgPathItem> path, int index)
        {
            var item = path[index];
            var cp = item.GetPoint();
            index++;
            if (index >= path.Count) return;
            if (index == path.Count - 1)
            {
                item = path[index];
                if (!item.IsZ()) return;
                item = path[0];
                if (cp == item.GetPoint())
                {
                    item.SetPoint(cp);
                }
            }
        }

        internal SvgPathItem GetSelectedItem()
        {
            if (!CurrentIndex.IsValid())
                return null;


            var path = Paths[CurrentIndex.BlockIndex];
            return path[CurrentIndex.ItemIndex];
        }

        internal bool IsSelectRuler()
        {
            if (RulerEnabled) {
                return CurrentIndex.BlockIndex == (Paths.Count - 1);
            }
            return false;
        }

        internal bool ConvertToCurve()
        {
            if (!CurrentIndex.IsValid())
                return false;


            var item = GetSelectedItem();
            return item.ConvertToCurve();
        }

        internal void DeSelected()
        {
            if (CurrentIndex.IsValid())
            {
                CurrentIndex.DeSelected();
            }
        }

        internal bool IsRuler(SvgPathIndex pressIndex)
        {
            if (RulerEnabled && pressIndex.BlockIndex == Paths.Count - 1)
            {
                return true;
            }

            return false;
        }
    }
}
