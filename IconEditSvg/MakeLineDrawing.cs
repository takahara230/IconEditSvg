using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace IconEditSvg
{
    class DrawingPoint
    {
        bool _haveControlPoint;
        private Vector2 _point;
        private Vector2 _controlPoint0;
        private Vector2 _controlPoint1;

        public DrawingPoint(Vector2 pos)
        {
            this._point = pos;
            _haveControlPoint = false;
        }

        internal Vector2 getPoint()
        {
            return new Vector2(_point.X, _point.Y);
        }

        internal void UpdateKind(Vector2 pos)
        {
            if (IsNear(pos))
            {
                _haveControlPoint = false;
            }
            else {
                _haveControlPoint = true;
                _controlPoint0 = pos;
            }
        }
        bool IsNear(Vector2 pos)
        {
            return MathF.Pow(_point.X - pos.X,2.0f) + MathF.Pow(_point.Y - pos.Y,2.0f) <= 9.0;
        }

        internal void DarwControlLine(CanvasDrawingSession ds)
        {
            if(_haveControlPoint)
                ds.DrawLine(_point, _controlPoint0, Colors.Yellow);
        }

        internal void DrawHandle(CanvasDrawingSession win2d, DrawingPoint befor,bool atlast)
        {
            if (atlast && befor!=null)
            {
                if (befor.IsHaveControlPoint) {
                    var cp = befor.getControlPoint(false);
                    var pt = befor.getPoint();
                    win2d.DrawLine(pt, cp, Colors.Green);
                    DrawHandle_sub(win2d, cp.X, cp.Y, true, Colors.Green);
                }
            }
            if (_haveControlPoint)
            {
                DrawHandle_sub(win2d, _point.X, _point.Y, true,Colors.Blue);
                if (atlast)
                {
                    var x = _point.X - (_controlPoint0.X - _point.X);
                    var y = _point.Y - (_controlPoint0.Y - _point.Y);

                    win2d.DrawLine(new Vector2(x, y), _controlPoint0, Colors.Green);

                    DrawHandle_sub(win2d, x, y, true, Colors.Green);
                }
                DrawHandle_sub(win2d, _controlPoint0.X, _controlPoint0.Y, true, Colors.Green);
            }
            else
            {
                DrawHandle_sub(win2d, _point.X, _point.Y, false,Colors.Blue);
            }
        }
        internal void DrawHandle_sub(CanvasDrawingSession win2d,float x,float y,bool ellipse,Color color)
        {

                bool fill = false;
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

        internal bool IsHaveControlPoint { get { return _haveControlPoint; } }

        internal Vector2 getControlPoint(bool second)
        {
            if (_haveControlPoint)
            {
                if (second)
                {
                    float x = _controlPoint0.X - _point.X;
                    float y = _controlPoint0.Y - _point.Y;
                    return new Vector2(_point.X - x, _point.Y - y);
                }
                else
                {
                    return _controlPoint0;
                }
            }
            else
                return _point;
        }

        internal void SetPos(Vector2 pos)
        {
            _point = pos;
        }
    }
    class MakeLineDrawing
    {
        private MainPage _mainPage;
        private List<DrawingPoint> _points;

        DrawingPoint pressPoint;
        Vector2 currentPoint;
        DrawingPoint movePoint=null;
        bool statePress = false;


        public MakeLineDrawing(MainPage mainPage)
        {
            this._mainPage = mainPage;
        }

        /// <summary>
        /// マウスイベント
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="pos"></param>
        internal void PointerEvent(MouseEventKind kind, Vector2 pos)
        {
            if (_points == null)
                _points = new List<DrawingPoint>();
            pos = snapPos(pos);
            switch (kind)
            {
                case MouseEventKind.Press:
                    {
                        _mainPage.FocusMove();
                        
                        statePress = true;
                        System.Diagnostics.Debug.WriteLine("Event:Press");
                        if (movePoint != null) {
                            pressPoint = movePoint;
                            movePoint = null;
                        }
                        else
                        {
                            pressPoint = new DrawingPoint(pos);
                            _points.Add(pressPoint);
                        }
                        currentPoint = pos;
                        Invalidate();
                    }
                    break;
                case MouseEventKind.Move:
                    if (_points.Count > 0)
                    {
                        if (pos != currentPoint)
                        {
                            currentPoint = pos;
                            if (pressPoint != null)
                                pressPoint.UpdateKind(pos);
                            else
                            {
                                if (movePoint == null)
                                {
                                    movePoint = new DrawingPoint(pos);
                                    _points.Add(movePoint);
                                }
                                movePoint.SetPos(pos);
                            }
                            Invalidate();
                        }
                    }
                    break;
                case MouseEventKind.Release:
                    System.Diagnostics.Debug.WriteLine("Event:Release");
                    if (statePress)
                    {
                        statePress = false;
                        pressPoint = null;
                        if (_points != null && _points.Count >= 3) {
                            var p0 = _points[0].getPoint();
                            var p1 = _points[_points.Count-1].getPoint();
                            if (p0 == p1) {
                                _mainPage.CreatePath(_points,true);
                                CancelEvent();
                            }
                        }
                        Invalidate();
                    }
                    break;
                case MouseEventKind.Double:
                    _mainPage.CreatePath(_points,false);
                    CancelEvent();
                    break;
            }
        }



        Vector2 snapPos(Vector2 pos)
        {
            if (_points != null && _points.Count > 0)
            {
                int max = _points.Count;
                if (!statePress) max--;
                for(int index=0; index < max; index++) {
                    var p = _points[index];
                    var t = p.getPoint();
                    if (IsNear(t,pos)) {
                        return t;
                    }

                }
            }
            var scale = _mainPage.Info.Scale;

            pos.X = MathF.Round((pos.X - scale) / (scale * 2)) * scale * 2 + scale;
            pos.Y = MathF.Round((pos.Y - scale) / (scale * 2)) * scale * 2 + scale;

            return pos;
        }

        public static bool IsNear(Vector2 p0, Vector2 p1)
        {
            return MathF.Pow(p0.X - p1.X, 2.0f) + MathF.Pow(p0.Y - p1.Y, 2.0f) <= 16.0;
        }

        internal void CancelEvent()
        {
            Reset();
        }

        void MakePath(ref CanvasPathBuilder path, DrawingPoint befor, DrawingPoint target)
        {
            if (befor.IsHaveControlPoint || target.IsHaveControlPoint)
            {
                //                    canvasPathBuilder.AddLine(200, 200);
                //                    canvasPathBuilder.AddCubicBezier(new Vector2(250, 300), new Vector2(250, 300), new Vector2(300, 200));
                Vector2 controlpoint1 = befor.getControlPoint(false);
                Vector2 controlpoint2 = target.getControlPoint(true);
                path.AddCubicBezier(controlpoint1, controlpoint2, target.getPoint());
                
            }
            else
            {
                path.AddLine(target.getPoint());
            }
        }

        object lockObject = new object();

        internal void Draw(CanvasDrawingSession ds, ViewInfo viewInfo)
        {
            //            var dt = DateTime.Now;
            //            System.Diagnostics.Debug.WriteLine(string.Format("Draw:time:{0}",dt.ToString("mm:ss:fff")));
            //            if (_points != null && _points.Count > 0)
            if (_points != null && _points.Count > 0)
            {
                ds.Transform = new Matrix3x2(1, 0, 0, 1, viewInfo.OffsetX, viewInfo.OffsetY);
                if (_points.Count > 1 || !statePress)
                {
                    var canvasPathBuilder = new Microsoft.Graphics.Canvas.Geometry.CanvasPathBuilder(ds);

                    int index = 0;
                    DrawingPoint befor = _points[index];
                    DrawingPoint tp = null;
                    index++;
                    canvasPathBuilder.BeginFigure(befor.getPoint());
                    for (; index < _points.Count; index++)
                    {
                        tp = _points[index];
                        MakePath(ref canvasPathBuilder, befor, tp);
                        befor = tp;
                        tp = null;
                    }

                    canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);

                    ds.DrawGeometry(CanvasGeometry.CreatePath(canvasPathBuilder), Colors.HotPink, 1);
                }

                {
                    DrawingPoint befor = null;
                    DrawingPoint tp = null;

                    for (int index = 0; index < _points.Count; index++)
                    {
                        tp = _points[index];
                        tp.DrawHandle(ds, befor, index == _points.Count - 1);
                        befor = tp;
                    }
                }
            }
        }

        internal void Reset()
        {
            if (_points != null)
            {
                _points.Clear();
            }
            statePress = false;
            movePoint = null;
            pressPoint = null;
            _mainPage.EditCanvasInvalidate();
        }

        private DispatcherTimer _timer;
        bool _invalidate = false;

        void Invalidate()
        {
            _invalidate = true;
            if (_timer == null)
            {
                this._timer = new DispatcherTimer();

                // タイマーイベントの間隔を指定。
                // ここでは1秒おきに実行する
                this._timer.Interval = TimeSpan.FromMilliseconds(100);
                this._timer.Tick += _timer_Tick;
                _timer.Start();
            }
        }

        private void _timer_Tick(object sender, object e)
        {
            if (_invalidate)
            {
                _invalidate = false;
                _mainPage.EditCanvasInvalidate();
            }
        }
    }
}
