using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace IconEditSvg
{
    public class SvgPathItem
    {
        public static int POS_C_CONTROLPOINT1 = 0;
        public static int POS_C_CONTROLPOINT2 = 1;
        public static int POS_C_END = 2;


        List<Vector2> points;
        public char Command;

        private SvgPathItem befor;
        public SvgPathItem Next { set; get; }

        private int index;
        private Vector2 current;
        private Vector2 beforPoint;

        float rx;   //水平方向の半径
        float ry;//垂直方向の半径
        float x_axis_rotation; //楕円の傾き
        int large_arc_flag; // 1:円弧の長い方を採用，0:短い方を採用
        int sweep_flag;//円弧の方向 1: 時計回りを採用，0:半時計回りを採用

        public static SvgPathItem Create(char command, SvgPathItem item)
        {
            return new SvgPathItem(command, item);
        }

        public SvgPathItem(char command, SvgPathItem befor)
        {
            this.befor = befor;
            Command = command;
            index = 0;
            points = new List<Vector2>();
            beforPoint = befor == null ? new Vector2(0, 0) : befor.GetPoint();

        }

        public void SetNum(string num)
        {
            float fnum;
            if (float.TryParse(num, out fnum))
            {
                bool relative = Char.IsLower(Command) ? true : false;
                switch (Command)
                {
                    case 'a':
                    case 'A':
                        {
                            switch (index)
                            {
                                case 0:
                                    rx = fnum;
                                    break;
                                case 1:
                                    ry = fnum;
                                    break;
                                case 2:
                                    x_axis_rotation = fnum;
                                    break;
                                case 3:
                                    large_arc_flag = (int)fnum;
                                    break;
                                case 4:
                                    sweep_flag = (int)fnum;
                                    break;
                                case 5:
                                    current = new Vector2(fnum + (relative ? beforPoint.X : 0), 0);
                                    break;
                                case 6:
                                    current = new Vector2(current.X, fnum + (relative ? beforPoint.Y : 0));
                                    points.Add(current);
                                    break;
                            }
                            break;
                        }
                    case 'h':
                    case 'H':
                        {
                            current = new Vector2(fnum + (relative ? beforPoint.X : 0), beforPoint.Y);
                            points.Add(current);
                            break;
                        }
                    case 'v':
                    case 'V':
                        {
                            current = new Vector2(beforPoint.X, fnum + (relative ? beforPoint.Y : 0));
                            points.Add(current);
                            break;
                        }
                    default:
                        {
                            if (index % 2 == 0)
                            {
                                current = new Vector2(fnum + (relative ? beforPoint.X : 0), 0);
                            }
                            else
                            {
                                current = new Vector2(current.X, fnum + (relative ? beforPoint.Y : 0));
                                points.Add(current);
                                if (Command == 'l')
                                {
                                    beforPoint = current;
                                }
                            }
                        }
                        break;
                }
            }
            index++;

        }

        public Vector2 GetPoint(int partIndex = -1)
        {

            if (partIndex < 0)
            {
                partIndex = points.Count - 1;
            }
            if (partIndex >= 0 && points.Count > partIndex)
            {
                return points[partIndex];
            }
            return new Vector2(0, 0);
        }

        public Windows.UI.Color GetColor()
        {
            switch (Command)
            {
                case 'M':
                    return Color.FromArgb(180, 0, 255, 255);// Colors.Purple;
                case 'h':
                case 'H':
                case 'v':
                case 'V':
                case 'l':
                case 'L':
                    return Colors.OrangeRed;
                case 'c':
                case 'C':
                    return Color.FromArgb(180, 0, 255, 0);// Colors.Green;
                case 'a':
                case 'A':
                    return Colors.HotPink;
                default:
                    return Colors.Blue;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="win2d"></param>
        /// <param name="scale"></param>
        internal void DrawAnchor(CanvasDrawingSession win2d, ViewInfo viewInfo, SvgPathData.SvgPathIndex myIndex)
        {
            float scale = viewInfo.Scale;
            switch (Command)
            {
                case 'M':
                case 'h':
                case 'H':
                case 'v':
                case 'V':
                case 'l':
                case 'L':
                    int partindex = 0;
                    foreach (Vector2 point in points)
                    {
                        float x = (float)point.X * scale;
                        float y = (float)point.Y * scale;
                        DrawAnchorSub(win2d, viewInfo, myIndex, partindex, x, y);
                        partindex++;
                    }
                    break;
                case 'c':
                case 'C':
                    if (points.Count == 3)
                    {
                        Color color = GetColor();
                        Vector2 point = points[2];
                        float x = (float)point.X * scale;
                        float y = (float)point.Y * scale;

                        DrawAnchorSub(win2d, viewInfo, myIndex, 2, x, y);

                        Vector2 p0 = befor.GetPoint();
                        float x0 = (float)p0.X * scale;
                        float y0 = (float)p0.Y * scale;

                        Vector2 p1 = points[0];
                        float x1 = (float)p1.X * scale;
                        float y1 = (float)p1.Y * scale;
                        win2d.DrawLine(x0, y0, x1, y1, color);
                        DrawAnchorSub(win2d, viewInfo, myIndex, 0, x1, y1);


                        Vector2 p2 = points[1];
                        float x2 = (float)p2.X * scale;
                        float y2 = (float)p2.Y * scale;
                        win2d.DrawLine(x, y, x2, y2, color);
                        DrawAnchorSub(win2d, viewInfo, myIndex, 1, x2, y2);
                    }
                    break;
                case 'a':
                case 'A':
                    foreach (Vector2 point in points)
                    {
                        float x = (float)point.X * scale;
                        float y = (float)point.Y * scale;
                        DrawAnchorSub(win2d, viewInfo, myIndex, 0, x, y);
                    }
                    break;
                default:
                    break;
            }
        }

        private void DrawAnchorSub(CanvasDrawingSession win2d, ViewInfo viewInfo, SvgPathData.SvgPathIndex myIndex, int partIndex, float x, float y)
        {
            myIndex.PartIndex = partIndex;
            bool ellipse = true;
            switch (Command)
            {
                case 'c':
                case 'C':
                    if (partIndex == 2)
                        ellipse = false;
                    break;
                default:
                    break;
            }

            //            bool hover = viewInfo.HoverIndex == myIndex;
            bool select = viewInfo.TargetPathData.GetCurrentIndex() == myIndex;

            Color color = Colors.Blue;
            bool fill = false;
            if (viewInfo.HoverIndex == myIndex)
            {
                fill = true;
            }
            else if (select)
            {
                color = GetColor();
                fill = true;
            }
            else
            {
                color = GetColor();
            }
            int size = 4;
            if (ellipse)
            {
                if (fill)
                {
                    win2d.FillEllipse(x, y, size, size, color);
                }
                else
                {
                    win2d.DrawEllipse(x, y, size, size, color);
                }
            }
            else
            {
                Rect r = new Rect(x - size, y - size, size * 2, size * 2);
                if (fill)
                {
                    win2d.FillRectangle(r, color);
                }
                else
                {
                    win2d.DrawRectangle(r, color);
                }

            }

        }


        internal int HitTest(Point mousePoint, float scale)
        {
            switch (Command)
            {
                case 'M':
                case 'h':
                case 'H':
                case 'v':
                case 'V':
                case 'l':
                case 'L':
                    int index = 0;
                    foreach (Vector2 point in points)
                    {
                        float x = (float)point.X * scale;
                        float y = (float)point.Y * scale;
                        if (IsNear(x, y, mousePoint.X, mousePoint.Y))
                        {
                            return index;
                        }
                        index++;
                    }
                    break;
                case 'c':
                case 'C':
                    if (points.Count == 3)
                    {
                        Vector2 point = points[2];
                        float x = (float)point.X * scale;
                        float y = (float)point.Y * scale;
                        if (IsNear(x, y, mousePoint.X, mousePoint.Y))
                        {
                            return 2;
                        }


                        Vector2 p0 = befor.GetPoint();
                        float x0 = (float)p0.X * scale;
                        float y0 = (float)p0.Y * scale;

                        Vector2 p1 = points[0];
                        float x1 = (float)p1.X * scale;
                        float y1 = (float)p1.Y * scale;
                        if (IsNear(x1, y1, mousePoint.X, mousePoint.Y))
                        {
                            return 0;
                        }


                        Vector2 p2 = points[1];
                        float x2 = (float)p2.X * scale;
                        float y2 = (float)p2.Y * scale;



                        if (IsNear(x2, y2, mousePoint.X, mousePoint.Y))
                        {
                            return 1;
                        }

                    }
                    break;
                case 'a':
                case 'A':
                    foreach (Vector2 point in points)
                    {
                        float x = (float)point.X * scale;
                        float y = (float)point.Y * scale;
                        if (IsNear(x, y, mousePoint.X, mousePoint.Y))
                        {
                            return 0;
                        }
                    }
                    break;
                default:
                    break;
            }
            return -1;
        }

        bool IsNear(double x0, double y0, double x1, double y1)
        {
            if (Math.Pow(x0 - x1, 2) + Math.Pow(y0 - y1, 2) < 16)
                return true;

            return false;
        }

        public string GetInfo(int partIndex, bool simple = false)
        {
            string label = "座標";
            Vector2 p = points[0];
            switch (Command)
            {
                case 'C':
                case 'c':
                    {
                        if (points.Count == 3)
                        {
                            p = points[2];
                            if (partIndex == 2)
                            {
                                label = string.Format("座標 {0} {1:0.00} {2:0.00} ", Command, p.X, p.Y);
                            }
                            else if (partIndex == 0 || partIndex == 1)
                            {
                                if (partIndex == 0)
                                    p = befor.GetPoint();
                                Vector2 c = points[partIndex];
                                c.X = c.X - p.X;
                                c.Y = c.Y - p.Y;
                                // コントロールポイントの長さ
                                float l = MathF.Sqrt(c.X * c.X + c.Y * c.Y);
                                // コントロールポイントのアンカーポイントからの角度
                                double r = l / 0.5522847;

                                float a = CmUtils.ToAngle(MathF.Atan2(c.Y, c.X));
                                if (simple)
                                    label = string.Format(" c1 {0:0.00} {1:0.00} r:{2:0.00} (長さ:{3:0.00}角度:{4:0.00})", c.X, c.Y, r, l, a);
                                else
                                    label = string.Format("座標 {0} {1:0.00} {2:0.00} c1 {3:0.00} {4:0.00} r:{5:0.00} (長さ:{6:0.00}角度:{7:0.00})", Command, p.X, p.Y, c.X, c.Y, r, l, a);
                            }
                        }
                        break;

                    }
                default:
                    return string.Format("座標 {0} {1:0.00} {2:0.00} ", Command, p.X, p.Y);
            }
            return label;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="partIndex"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        internal bool ValueChange(int partIndex, float x, float y)
        {
            bool ret = false;
            switch (Command)
            {
                case 'C':
                case 'c':
                    {
                        if (points.Count == 3)
                        {
                            Vector2 p = points[partIndex];
                            Vector2 po = p;
                            p.X += x;
                            p.Y += y;
                            if (x != 0)
                            {
                                p.X = MathF.Round(p.X, 1);
                            }
                            if (y != 0)
                            {
                                p.Y = MathF.Round(p.Y, 1);
                            }
                            points[partIndex] = p;
                            if (partIndex == 2)
                            {
                                var pc = points[1];
                                pc.X += x;
                                pc.Y += y;
                                if (x != 0)
                                {
                                    p.X = MathF.Round(p.X, 1);
                                }
                                if (y != 0)
                                {
                                    p.Y = MathF.Round(p.Y, 1);
                                }
                                points[1] = pc;

                                var next = FindNext();
                                if (next != null && next.IsM() && next.GetPoint() == po)
                                {
                                    next.SetPoint(p);
                                }
                            }
                            else if (partIndex == 0)
                            {
                                var befor = FindBefor();
                                //if(befor!=null && befor.IsC())
                            }
                            ret = true;
                        }
                        break;
                    }
                case 'M':
                case 'l':
                case 'L':
                    {
                        Vector2 p = points[partIndex];
                        p.X += x;
                        p.Y += y;
                        if (x != 0)
                        {
                            p.X = MathF.Round(p.X, 1);
                        }
                        if (y != 0)
                        {
                            p.Y = MathF.Round(p.Y, 1);
                        }
                        points[partIndex] = p;
                        ret = true;
                        break;
                    }
            }
            return ret;
        }

        /// <summary>
        /// 指定ポイントの回転
        /// </summary>
        /// <param name="partindex"></param>
        /// <param name="center"></param>
        /// <param name="da"></param>
        /// <param name="dr"></param>
        /// <returns></returns>
        bool ValueRotate(int partindex, Vector2 center, float da, float dr)
        {
            switch (Command)
            {
                case 'C':
                case 'c':
                    {
                        if (partindex == 2)
                        {
                            Vector2 p = points[2];
                            Vector2 ps = p;
                            p = CalcRotatePosition(p, center, da, dr);
                            points[2] = p;
                            // コントロールポイントも回転
                            ControlRotate(1, center, ps, p);
                            // 次のcのコントロールポイント1を補正
                            var nexti = FindNext();
                            if (nexti != null)
                            {
                                if (nexti.IsM())
                                {
                                    nexti.SetPoint(p);
                                    if (nexti.GetPoint() == ps)
                                    {
                                        // 同じだったら
                                        nexti = nexti.Next;
                                    }
                                }
                                if (nexti.IsC())
                                {
                                    nexti.ControlRotate(0, center, ps, p);
                                }
                            }
                        }
                        else
                        {
                            var p = points[partindex];
                            p = CalcRotatePosition(p, center, da, dr);
                            points[partindex] = p;
                        }
                        break;
                    }
                case 'M':
                case 'l':
                case 'L':
                    {
                        if (partindex != 0) return false;
                        Vector2 p = points[0];
                        p = CalcRotatePosition(p, center, da, dr);
                        points[0] = p;
                        break;
                    }
            }

            return true;
        }

        void ControlRotate(int partIndex, Vector2 center, Vector2 po, Vector2 pc)
        {
            switch (Command)
            {
                case 'C':
                case 'c':
                    {
                        if (partIndex == 0 || partIndex == 1)
                        {
                            var cp = points[partIndex];
                            cp = CalcRotatePosition2(cp, center, po, pc);
                            points[partIndex] = cp;

                        }
                        break;
                    }
            }
        }



        internal void PolygonChange(ViewInfo info, float or, float oa, Vector2 center)
        {
            switch (Command)
            {
                case 'C':
                case 'c':
                    {
                        Vector2 p = points[2];
                        Vector2 ps = p;
                        p = CalcRotatePosition(p, center, oa, or);
                        points[2] = p;

                        var cp = points[1];
                        cp = CalcRotatePosition2(cp, center, ps, p);
                        points[1] = cp;
                        break;
                    }
                case 'M':
                case 'l':
                case 'L':
                    {
                        Vector2 p = points[0];
                        p = CalcRotatePosition(p, center, oa, or);
                        points[0] = p;
                        break;
                    }
            }
        }

        /// <summary>
        /// item の線対称の値をセット
        /// </summary>
        /// <param name="item"></param>
        /// <param name="partIndex"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        internal void ApplyOtherValue(SvgPathItem item, int partIndex, Vector2 start, Vector2 end)
        {
            if (item.IsC() || IsC())
            {
                switch (Command)
                {
                    case 'L':
                    case 'l':
                    case 'M':
                    case 'm':
                        {
                            if (item.IsC())
                            {
                                // 自分の次はCで無いとつじつまが合わない
                                var next = this.Next;
                                if (next == null || !next.IsC()) return;

                                if (partIndex == 2)
                                {
                                    points[0] = CalcSymmetricPoint(item.GetPoint(partIndex), start, end);
                                }
                                if (partIndex == 2 || partIndex == 1)
                                {
                                    var v = CalcSymmetricPoint(item.GetPoint(1), start, end);
                                    next.SetPoint(v, 0);
                                }
                                if (partIndex == 0)
                                {
                                    var v = CalcSymmetricPoint(item.GetPoint(1), start, end);
                                    next.SetPoint(v, 1);
                                }
                            }
                            break;
                        }
                    case 'c':
                    case 'C':
                        // 自分が C 
                        if (item.IsC())
                        {
                            if (partIndex == 2) //終点　この場合は全体を移動
                            {
                                {
                                    var v = CalcSymmetricPoint(item.GetPoint(), start, end);
                                    SetPoint(v, 2);
                                }
                                // 制御点をitemの制御点にあわせる
                                {
                                    // item の終点の制御点を next の始点の制御点に
                                    var next = this.Next;
                                    if (next != null && next.IsC())
                                    {
                                        var v = CalcSymmetricPoint(item.GetPoint(1), start, end);
                                        next.SetPoint(v, 0);
                                    }
                                }
                                {
                                    // item の 次の始点の制御点を this の終点の制御点に
                                    var next = item.Next;

                                    if (next != null && next.IsC())
                                    {
                                        var v = CalcSymmetricPoint(next.GetPoint(0), start, end);
                                        SetPoint(v, 1);
                                    }
                                }
                            }
                            else if (partIndex == 0)
                            {
                                // 相手側の始点制御点の変更　なので、相手側の対象点を自分の次のアイテムの終点制御点へ
                                var next = this.Next;
                                if (next != null && next.IsC())
                                {
                                    var v = CalcSymmetricPoint(item.GetPoint(0), start, end);
                                    next.SetPoint(v, 1);
                                }
                            }
                        }
                        else if (item.IsL() || item.IsM())
                        {
                            // itemの次はCで無いとつじつまが合わない
                            var next = item.Next;
                            if (next == null || !next.IsC()) return;

                            {
                                var v = CalcSymmetricPoint(item.GetPoint(), start, end);
                                SetPoint(v, 2);
                            }

                            {
                                var v = CalcSymmetricPoint(next.GetPoint(0), start, end);
                                SetPoint(v, 1);
                            }
                        }
                        break;
                }
            }
            else if (item.IsL() || item.IsM())
            {
                switch (Command)
                {
                    case 'L':
                    case 'l':
                    case 'M':
                    case 'm':
                        {
                            points[0] = CalcSymmetricPoint(item.GetPoint(), start, end);
                        }
                        break;
                }
            }
        }
        /// <summary>
        /// 線対称の位置を計算
        /// </summary>
        /// <param name="v"></param>
        /// <param name="start">線対称の線の始点</param>
        /// <param name="end">線対称の線の終点</param>
        /// <returns></returns>
        Vector2 CalcSymmetricPoint(Vector2 v, Vector2 start, Vector2 end)
        {
            var v0 = end - start;
            var a0 = MathF.Atan2(v0.Y, v0.X);
            var v1 = v - start;
            var a1 = MathF.Atan2(v1.Y, v1.X);
            var a = a0 - (a1 - a0);
            var l = CmUtils.Length(v, start);

            Vector2 vn = new Vector2(l * MathF.Cos(a), l * MathF.Sin(a));
            vn += start;
            return vn;
        }




        /// <summary>
        /// 現在の座標を指定されたいテムを指定した角度回転した値にセット。
        /// </summary>
        /// <param name="item0"></param>
        /// <param name="partIndex"></param>
        /// <param name="center"></param>
        /// <param name="da"></param>
        internal void ApplyOtherValue2(SvgPathItem item0, int partIndex, Vector2 center, float da)
        {
            switch (Command)
            {
                case 'C':
                case 'c':
                    {
                        if (item0.IsC())
                        {
                            if (partIndex == 2 || partIndex == 1)
                            {
                                Vector2 p = item0.GetPoint();
                                Vector2 ps = GetPoint();
                                p = CalcRotatePosition(p, center, da);
                                points[2] = p;
                                // コントロールポイント2も同様に、
                                var cp2 = item0.GetControlPoint(true);
                                cp2 = CalcRotatePosition(cp2, center, da);
                                points[1] = cp2;
                                //  隣のコントロールポイント1も同様に
                                var nexti = FindNext();
                                if (nexti.IsC())
                                {
                                    nexti.ControlRotate(0, center, ps, p);
                                }
                            }
                            else if (partIndex == 0)
                            {
                                /*
                                SvgPathItem item = item0.FindBefor();
                                Vector2 p = item.GetPoint();

                                p = CalcRotatePosition(p, center, da);
                                var itemb = FindBefor();
                                itemb.SetPoint(p);

                                // コントロールポイント
                                var cp1 = item0.GetControlPoint(false);
                                cp1 = CalcRotatePosition(p, center, da);
                                points[0] = cp1;
                                */
                                var p = item0.GetControlPoint(false);
                                p = CalcRotatePosition(p, center, da);
                                points[0] = p;
                            }
                        }
                        break;
                    }
                case 'M':
                case 'l':
                case 'L':
                    {
                        Vector2 p = item0.GetPoint();
                        p = CalcRotatePosition(p, center, da);
                        points[0] = p;
                        break;
                    }
            }
        }

        internal void MoveAll(float x, float y)
        {
            if (points != null && points.Count > 0)
            {
                for (int ix = 0; ix < points.Count; ix++)
                {
                    Vector2 p = points[ix];
                    p.X += x;
                    p.Y += y;
                    points[ix] = p;
                }
            }
        }

        internal void MovePos(int partIndex, float x, float y)
        {
            Vector2 p = points[partIndex];
            p.X += x;
            p.Y += y;
            points[partIndex] = p;

        }

        internal bool MovePos(int partIndex, Vector2 pos, bool forKey = false)
        {
            var p0 = points[partIndex];
            var x = pos.X;
            var y = pos.Y;
            if (!forKey)
            {
                x = MathF.Round(pos.X);
                y = MathF.Round(pos.Y);
            }
            x = x - p0.X;
            y = y - p0.Y;
            if (x == 0 && y == 0) return false;

            if (IsC())
            {
                var next = FindNext(true);
                MovePos(partIndex, x, y);
                if (partIndex == POS_C_END)
                {
                    MovePos(POS_C_CONTROLPOINT2, x, y);
                    if (next?.IsC() == true)
                    {
                        next.MovePos(POS_C_CONTROLPOINT1, x, y);
                        if (this.Next?.IsZ() == true)
                        {
                            var n2 = this.FindNext();
                            if (n2?.IsM() == true)
                            {
                                n2?.MovePos(0, x, y);
                            }
                        }
                    }
                }
                else if (!MainPage.CurrentInstance().Info.ControlPointIndependent)
                {
                    if (partIndex == POS_C_CONTROLPOINT1)
                    {
                        var befor = FindBefor();
                    }
                    else
                    {
                    }
                }
            }
            else
            {
                MoveAll(x, y);
            }
            return true;
        }

        internal void ResizePath(float ratio, Vector2 center)
        {
            if (points != null && points.Count > 0)
            {
                for (int ix = 0; ix < points.Count; ix++)
                {
                    Vector2 p = points[ix];
                    var v = p - center;
                    v *= ratio;
                    points[ix] = v;
                }
            }

        }


        internal string Encode()
        {
            string path = "";
            var c = Command;
            c = Char.ToUpper(c);
            if (c == 'V' || c == 'H')
            {
                c = 'L';
            }
            path = path + c;
            foreach (Vector2 point in points)
            {
                path = path + string.Format("{0:0.00} {1:0.00} ", point.X, point.Y);
            }

            return path;
        }

        internal bool NextHandle(int partIndex, bool IsShift)
        {
            if (IsShift)
            {
                return partIndex != 0;
            }
            else
            {
                if (points.Count > partIndex + 1)
                {
                    return true;
                }
            }
            return false;
        }

        internal int LastPartIndex()
        {
            return points.Count - 1;
        }

        internal bool NotZ()
        {
            return Command != 'z' && Command != 'Z';
        }



        private Vector2 GetControlPoint(bool second)
        {
            return points[second ? 1 : 0];
        }

        static Vector2 CalcRotatePosition(Vector2 p, Vector2 center, float oa)
        {
            var c = center;
            float ofx = p.X - c.X;
            float ofy = p.Y - c.Y;
            float r = MathF.Sqrt(MathF.Pow(ofx, 2) + MathF.Pow(ofy, 2));

            float a = MathF.Atan2(ofy, ofx);
            a = a + oa * MathF.PI / 180;

            //
            p.X = c.X + r * MathF.Cos(a);
            p.Y = c.Y + r * MathF.Sin(a);
            return p;
        }
        /// <summary>
        /// center を基準に指定角度、指定長さ変更した位置を計算、角度は1度、長さは0.1で丸めます、
        /// </summary>
        /// <param name="p"></param>
        /// <param name="center"></param>
        /// <param name="oa"></param>
        /// <param name="or"></param>
        /// <returns></returns>
        static Vector2 CalcRotatePosition(Vector2 p, Vector2 center, float oa, float or, bool proximate = false)
        {
            var c = center;
            float ofx = p.X - c.X;
            float ofy = p.Y - c.Y;
            float r = MathF.Sqrt(MathF.Pow(ofx, 2) + MathF.Pow(ofy, 2)) + or;
            r = MathF.Round(r, 1);
            float a = MathF.Atan2(ofy, ofx);
            a = a + oa * MathF.PI / 180;

            a = MathF.Round(a * 180 / MathF.PI) * MathF.PI / 180;

            //
            float vc = MathF.Cos(a);
            p.X = c.X + r * vc;
            p.Y = c.Y + r * MathF.Sin(a);

            return p;
        }

        /// <summary>
        /// コントロールポイントの回転、元のアンカーポイント(po)との角度を保持したまま アンカーポイント(pn)に移動いた時の位置を計算
        /// </summary>
        /// <param name="p"></param>
        /// <param name="center"></param>
        /// <param name="po"></param>
        /// <param name="pn"></param>
        /// <returns></returns>
        static Vector2 CalcRotatePosition2(Vector2 p, Vector2 center, Vector2 po, Vector2 pc)
        {
            float radApOriginal = MathF.Atan2(po.Y - center.Y, po.X - center.X); //アンカーポイントの角度
            float radCpOriginal = MathF.Atan2(p.Y - po.Y, p.X - po.X);// コントロールポイントの角度
            float radApCurrent = MathF.Atan2(pc.Y - center.Y, pc.X - center.X);
            float rad = radCpOriginal + (radApCurrent - radApOriginal);
            float l = MathF.Sqrt(MathF.Pow(p.X - po.X, 2) + MathF.Pow(p.Y - po.Y, 2));
            p.X = l * MathF.Cos(rad) + pc.X;
            p.Y = l * MathF.Sin(rad) + pc.Y;

            return p;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        internal void SetPoint(Vector2 p, int partIndex = -1)
        {
            switch (Command)
            {
                case 'l':
                case 'L':
                case 'M':
                case 'm':
                    if (points.Count == 0)
                    {
                        points.Add(p);
                    }
                    else
                    {
                        points[0] = p;
                    }
                    break;
                case 'c':
                case 'C':
                    if (partIndex > 2) return;
                    if (partIndex < 0)
                    {
                        partIndex = 2;
                    }
                    points[partIndex] = p;
                    break;

            }
        }


        internal bool IsM()
        {
            return Command == 'm' || Command == 'M';
        }
        internal bool IsZ()
        {
            return Command == 'z' || Command == 'Z';
        }
        internal bool IsC()
        {
            return Command == 'c' || Command == 'C';
        }
        internal bool IsL()
        {
            return Command == 'l' || Command == 'L' || Command == 'h' || Command == 'H' || Command == 'v' || Command == 'V';
        }

        internal void DrawPart(CanvasDrawingSession win2d, ViewInfo info)
        {
            switch (Command)
            {
                case 'm':
                case 'M':
                    {
                        var b = FindBefor(false);
                        if (b != null && b.IsZ()) 
                        {
                            var bb = b.befor;
                            var p0 = GetPoint();
                            var p1 = bb.GetPoint();
                            if (p1 != p0) {
                                drawLine(win2d, info, p1, p0);
                            }
                        }
                        break;
                    }
                case 'c':
                case 'C':
                    {
                        if (befor != null)
                        {
                            var bp = befor.GetPoint();
                            bp.X *= info.Scale;
                            bp.Y *= info.Scale;


                            var canvasPathBuilder = new Microsoft.Graphics.Canvas.Geometry.CanvasPathBuilder(win2d);

                            int index = 0;
                            index++;
                            canvasPathBuilder.BeginFigure(bp);
                            var p0 = points[0];
                            var p1 = points[1];
                            var p2 = points[2];
                            p0.X *= info.Scale;
                            p0.Y *= info.Scale;
                            p1.X *= info.Scale;
                            p1.Y *= info.Scale;
                            p2.X *= info.Scale;
                            p2.Y *= info.Scale;
                            canvasPathBuilder.AddCubicBezier(p0, p1, p2);

                            canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);

                            win2d.DrawGeometry(CanvasGeometry.CreatePath(canvasPathBuilder), Colors.HotPink, 1);

                        }
                    }
                    break;
                case 'l':
                case 'L':
                case 'h':
                case 'H':
                case 'v':
                case 'V':
                    {
                        if (befor != null)
                        {
                            var bp = befor.GetPoint();
                            int index = 0;
                            index++;
                            var p0 = points[0];
                            drawLine(win2d, info, bp, p0);
                        }
                    }
                    break;

            }
        }

        private void drawLine(CanvasDrawingSession win2d, ViewInfo info,Vector2 bp,Vector2 p0)
        {
            
            bp.X *= info.Scale;
            bp.Y *= info.Scale;


            var canvasPathBuilder = new Microsoft.Graphics.Canvas.Geometry.CanvasPathBuilder(win2d);

            canvasPathBuilder.BeginFigure(bp);
            p0.X *= info.Scale;
            p0.Y *= info.Scale;

            canvasPathBuilder.AddLine(p0);

            canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);

            win2d.DrawGeometry(CanvasGeometry.CreatePath(canvasPathBuilder), Colors.HotPink, 1);

        }


        internal void SetBefor(SvgPathItem cp)
        {
            befor = cp;
            beforPoint = befor.GetPoint();
        }

        internal void SetPoints(List<Vector2> points)
        {
            this.points = points;
        }

        internal void AdjustSymmetric(SvgPathItem item)
        {
            if (!IsC() || !item.IsC()) return;
            var c = item.GetControlPoint(true);
            var p = item.GetPoint();

            c.X = p.X + (p.X - c.X);
            c.Y = p.Y + (p.Y - c.Y);
            points[0] = c;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="index">自分のindex</param>
        /// <param name="step"></param>
        /// <returns></returns>
        internal bool RoundCorner(List<SvgPathItem> path, int index, float step)
        {
            SvgPathItem b1 = path[index - 1]; // 自分はＣなので最低Ｍは存在する。
            var p1 = b1.GetPoint();
            var p3 = GetPoint();
            SvgPathItem b2 = null;
            if (index >= 2)
                b2 = path[index - 2];
            else
            {
                var item = path[path.Count - 1];
                if (item.IsZ())
                {
                    b2 = path[path.Count - 2];
                    var p = b2.GetPoint();
                    if (p == p1)
                    {
                        if (b2.IsL())
                        {
                            b2 = null;
                        }
                    }
                }
            }
            if (b2 == null) return false;
            var p2 = b2.GetPoint();
            SvgPathItem n1 = null;
            if (index <= path.Count - 2)
            {
                n1 = path[index + 1];
                if (n1.IsZ())
                {
                    n1 = path[0];
                    var p = n1.GetPoint();
                    if (p == p3)
                    {
                        n1 = path[1];
                        if (!n1.IsL())
                            return false;
                    }
                }
            }
            else
            {
                return false;
            }
            // 交点求める公式より
            var p4 = n1.GetPoint();
            float dev = (p2.Y - p1.Y) * (p4.X - p3.X) - (p2.X - p1.X) * (p4.Y - p3.Y);
            if (dev == 0) return false;
            float d1 = p3.Y * p4.X - p3.X * p4.Y;
            float d2 = p1.Y * p2.X - p1.X * p2.Y;
            float x = d1 * (p2.X - p1.X) - d2 * (p4.X - p3.X);
            x /= dev;
            float y = d1 * (p2.Y - p1.Y) - d2 * (p4.Y - p3.Y);
            y /= dev;



            if (RoundSub(new Vector2(x, y), p2, p4, ref p1, ref p3, step))
            {
                SvgPathData.SetPreviusPoint(path, index, p1);
                SvgPathData.SetSameNextPoint(path, index);
                return true;
            }
            return false;
        }

        bool RoundSub(Vector2 pc, Vector2 pb, Vector2 pn, ref Vector2 pb2, ref Vector2 pn2, float step)
        {

            var a1 = MathF.Atan2(pc.Y - pb.Y, pc.X - pb.X);//傾き
            var a2 = MathF.Atan2(pn.Y - pc.Y, pn.X - pc.X);//次の線の傾き

            var a = (a1 + a2) / 2;
            a = MathF.Abs(a);
            if (a > MathF.PI)
            {
                a -= MathF.PI;
            }
            // a は中線の傾き

            float lb2 = CmUtils.Length(pc, pb2);
            float ln2 = CmUtils.Length(pc, pn2);
            float l = MathF.Min(lb2, ln2);

            var ac2 = MathF.Abs((a1 - a2) / 2);
            if (ac2 > MathF.PI)
            {
                ac2 -= MathF.PI;
            }

            float r = l * MathF.Tan(ac2);
            r = MathF.Round(r, 1);
            if (step > 0)
            {
                if (r < 2)
                {
                    step = 0.1f;
                }
                else if (r < 2.4)
                {
                    step = 2.4f - r;
                }
                else
                {
                    step = 0.1f;
                }
            }
            else
            {
                if (r <= 2.0f)
                {
                    step = -0.1f;
                }
                else if (r < 2.5)
                {
                    step = 2.0f - r;

                }
                else
                {
                    step = -0.5f;
                }
            }
            r += step;
            ///

            float v = 0;

            v = r / MathF.Tan(a); // 半径r円の接点の頂点からの距離
            float l1 = CmUtils.Length(pb, pc); // 自分の線の長さ
            float l2 = CmUtils.Length(pc, pn); // 次の線の長さ
            if (l1 < v || l2 < v) return false;




            var x = MathF.Cos(a1) * v;
            var y = MathF.Sin(a1) * v;
            var p1 = new Vector2(pc.X - x, pc.Y - y);
            x = MathF.Cos(a2) * v;
            y = MathF.Sin(a2) * v;
            var p2 = new Vector2(pc.X + x, pc.Y + y);


            var c = r * 0.5522847f; // 半径rの円弧に近似するためのコントロールポイントの長さ

            x = MathF.Cos(a1) * c;
            y = MathF.Sin(a1) * c;
            var c1 = new Vector2(p1.X + x, p1.Y + y);
            x = MathF.Cos(a2) * c;
            y = MathF.Sin(a2) * c;
            var c2 = new Vector2(p2.X - x, p2.Y - y);

            //
            points[0] = c1;
            points[1] = c2;
            points[2] = p2;

            pb2 = p1;
            pn2 = p2;




            //            this.points[0] = p1;

            CmUtils.DebugWriteLine(string.Format("a1:{0:0.00},a2:{1:0.00},ac:{2:0.00},r:{3:0.00}", CmUtils.ToAngle(a1), CmUtils.ToAngle(a2), CmUtils.ToAngle(ac2), r));

            return true;
        }

        /// <summary>
        /// 直線をベジェに変換、コントロールポイントは直線上に1/3の長さで設定
        /// </summary>
        /// <returns></returns>
        internal bool ConvertToCurve()
        {
            var p0 = befor.GetPoint();
            var p1 = GetPoint();
            var l = CmUtils.Length(p0, p1) / 3;
            var rad = MathF.Atan2(p1.Y - p0.Y, p1.X - p0.X);

            points.Insert(0, CmUtils.Coordinate(p0, l, rad));
            points.Insert(1, CmUtils.Coordinate(p1, l, rad + MathF.PI));
            Command = 'C';


            return true;
        }

        /// <summary>
        /// 位置変更メイン
        /// </summary>
        /// <param name="polygonUnit"></param>
        /// <param name="partIndex"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="da"></param>
        /// <param name="dr"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        internal bool PointChange(ViewInfo info, MainPage.PolygonUnit polygonUnit, int partIndex, float dx, float dy, float da, float dr, Vector2 center)
        {
            int unit = (int)polygonUnit;
            bool rot = true;
            if (dx != 0 || dy != 0)
                rot = false;

            if (rot)
            {
                if (IsC() && partIndex != 2)
                {
                    if (partIndex == 1)
                        center = points[2];
                    else
                        center = befor.GetPoint(); // C の場合最低前にMが存在。
                }
                ValueRotate(partIndex, center, da, dr);
            }
            else
            {
                // 移動
                //ValueChange(partIndex, dx, dy);
                var p = GetPoint(partIndex);
                p.X += dx;
                p.Y += dy;
                MovePos(partIndex, p, true);
            }

            //----------------------------------------------------------
            if (polygonUnit == MainPage.PolygonUnit.Symmetry)
            {

            }
            else if (unit > 0 && polygonUnit != MainPage.PolygonUnit.RulerOrigin)
            {
                if (IsC() && partIndex != 2)
                {
                    var v = CalcCenter();
                    if (v == null) return false;
                    center = v.Value;
                }
                SvgPathItem last = null;
                var top = FindTop();
                int count = PathCount(ref last);
                var item = this;
                for (int cx = 1; cx < count / unit; cx++)
                {
                    for (int ix = 0; ix < unit; ix++)
                    {
                        item = item.FindNext(true);
                    }
                    if (item == this)
                        break;
                    item.ApplyOtherValue2(this, partIndex, center, (360.0f / count * unit) * cx);
                }
            }

            return true;

        }


        SvgPathItem FindTop()
        {
            var top = this;
            for (; ; )
            {
                if (top.befor == null)
                    break;
                top = top.befor;
            }
            return top;
        }

        int PathCount(ref SvgPathItem last,bool excludingCtrElement = true)
        {
            // 先頭を探す
            var top = FindTop();
            // アイテム数を数えるのと最後を見つける
            last = top;
            int count = 1;
            for (; ; )
            {
                if (last.Next == null || (excludingCtrElement && last.Next.IsZ()))
                    break;
                last = last.Next;
                count++;
            }
            if (last.Next != null)
            {
                if (top.GetPoint() == last.GetPoint())
                    count--;
            }
            return count;
        }

        /// <summary>
        /// 中心を返す、循環してなかったらnullを返す
        /// </summary>
        /// <returns></returns>
        private Vector2? CalcCenter()
        {
            // 先頭を探す
            var top = FindTop();
            SvgPathItem last = null;
            int count = PathCount(ref last);

            int offset = last.IsC() ? 1 : 0;


            var item0 = top;
            var n1 = count / 2;
            var item1 = top;// = m_path[n1];
            int index = 0;
            for (; ; )
            {
                if (item1.Next == null) return null;
                if (index == n1)
                    break;
                item1 = item1.Next;
                index++;
            }
            var p0 = item0.GetPoint();
            var p1 = item1.GetPoint();
            float x = MathF.Round((float)(p0.X + p1.X) / 2);
            float y = MathF.Round((float)(p0.Y + p1.Y) / 2);
            System.Diagnostics.Debug.WriteLine("中心 {0:0.00},{1:0.00}", x, y);

            return new Vector2(x, y);

        }

        /// <summary>
        /// z 関係なく前を探す
        /// </summary>
        /// <param name="excludingCtrElement"></param>
        /// <returns></returns>
        private SvgPathItem FindBefor(bool excludingCtrElement = true)
        {
            var item = befor;
            if (item == null || (excludingCtrElement && item.IsM()))
            {
                SvgPathItem last = null;
                PathCount(ref last, excludingCtrElement);
                if (item == null) return last;
                if (item.GetPoint() == last.GetPoint())
                {
                    return last;
                }
            }
            return item;
        }



        SvgPathItem FindNext(bool skipSameM = false)
        {
            if (Next == null) return null;
            if (!Next.IsZ()) return Next;

            var top = FindTop();
            var p0 = GetPoint();
            var p1 = top.GetPoint();
            if (skipSameM && p0 == p1)
            {
                return top.Next;
            }

            return top;
        }



    }
}
