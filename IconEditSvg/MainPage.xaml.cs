﻿using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Svg;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Data.Xml.Dom;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Input;
using Microsoft.Graphics.Canvas.Geometry;




// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace IconEditSvg
{
    public enum MouseEventKind { Press,Move,Release, Double };
    public class Command : ICommand
    {
        private readonly Action<object> _action;
        private readonly bool _canExecute;
#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore

        public Command(Action<object> action, bool canExecute)
        {
            this._action = action;
            this._canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute;
        public void Execute(object parameter) => _action(parameter);
    }

    public class CommandForRichEditBox : Command
    {
        private readonly Windows.UI.Xaml.Controls.RichEditBox _richEditBox;

        public CommandForRichEditBox(Action<object> action,
          Windows.UI.Xaml.Controls.RichEditBox richEditBox, bool canExecute)
          : base(action, canExecute)
        {
            _richEditBox = richEditBox;
        }
    }

    public class ViewInfo
    {
        public float Width;
        public float Height;
        public float Scale;
        public float OffsetX;
        public float OffsetY;
        public int TargetItemIndex;
        public int TargetItemPartIndex;
        public int HoverItemIndex;
        public int HoverItemPartIndex;
        public int PressItemIndex;
        public int PressItemPartIndex;

        public ViewInfo()
        {
            Width = 80;
            Height = 80;
            Scale = 6;
            TargetItemIndex = -1;
            TargetItemPartIndex = -1;
            HoverItemIndex = -1;
            HoverItemPartIndex = -1;
            PressItemIndex = -1;
            PressItemPartIndex = -1;

        }
    };

    public class Item
    {
        public string Name { get; set; }
        public ObservableCollection<Item> Children { get; set; } = new ObservableCollection<Item>();

        public override string ToString()
        {
            return Name;
        }
    }
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page,INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private ObservableCollection<Item> DataSource = new ObservableCollection<Item>();

        private ObservableCollection<Item> GetDessertData()
        {
            var list = new ObservableCollection<Item>();

            Item flavorsCategory = new Item()
            {
                Name = "Flavors",
                Children =
                {
                    new Item() { Name = "Vanilla" },
                    new Item() { Name = "Strawberry" },
                    new Item() { Name = "Chocolate" }
                }
            };

            Item toppingsCategory = new Item()
            {
                Name = "Toppings",
                Children =
                {
                    new Item()
                    {
                        Name = "Candy",
                        Children =
                        {
                            new Item() { Name = "Chocolate" },
                            new Item() { Name = "Mint" },
                            new Item() { Name = "Sprinkles" }
                        }
                    },
                    new Item()
                    {
                        Name = "Fruits",
                        Children =
                        {
                            new Item() { Name = "Mango" },
                            new Item() { Name = "Peach" },
                            new Item() { Name = "Kiwi" }
                        }
                    },
                    new Item()
                    {
                        Name = "Berries",
                        Children =
                        {
                            new Item() { Name = "Strawberry" },
                            new Item() { Name = "Blueberry" },
                            new Item() { Name = "Blackberry" }
                        }
                    }
                }
            };

            list.Add(flavorsCategory);
            list.Add(toppingsCategory);
            return list;
        }

        internal void FocusMove()
        {
            EditCanvas.Focus(FocusState.Keyboard);
            var x = FocusManager.GetFocusedElement();
            Debug.WriteLine(x.ToString());
        }
#if false
        private void OrderButton_Click(object sender, RoutedEventArgs e)
        {
            FlavorList.Text = string.Empty;
            ToppingList.Text = string.Empty;

            foreach (muxc.TreeViewNode node in DessertTree.SelectedNodes)
            {
                if (node.Parent.Content?.ToString() == "Flavors")
                {
                    FlavorList.Text += node.Content + "; ";
                }
                else if (node.HasChildren == false)
                {
                    ToppingList.Text += node.Content + "; ";
                }
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (DessertTree.SelectionMode == muxc.TreeViewSelectionMode.Multiple)
            {
                DessertTree.SelectAll();
            }
        }
#endif

        private bool _drawMode=false;
        private bool DrawMode {
            get { return _drawMode; }
            set {
                _drawMode = value;
                EditCanvas.Visibility = value?Visibility.Visible:Visibility.Collapsed;
                OnPropertyChanged("DrawMode");
                if (_drawMode)
                {

                    //EditCanvas.Focus(FocusState.Keyboard);
                    //var x =  FocusManager.GetFocusedElement();
                    _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, (Windows.UI.Core.DispatchedHandler)(async () =>
                    {
                        var result  = await FocusManager.TryFocusAsync(EditCanvas, FocusState.Keyboard);
                        if (result.Succeeded) System.Diagnostics.Debug.WriteLine("成功");
                        else Debug.WriteLine("失敗");
                    }));
                    
                    

                    if (m_path != null)
                    {
                        // まぁ選択状態解除みたいな。
                        m_path = null;
                        MainCanvas.Invalidate();
                        viewInfo.TargetItemIndex = -1;
                        viewInfo.TargetItemPartIndex = -1;
                        viewInfo.PressItemIndex = -1;
                        viewInfo.PressItemPartIndex = -1;
                    }
                    if (_makeLineDrawing != null)
                    {
                        _makeLineDrawing.Reset();
                    }
                }
                else {
                }
            }
        }


        private Command _menuCommand;

        string svgdata;
        byte[] folder40bytes;
        byte[] OrgImageBytes;

        double _opacityValue = 50;


        ViewInfo viewInfo;
        XmlDocument m_svgXmlDoc;

        public MainPage()
        {
            DataContext = this;
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;


            this.Loaded += MainPage_Loaded;
            //var task = testAsync();
            SetupCommand();

            viewInfo = new ViewInfo();
            

            OpacitySlider.Value = _opacityValue;

            InitializeTreeView();

            var appView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            appView.Title = "Assets/folder.svg";


            this.ViewModel = new RecordingViewModel();

            ApplicationDataContainer container = ApplicationData.Current.LocalSettings;

            string path = container.Values["Folder"] as string;
            if (path != null)
            {
                appView.Title = path;
                Task.Run(async () =>
                {
                    StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                    if (folder != null)
                    {
                        _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, (Windows.UI.Core.DispatchedHandler)(() =>
                        {
                            _ = ViewModel.UpdateAsync(folder);
                        }));
                    }
                });
            }

        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            switch (args.VirtualKey) {
                case Windows.System.VirtualKey.Escape:
                    _makeLineDrawing.CancelEvent();
                    break;
            }
        }

        public RecordingViewModel ViewModel { get; set; }


        private void SetupCommand()
        {
            // メニューバーのコマンド
            _menuCommand = new Command(
              async param =>
              {
                  if (param is string s)
                      await (new MessageDialog($"メニュー「{s}」が選択されました。",
                                         "Menu Sample")).ShowAsync();
              }, true);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            /*
            IList<TreeViewNode> l1 = new List<TreeViewNode>();
            for (int i = 0; i < 20; ++i)
            {
                l1.Add(new TreeViewNode() { Content = string.Format("アイテム{0}", i), IsExpanded = true });
                switch (i)
                {
                    case 0:
                        break;
                    default:
                        l1[i - 1].Children.Add(l1[i]);
                        break;
                }
            }
            TreeView1.RootNodes.Add(l1[0]);
            */
            Task.Run((Func<Task>)(async () =>
            {

                // Assetsからのファイル取り出し
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/folder.svg"));

#if false
                // ファイルの読み込み
                svgdata = await FileIO.ReadTextAsync(file);
#else
                XmlLoadSettings xmlLoadSettings = new XmlLoadSettings();
                xmlLoadSettings.ElementContentWhiteSpace = false;
                m_svgXmlDoc = await XmlDocument.LoadFromFileAsync(file, xmlLoadSettings);
                svgdata = m_svgXmlDoc.GetXml();
#endif

                _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, (Windows.UI.Core.DispatchedHandler)(async () =>
                {

                    var i40 = await SampleAsync();
                    if (i40 != null)
                    {
                        Image40.Source = i40;

                        Magnification.Invalidate();
                        //Magnification.Source = i40;
                    }
                    var i80 = await this.makeImageFromSvgAsync((Size)new Size((double)80, (double)80), (Size)new Size((double)80, (double)80));
                    if (i80 != null)
                    {
                        Image80.Source = i80;
                    }

                    if (svgdata != null)
                    {
                        svgText.Text = svgdata;
                        updateTree();
                    }
                }));
            }));


        }

        void updateTree()
        {
            if (m_svgXmlDoc == null) return;

            sampleTreeView.RootNodes.Clear();
            InitializeTreeView();
            //SvgTreeView.RootNodes.Add(trvRoot);

        }
        /// <summary>
        /// 変更に伴う初期化
        /// </summary>
        /// <param name="pre"></param>
        void UpdateEtc(bool pre)
        {
            if (pre)
            {
                DrawMode = false;
                m_path = null;
            }
            else
            {
            }
        }



        /// <summary>
        /// XMLをツリーノードに変換する
        /// </summary>
        /// <param name="xmlParent">変化するXMLデータ</param>
        /// <param name="trvParent">変換されたツリーノード</param>
        private void MakeTree(XmlElement xmlParent, ref TreeViewNode trvParent)
        {
            if (xmlParent.HasChildNodes())
            {
                for (int i = 0; i < xmlParent.ChildNodes.Count; i++)
                {
                    var type = xmlParent.ChildNodes[i].GetType();
                    if (type.ToString().IndexOf("XmlElement") >= 0)
                    {
                        var xmlChild = (XmlElement)xmlParent.ChildNodes[i];
                        TreeViewNode trvChild = new TreeViewNode();

                        string attr = "";
                        if (xmlChild.Attributes != null)
                        {
                            foreach (var a in xmlChild.Attributes)
                            {
                                attr = attr + a.GetXml();
                            }
                        }


                        trvChild.Content = xmlChild.TagName + " " + attr;
                        // 子ノードがまだあるか？
                        if (xmlChild.HasChildNodes())
                            MakeTree(xmlChild, ref trvChild);
                        trvParent.Children.Add(trvChild);
                    }
                    /*
                    else if (xmlParent.ChildNodes[i].GetType().ToString().IndexOf("XmlText") >= 0)
                    {
                        TreeViewNode trvChild = new TreeViewNode();
                        trvChild.Content = xmlParent.ChildNodes[i].InnerText;
                        trvParent.Children.Add(trvChild);
                    }*/
                }
            }
            //親ノードの設定
            trvParent.Content = xmlParent.TagName;
            trvParent.IsExpanded = true;
        }
#if false
        async System.Threading.Tasks.Task testAsync()
        {
            // Assetsからのファイル取り出し
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/folder.svg"));
            // ファイルの読み込み
            svgdata = await FileIO.ReadTextAsync(file);

            /*
            var device = CanvasDevice.GetSharedDevice();

            Size size = new Size(200, 200);
            var offscreen = new CanvasRenderTarget(
                device, (float)size.Width, (float)size.Height, 96);

            using (var ds = offscreen.CreateDrawingSession())
            {
                ds.DrawText("Hello world", 10, 10, Colors.Blue);
            }

            var displayInformation = DisplayInformation.GetForCurrentView();

            */

            /*
            var displayInformation = DisplayInformation.GetForCurrentView();
            using (var stream = new InMemoryRandomAccessStream())
            {
                var device = CanvasDevice.GetSharedDevice();
                var renderer = new CanvasRenderTarget(device,
                                                      100,
                                                      100,
                                                      displayInformation.LogicalDpi);

                using (var ds = renderer.CreateDrawingSession())
                {
                    ds.DrawText("Hello world", 10, 10, Colors.Blue);
                }

                stream.Seek(0);
                await renderer.SaveAsync(stream, CanvasBitmapFileFormat.Png);

                var image = new BitmapImage();
                await image.SetSourceAsync(stream);

                testimage.Source = image;

                return image;
            }
            */

        }


#endif
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void MatCanvas_Draw(CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            // 背景格子
            var color0 = Color.FromArgb(255, 191, 191, 191);
            var color1 = Color.FromArgb(255, 255, 255, 255);
            float w = viewInfo.Width * viewInfo.Scale;
            float h = viewInfo.Height * viewInfo.Scale;
            int x = (int)((sender.ActualWidth - w) / 2);
            int y = (int)((sender.ActualHeight - h) / 2);
            for (int r = 0; r < h; r += 8)
                for (int c = 0; c < w; c += 8)
                {
                    var b = (r / 8 % 2 + c / 8) % 2;
                    Rect rect = new Rect(x + c, y + r, 8, 8);
                    args.DrawingSession.FillRectangle(rect, b == 0 ? color0 : color1);
                }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void RefCanvas_Draw(CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            if (OrgImageBytes != null)
            {
                float vw = viewInfo.Width * viewInfo.Scale;
                float vh = viewInfo.Height * viewInfo.Scale;
                int x = (int)((sender.ActualWidth - vw) / 2);
                int y = (int)((sender.ActualHeight - vh) / 2);

                float w = viewInfo.Scale*2;

                int cl = 40;
                for (int row = 0; row < 40; row++)
                {
                    for (int col = 0; col < cl; col++)
                    {
                        int index = (row * cl + col) * 4;
                        if (index >= OrgImageBytes.Length) break;

                        byte r = OrgImageBytes[index + 2];
                        byte g = OrgImageBytes[index + 1];
                        byte b = OrgImageBytes[index + 0];
                        byte a = OrgImageBytes[index + 3];
//                        a = (byte)((float)a * _opacityValue / 100);
                        Rect rect = new Rect(x+col * w,y+ row * w, w, w);
                        args.DrawingSession.FillRectangle(rect, Color.FromArgb(a, r, g, b));
                    }
                }
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void MainCanvas_Draw(CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            args.DrawingSession.DrawEllipse(155, 115, 80, 30, Colors.Black, 3);
            args.DrawingSession.DrawText("Hello, world!", 100, 100, Colors.Yellow);

            var des = new CompositeEffect();
            //des.Mode = CanvasComposite.SourceAtop;
            //des.Mode = CanvasComposite.Xor;
            des.Mode = CanvasComposite.SourceIn;
            des.Sources.Add(getImage1(sender));
            des.Sources.Add(getImage2(sender));

            args.DrawingSession.DrawImage(des);

            float w = viewInfo.Width * viewInfo.Scale;
            float h = viewInfo.Height * viewInfo.Scale;
            int x = (int)((sender.ActualWidth - w) / 2);
            int y = (int)((sender.ActualHeight - h) / 2);
#if false
            // 背景格子
            var color0 = Color.FromArgb(255, 191, 191, 191);
            var color1 = Color.FromArgb(255, 255, 255, 255);
            for (int r = 0; r < h; r += 8)
                for (int c = 0; c < w; c += 8)
                {
                    var b = (r / 8 % 2 + c / 8) % 2;
                    Rect rect = new Rect(x + c, y + r, 8, 8);
                    args.DrawingSession.FillRectangle(rect, b == 0 ? color0 : color1);
                }
#endif
            // 

            //
            if (svgdata != null)
            {
                args.DrawingSession.Transform = new Matrix3x2(viewInfo.Scale, 0, 0, viewInfo.Scale, x, y);
                var doc = CanvasSvgDocument.LoadFromXml(sender, svgdata);
                if (doc != null)
                {
                    SvgDocOrg = doc;
                }
                if (SvgDocOrg != null)
                {

                    args.DrawingSession.DrawSvg(SvgDocOrg, new Size(viewInfo.Width, viewInfo.Height), 0, 0);
                }
            }
            if (m_path != null)
            {
                viewInfo.OffsetX = x;
                viewInfo.OffsetY = y;
                args.DrawingSession.Transform = new Matrix3x2(1, 0, 0, 1, x, y);
                int index = 0;
                foreach (SvgPathItem item in m_path)
                {
                    item.DrawAnchor(args.DrawingSession, viewInfo,index);
                    /*
                    Point point = item.GetPoint();
                    point.X *= viewInfo.Scale;
                    point.Y *= viewInfo.Scale;
                    args.DrawingSession.DrawEllipse((float)point.X, (float)point.Y, 2, 2, item.GetColor());
                    */
                    index++;
                }

            }


        }

        private void EditCanvas_Draw(CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            if(_makeLineDrawing!=null)
                _makeLineDrawing.Draw(args.DrawingSession, viewInfo);
            /*
            using (var canvasPathBuilder = new Microsoft.Graphics.Canvas.Geometry.CanvasPathBuilder(args.DrawingSession))
            {
                canvasPathBuilder.BeginFigure(100, 100);
                canvasPathBuilder.AddLine(200, 100);
                canvasPathBuilder.AddLine(200, 200);
                canvasPathBuilder.AddCubicBezier(new Vector2(250, 300), new Vector2(250,300), new Vector2(300, 200));
                canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);

                args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(canvasPathBuilder), Colors.Gray, 2);
            }*/

        }

        CanvasBitmap getImage1(CanvasControl sender)
        {
            var device = CanvasDevice.GetSharedDevice();

            Size size = new Size(100, 100);
            var offscreen = new CanvasRenderTarget(
                device, (float)size.Width, (float)size.Height, sender.Dpi);

            using (var ds = offscreen.CreateDrawingSession())
            {
                ds.DrawText("Hello world", 10, 10, Colors.Blue);
            }
            return offscreen;
        }
        CanvasBitmap getImage2(CanvasControl sender)
        {
            var device = CanvasDevice.GetSharedDevice();

            Size size = new Size(100, 100);
            var offscreen = new CanvasRenderTarget(
                device, (float)size.Width, (float)size.Height, sender.Dpi);

            using (var ds = offscreen.CreateDrawingSession())
            {
                //ds.FillRectangle(new Rect(0, 0, 100, 100),Color.FromArgb(100,200,0,0));
                ds.FillRectangle(new Rect(0, 0, 100, 100), Color.FromArgb(255, 0, 255, 0));
            }
            return offscreen;

        }

        void drawSvg(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (svgdata != null)
            {
                var doc = CanvasSvgDocument.LoadFromXml(sender, svgdata);

                args.DrawingSession.Transform = Matrix3x2.CreateScale(0.5f);
                args.DrawingSession.DrawSvg(doc, new Size(40, 40), 0, 0);
                args.DrawingSession.Transform = Matrix3x2.CreateScale(2.0f);
                args.DrawingSession.DrawSvg(doc, new Size(160, 160), 0, 0);
            }

        }

        private CanvasSvgDocument SvgDocOrg;
        private List<SvgPathItem> m_path;
        /// <summary>
        /// svgdata から指定サイズのWriteableBitmapを作成
        /// </summary>
        /// <param name="orgSize"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        async Task<WriteableBitmap> makeImageFromSvgAsync(Size orgSize, Size targetSize)
        {
            var displayInformation = DisplayInformation.GetForCurrentView();
            using (var stream = new InMemoryRandomAccessStream())
            {
                var device = CanvasDevice.GetSharedDevice();
                var renderer = new CanvasRenderTarget(device,
                                                      (float)targetSize.Width,
                                                      (float)targetSize.Height,
                                                      displayInformation.LogicalDpi);
                var doc = CanvasSvgDocument.LoadFromXml(device, svgdata);

                using (var ds = renderer.CreateDrawingSession())
                {
                    ds.Transform = Matrix3x2.CreateScale((float)(targetSize.Height / orgSize.Height));
                    ds.DrawSvg(doc, targetSize);
                }

                stream.Seek(0);
                await renderer.SaveAsync(stream, CanvasBitmapFileFormat.Png);

                var image = new WriteableBitmap((int)targetSize.Width, (int)targetSize.Height);
                await image.SetSourceAsync(stream);

                return image;
            }
        }



        private void Magnification_Draw(CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(new Rect(0, 0, 320, 320), Colors.White);

            if (OrgImageBytes != null)
            {
                int cl = 40;
                for (int row = 0; row < 40; row++)
                {
                    for (int col = 0; col < cl; col++)
                    {
                        int index = (row * cl + col) * 4;
                        if (index >= OrgImageBytes.Length) break;

                        byte r = OrgImageBytes[index + 2];
                        byte g = OrgImageBytes[index + 1];
                        byte b = OrgImageBytes[index + 0];
                        byte a = OrgImageBytes[index + 3];
                        a = (byte)((float)a * _opacityValue / 100);
                        Rect rect = new Rect(col * 8, row * 8, 8, 8);
                        args.DrawingSession.FillRectangle(rect, Color.FromArgb(a, r, g, b));
                    }
                }
            }

            if (folder40bytes != null)
            {
                int cl = 40;
                for (int row = 0; row < 40; row++)
                {
                    for (int col = 0; col < cl; col++)
                    {
                        int index = (row * cl + col) * 4;
                        if (index >= folder40bytes.Length) break;

                        byte r = folder40bytes[index + 2];
                        byte g = folder40bytes[index + 1];
                        byte b = folder40bytes[index + 0];
                        byte a = folder40bytes[index + 3];

                        Rect rect = new Rect(col * 8, row * 8, 8, 8);
                        args.DrawingSession.FillRectangle(rect, Color.FromArgb(a, r, g, b));

                        //Debug.WriteLine("a {0} r {1} g {2} b {3}",a,r,g,b);
                    }
                }
            }

        }

        /// <summary>
        /// svgdata から 96dpi 相当の画像を作成
        /// </summary>
        /// <returns></returns>
        async Task<WriteableBitmap> SampleAsync()
        {
            if (svgdata == null) return null;

            WriteableBitmap writeableBitmap;

            Size targetSize = new Size(40, 40);
            Size orgSize = new Size(80, 80);



            using (var stream = new InMemoryRandomAccessStream())
            {
                var device = CanvasDevice.GetSharedDevice();
                var renderer = new CanvasRenderTarget(device,
                                                      (float)targetSize.Width,
                                                      (float)targetSize.Height,
                                                      96);
                var doc = CanvasSvgDocument.LoadFromXml(device, svgdata);

                using (var ds = renderer.CreateDrawingSession())
                {
                    ds.Transform = Matrix3x2.CreateScale((float)(targetSize.Height / orgSize.Height));
                    ds.DrawSvg(doc, targetSize);
                }

                stream.Seek(0);
                await renderer.SaveAsync(stream, CanvasBitmapFileFormat.Png);

                writeableBitmap = new WriteableBitmap((int)targetSize.Width, (int)targetSize.Height);
                await writeableBitmap.SetSourceAsync(stream);




            }
            if (writeableBitmap == null) return null;
            folder40bytes = writeableBitmap.PixelBuffer.ToArray();
            return writeableBitmap;
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateEtc(true);
            svgdata = svgText.Text;
            UpdateSvg();
        }

        void UpdateSvg()
        {
            bool success = false;
            try
            {
                XmlLoadSettings xmlLoadSettings = new XmlLoadSettings();
                xmlLoadSettings.ElementContentWhiteSpace = false;
                m_svgXmlDoc.LoadXml(svgdata, xmlLoadSettings);
                success = true;

            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex.ToString());
            }
            updateTree();
            MainCanvas.Invalidate();
            _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, (Windows.UI.Core.DispatchedHandler)(async() => {
                WriteableBitmap i40 = null;
                WriteableBitmap i80 = null;
                if (success)
                {
                    i40 = await SampleAsync();
                    i80 = await this.makeImageFromSvgAsync((Size)new Size((double)80, (double)80), (Size)new Size((double)80, (double)80));
                }
                Magnification.Invalidate();
                Image40.Source = i40;
                Image80.Source = i80;
            }));
        }


        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            {
                Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();


                if (folder != null)
                {
                    // Application now has read/write access to all contents in the picked folder
                    // (including other sub-folder contents)
                    Windows.Storage.AccessCache.StorageApplicationPermissions.
                    FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                    TargetFolder.Text = "Picked folder: " + folder.Path;

                    ApplicationDataContainer container = ApplicationData.Current.LocalSettings;
                    container.Values["Folder"] = folder.Path;

                    _ = ViewModel.UpdateAsync(folder);
                }
                else
                {
                    TargetFolder.Text = "Operation cancelled.";
                }


            }

        }

        private void SvgTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            var text = ((TreeViewNode)args.InvokedItem).Content.ToString();

        }

        private void InitializeTreeView()
        {
            // A TreeView can have more than 1 root node. The Pictures library
            // and the Music library will each be a root node in the tree.
            // Get Pictures library.


            var root = new SvgEditData(m_svgXmlDoc?.DocumentElement);

            TreeViewNode rootNode = new TreeViewNode();
            rootNode.Content = root;
            rootNode.IsExpanded = true;
            rootNode.HasUnrealizedChildren = true;
            sampleTreeView.RootNodes.Add(rootNode);
            FillTreeNode(rootNode);
        }

        private void FillTreeNode(TreeViewNode node)
        {
            // Get the contents of the folder represented by the current tree node.
            // Add each item as a new child node of the node that's being expanded.

            // Only process the node if it's a folder and has unrealized children.
            SvgEditData folder = null;

            if (node.Content is SvgEditData && node.HasUnrealizedChildren == true)
            {
                folder = node.Content as SvgEditData;
            }
            else
            {
                // The node isn't a folder, or it's already been filled.
                return;
            }

            IReadOnlyList<SvgEditData> itemsList = folder.GetItems();

            if (itemsList.Count == 0)
            {
                // The item is a folder, but it's empty. Leave HasUnrealizedChildren = true so
                // that the chevron appears, but don't try to process children that aren't there.
                return;
            }

            foreach (var item in itemsList)
            {
                var newNode = new TreeViewNode();
                newNode.Content = item;
                if (item.GetItems().Count > 0)
                {
                    newNode.HasUnrealizedChildren = true;
                    FillTreeNode(newNode);
                    newNode.IsExpanded = true;
                }
                else
                    newNode.HasUnrealizedChildren = false;


                node.Children.Add(newNode);
            }

            // Children were just added to this node, so set HasUnrealizedChildren to false.
            node.HasUnrealizedChildren = false;
        }
#region イベント

        private void MainCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            findItem(e, MouseEventKind.Press);
        }

        private void MainCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            findItem(e, MouseEventKind.Release);
        }


        private void MainCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            findItem(e, MouseEventKind.Move);
        }


        void findItem(PointerRoutedEventArgs e,MouseEventKind kind)
        {
            var ptr = e.Pointer;
            if (ptr.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                var ptrPt = e.GetCurrentPoint(MainCanvas);
                if (m_path != null)
                {
                    int index = 0;
                    int partindex = -1;
                    var pos = ptrPt.Position;
                    pos.X -= viewInfo.OffsetX;
                    pos.Y -= viewInfo.OffsetY;
                    //Debug.WriteLine("x;{0} y:{1}",pos.X,pos.Y);
                    foreach (SvgPathItem item in m_path)
                    {
                        //   item.DrawAnchor(args.DrawingSession, viewInfo.Scale);
                        partindex = item.HitTest(pos, viewInfo.Scale);
                        if (partindex >= 0)
                        {
                            break;
                        }
                        index++;

                    }
                    switch (kind)
                    {
                        case MouseEventKind.Move:
                            {
                                if (viewInfo.HoverItemIndex >= 0 || partindex >= 0)
                                {
                                    viewInfo.HoverItemIndex = index;
                                    viewInfo.HoverItemPartIndex = partindex;
                                    MainCanvas.Invalidate();
                                }
                                break;
                            }
                        case MouseEventKind.Press:
                            {
                                if (partindex >= 0)
                                {
                                    viewInfo.PressItemIndex = index;
                                    viewInfo.PressItemPartIndex = partindex;

                                }
                                else
                                {
                                    viewInfo.PressItemIndex = -1;
                                    viewInfo.PressItemPartIndex = -1;
                                }
                                break;
                            }
                        case MouseEventKind.Release:
                            {
                                if (partindex >= 0 && viewInfo.PressItemIndex == index && viewInfo.PressItemPartIndex == partindex)
                                {
                                    viewInfo.TargetItemIndex = index;
                                    viewInfo.TargetItemPartIndex = partindex;
                                    MainCanvas.Invalidate();
                                }
                                else if(viewInfo.TargetItemIndex>=0) {
                                    viewInfo.TargetItemIndex = -1;
                                    viewInfo.TargetItemPartIndex = -1;
                                    MainCanvas.Invalidate();
                                }
                                viewInfo.PressItemIndex = -1;
                                viewInfo.PressItemPartIndex = -1;
                                break;
                            }
                    }
                    
                }
            }
            e.Handled = true;
        }

        MakeLineDrawing _makeLineDrawing;

        private void EditCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            editpoint(e, MouseEventKind.Press);
        }

        private void EditCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            editpoint(e, MouseEventKind.Move);
        }
        private void EditCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            editpoint(e, MouseEventKind.Release);
        }

        private void EditCanvas_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            if (_makeLineDrawing != null) {
                _makeLineDrawing.CancelEvent();
            }
        }

        private void EditCanvas_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            if (_makeLineDrawing != null)
            {
                _makeLineDrawing.CancelEvent();
            }

        }

        private void EditCanvas_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var pos = e.GetPosition(MainCanvas);
            pos.X -= viewInfo.OffsetX;
            pos.Y -= viewInfo.OffsetY;
            if (_makeLineDrawing == null)
            {
                _makeLineDrawing = new MakeLineDrawing(this);
            }
            _makeLineDrawing.PointerEvent(MouseEventKind.Double, new Vector2((float)pos.X, (float)pos.Y));

        }


        void editpoint(PointerRoutedEventArgs e, MouseEventKind kind)
        {
            var ptr = e.Pointer;
            if (ptr.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                var ptrPt = e.GetCurrentPoint(MainCanvas);
                {
                    var pos = ptrPt.Position;
                    pos.X -= viewInfo.OffsetX;
                    pos.Y -= viewInfo.OffsetY;
                    if (_makeLineDrawing == null) {
                        _makeLineDrawing = new MakeLineDrawing(this);
                    }
                    _makeLineDrawing.PointerEvent(kind,new Vector2((float)pos.X,(float)pos.Y));
                }
            }
        }

        private void EditCanvas_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Escape:
                    break;
            }
        }

        private void EditCanvas_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_makeLineDrawing != null)
            {
                switch (e.Key)
                {
                    case Windows.System.VirtualKey.Escape:
                        _makeLineDrawing.CancelEvent();
                        break;
                }
            }
        }


        #endregion
        private void SampleTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            var node = args.InvokedItem as TreeViewNode;

            if (node.Content is SvgEditData item)
            {
                //node.IsExpanded = !node.IsExpanded;
                m_path = item.GetPathData();
                MainCanvas.Invalidate();
            }
        }

        private async void SampleTreeView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var item = e.OriginalSource;
            if (item is FrameworkElement)
            {


                var menu = new PopupMenu();
                menu.Commands.Add(new UICommand("ノード追加"));
                menu.Commands.Add(new UICommand("キャンセル"));
                var selected = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)item));
            }
        }

        private static Rect GetElementRect(FrameworkElement element)
        {
            var transform = element.TransformToVisual(null);
            var point = transform.TransformPoint(new Point(Canvas.GetLeft(element), Canvas.GetTop(element)));
            return new Rect(point, new Size(100, element.ActualHeight));
        }

        private void SampleTreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
        {
            if (args.Items.Count > 0)
            {
                foreach (TreeViewNode item in args.Items)
                {
                    var svgedit = item.Content as SvgEditData;
                    if (svgedit != null)
                    {
                        if (svgedit.IsRoot())
                        {
                            args.Cancel = true;
                            break;
                        }
                    }
                }
            }
            if (!args.Cancel)
            {

            }


        }


        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            var source = (e.OriginalSource as FrameworkElement)?.DataContext as TreeViewNode;
            if (source != null)
            {

                var svgEdit = source.Content as SvgEditData;
                if (svgEdit != null)
                {
                    if (!svgEdit.IsRoot())
                    {
                        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                        e.Handled = true;
                    }
                }

            }
        }

        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            var source = (e.OriginalSource as FrameworkElement)?.DataContext as TreeViewNode;
            if (source != null)
            {

                var svgEdit = source.Content as SvgEditData;
                if (svgEdit != null)
                {
                    if (!svgEdit.IsRoot())
                    {
                        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                        e.Handled = true;
                    }
                }

            }


        }

        WriteableBitmap PngFile100;
        BitmapImage PngFile200;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var o = IconListView.SelectedItem as PngFileItem;
            if (o != null)
            {
                UpdateEtc(true);
                Task.Run(async () =>
                {
                    string path = o.FullPath;
                    string folder = System.IO.Path.GetDirectoryName(path);
                    string name0 = Path.GetFileName(path);
                    string name1 = null;
                    int num = name0.IndexOf(".scale-100");
                    if (num > 0)
                    {
                        name1 = name0.Replace(".scale-100", ".scale-200");
                    }
                    try
                    {
                        string[] spliststr = name0.Split('.');
                        var file = await CmUtils.FindFileAsync(folder, spliststr[0], "svg");
                        if (file != null)
                        {
                            XmlLoadSettings xmlLoadSettings = new XmlLoadSettings();
                            xmlLoadSettings.ElementContentWhiteSpace = false;
                            m_svgXmlDoc = await XmlDocument.LoadFromFileAsync(file, xmlLoadSettings);
                            svgdata = m_svgXmlDoc.GetXml();
                        }
                        else {
                            svgdata = @"<svg width=""80px"" height=""80px""><path d=""M51 47h21.5l5.875 7-5.875 7H51a2 2 0 0 1-2-2V49a2 2 0 0 1 2-2z"" fill=""none"" stroke-width=""1.8"" stroke=""#000""/></svg>";

                        }
                    }
                    catch (Exception)
                    {
                    }

                    


                    StorageFile file0 = null;
                    try
                    {
                        file0 = await StorageFile.GetFileFromPathAsync(folder + "\\" + name0);
                    }
                    catch (Exception)
                    {

                        file0 = null;
                    }

                    StorageFile file1 = null;
                    if (name1 != null)
                    {
                        try
                        {
                            file1 = await StorageFile.GetFileFromPathAsync(folder + "\\" + name1);
                        }
                        catch (Exception)
                        {

                            file1 = null;
                        }

                    }
                    _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, (Windows.UI.Core.DispatchedHandler)(async () =>
                    {
                        var appView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                        appView.Title = o.FullPath;

                        PngFile100 = null;
                        PngFile200 = null;
                        OrgImage100.Source = null;
                        OrgImage200.Source = null;

                        if (file0 != null)
                        {
                            PngFile100 = new WriteableBitmap(40, 40);
                            using (var s = await file0.OpenReadAsync())
                            {
                                PngFile100.SetSource(s);
                                OrgImageBytes = PngFile100.PixelBuffer.ToArray();
                            }
                        }
                        if (file1 != null)
                        {
                            PngFile200 = new BitmapImage();
                            using (var s = await file1.OpenReadAsync())
                            {
                                PngFile200.SetSource(s);
                            }
                        }
                        OrgImage100.Source = PngFile100;
                        OrgImage200.Source = PngFile200;
                        Magnification.Invalidate();


                        svgText.Text = svgdata;
                        try
                        {
                            XmlLoadSettings xmlLoadSettings = new XmlLoadSettings();
                            xmlLoadSettings.ElementContentWhiteSpace = false;
                            m_svgXmlDoc.LoadXml(svgdata, xmlLoadSettings);

                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                            
                        }
                        
                        
                        RefCanvas.Invalidate();

                        UpdateSvg();
                        

                    }));
                });

            }

        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            _opacityValue = e.NewValue;
            OrgImage100.Opacity = _opacityValue / 100;
            OrgImage200.Opacity = _opacityValue / 100;
            RefCanvas.Opacity = _opacityValue / 100;

            Magnification.Invalidate();
        }

        internal void EditCanvasInvalidate()
        {
            EditCanvas.Invalidate();
        }

    }


    public class ExplorerItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            var node = (TreeViewNode)item;

            return DefaultTemplate;
        }
    }

    public class SvgEditData
    {
        private string _displayName;
        public string DisplayName { get { return _displayName; } }


        public SvgEditData(XmlElement documentElement)
        {
            element = documentElement;
            if (element == null)
            {
                _displayName = "(未読)";

            }
            else
            {

                string attr = "";
                if (element.Attributes != null)
                {
                    foreach (var a in element.Attributes)
                    {
                        attr = attr + a.GetXml();
                    }
                }


                _displayName = element.TagName + " " + attr;
            }

        }

        internal bool IsRoot()
        {

            if (element != null && element.TagName != "svg")
            {
                return false;
            }
            return true;
        }

        XmlElement element;

        internal List<SvgEditData> GetItems()
        {
            var res = new List<SvgEditData>();
            if (element == null || !element.HasChildNodes())
                return res;

            for (int i = 0; i < element.ChildNodes.Count; i++)
            {
                var type = element.ChildNodes[i].GetType();
                if (type.ToString().IndexOf("XmlElement") >= 0)
                {
                    var xmlChild = (XmlElement)element.ChildNodes[i];
                    var svgeditdata = new SvgEditData(xmlChild);
                    res.Add(svgeditdata);
                }
            }
            return res;
        }

        internal List<SvgPathItem> GetPathData()
        {
            if (element == null) return null;

            if (element.TagName == "path")
            {
                if (element.Attributes != null)
                {
                    var data = element.Attributes.GetNamedItem("d");
                    if (data != null)
                    {
                        System.Diagnostics.Debug.WriteLine("");
                        return ParsePath(data.InnerText);
                    }
                }

            }
            return null;
        }

        private List<SvgPathItem> ParsePath(string text)
        {
            List<SvgPathItem> svgPathItems = new List<SvgPathItem>();
            SvgPathItem item = null;
            string num = "";
            foreach (char c in text)
            {
                switch (c)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '.':
                    case '-':
                        if (c == '-' && item != null && num.Length != 0)
                        {
                            item.SetNum(num);
                            num = "";
                        }
                        num = num + c.ToString();
                        break;
                    default:
                        if (item != null && num.Length != 0)
                        {
                            item.SetNum(num);
                            num = "";
                        }
                        if (item != null)
                        {
                            svgPathItems.Add(item);
                        }
                        if (c != ' ' && c != ',')
                        {
                            item = SvgPathItem.Create(c, item);
                        }
                        break;
                }
            }
            if (item != null && num.Length != 0)
            {
                item.SetNum(num);
                num = "";
            }
            if (item != null)
            {
                svgPathItems.Add(item);
            }

            return svgPathItems;
        }
    }

    class SvgPathItem
    {
        List<Point> points;
        public char Command;

        private SvgPathItem befor;
        private int index;
        private Point current;
        private Point beforPoint;

        float rx;   //水平方向の半径
        float ry;//垂直方向の半径
        float x_axis_rotation; //楕円の傾き
        int large_arc_flag; // 1:円弧の長い方を採用，0:短い方を採用
        int sweep_flag;//円弧の方向 1: 時計回りを採用，0:半時計回りを採用

        public static SvgPathItem Create(char command, SvgPathItem item)
        {
            return new SvgPathItem(command, item);
        }

        public SvgPathItem(char command, SvgPathItem item)
        {
            befor = item;
            Command = command;
            index = 0;
            points = new List<Point>();
            beforPoint = item == null ? new Point(0, 0) : item.GetPoint();

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
                                    current = new Point(fnum + (relative ? beforPoint.X : 0), 0);
                                    break;
                                case 6:
                                    current = new Point(current.X, fnum + (relative ? beforPoint.Y : 0));
                                    points.Add(current);
                                    break;
                            }
                            break;
                        }
                    case 'h':
                    case 'H':
                        {
                            current = new Point(fnum + (relative ? beforPoint.X : 0), beforPoint.Y);
                            points.Add(current);
                            break;
                        }
                    case 'v':
                    case 'V':
                        {
                            current = new Point(beforPoint.X, fnum + (relative ? beforPoint.Y : 0));
                            points.Add(current);
                            break;
                        }
                    default:
                        {
                            if (index % 2 == 0)
                            {
                                current = new Point(fnum + (relative ? beforPoint.X : 0), 0);
                            }
                            else
                            {
                                current = new Point(current.X, fnum + (relative ? beforPoint.Y : 0));
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

        public Point GetPoint()
        {
            if (points.Count != 0)
            {
                return points[points.Count - 1];
            }
            return new Point(0, 0);
        }

        public Color GetColor()
        {
            switch (Command)
            {
                case 'M':
                    return Colors.Purple;
                case 'h':
                case 'H':
                case 'v':
                case 'V':
                case 'l':
                case 'L':
                    return Colors.OrangeRed;
                case 'c':
                case 'C':
                    return Colors.Green;
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
        internal void DrawAnchor(CanvasDrawingSession win2d, ViewInfo viewInfo,int myIndex)
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
                    foreach (Point point in points)
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
                        Point point = points[2];
                        float x = (float)point.X * scale;
                        float y = (float)point.Y * scale;

                        DrawAnchorSub(win2d, viewInfo, myIndex, 2, x, y);

                        Point p0 = befor.GetPoint();
                        float x0 = (float)p0.X * scale;
                        float y0 = (float)p0.Y * scale;

                        Point p1 = points[0];
                        float x1 = (float)p1.X * scale;
                        float y1 = (float)p1.Y * scale;
                        win2d.DrawLine(x0, y0, x1, y1, Colors.Green);
                        DrawAnchorSub(win2d, viewInfo, myIndex, 0, x1, y1);


                        Point p2 = points[1];
                        float x2 = (float)p2.X * scale;
                        float y2 = (float)p2.Y * scale;
                        win2d.DrawLine(x, y, x2, y2, Colors.Green);
                        DrawAnchorSub(win2d, viewInfo, myIndex, 1, x2, y2);
                    }
                    break;
                case 'a':
                case 'A':
                    foreach (Point point in points)
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

        private void DrawAnchorSub(CanvasDrawingSession win2d, ViewInfo viewInfo, int myIndex,int partIndex,float x,float y)
        {
            bool ellipse = true;
            switch (Command)
            {
                case 'c':
                case 'C':
                    ellipse = false;
                    break;
                default:
                    break;
            }

            bool hover = viewInfo.HoverItemIndex == myIndex;
            bool select = viewInfo.TargetItemIndex == myIndex;

            Color color = Colors.Blue;
            bool fill = false;
            if (hover && viewInfo.HoverItemPartIndex == partIndex)
            {
                fill = true;
            }
            else if (select && viewInfo.TargetItemPartIndex == partIndex)
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
                Rect r = new Rect(x - size, y - size, size*2, size*2);
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


        internal int HitTest(Point mousePoint,float scale)
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
                    foreach (Point point in points)
                    {
                        float x = (float)point.X * scale;
                        float y = (float)point.Y * scale;
                        if ( IsNear(x,y,mousePoint.X,mousePoint.Y)){
                            return index;
                        }
                        index++;
                    }
                    break;
                case 'c':
                case 'C':
                    if (points.Count == 3)
                    {
                        Point point = points[2];
                        float x = (float)point.X * scale;
                        float y = (float)point.Y * scale;
                        //win2d.DrawEllipse(x, y, 3, 3, Colors.Green);
                        if (IsNear(x, y, mousePoint.X, mousePoint.Y))
                        {
                            return 2;
                        }


                        Point p0 = befor.GetPoint();
                        float x0 = (float)p0.X * scale;
                        float y0 = (float)p0.Y * scale;

                        Point p1 = points[0];
                        float x1 = (float)p1.X * scale;
                        float y1 = (float)p1.Y * scale;
                        if (IsNear(x1, y1, mousePoint.X, mousePoint.Y))
                        {
                            return 0;
                        }


                        Point p2 = points[1];
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
                    foreach (Point point in points)
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

        bool IsNear(double x0,double y0,double x1,double y1)
        {
            if (Math.Pow(x0 - x1, 2) + Math.Pow(y0 - y1, 2) < 16)
                return true;

            return false;
        }

    }
}