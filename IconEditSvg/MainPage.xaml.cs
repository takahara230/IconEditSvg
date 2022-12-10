using Microsoft.Graphics.Canvas;
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
using Windows.UI.Core;
using Windows.System;




// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace IconEditSvg
{

    public enum ParamIndex { 
        C_StartControlPoint=0,
        C_EndControlPoint=1,
        C_EndPoint=2,};
    public enum KeyCommand { Non, Up, Down, Left, Right, Del, Esc, Tab, PageUp, PageDown, Ins, Home, End };
    public enum MouseEventKind { Press, Move, Release, Double, Non };
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
        public int Width;
        public int Height;
        public float Scale;
        public float OffsetX;
        public float OffsetY;
        public SvgPathData TargetPathData;

        public ViewInfo()
        {
            Width = 80;
            Height = 80;
            Scale = 6;

            HoverIndex = new SvgPathData.SvgPathIndex();
            PressIndex = null;
            MoveUnit = MainPage.MoveUnitDef.few;

        }

        public string FolderPath { get; internal set; }
        public string FileName { get; internal set; }
        public SvgEditData TargetItem { get; internal set; }
        public SvgPathData.SvgPathIndex HoverIndex { get; internal set; }
        public SvgPathData.SvgPathIndex PressIndex { get; internal set; }
        public MainPage.MoveUnitDef MoveUnit { get; internal set; }
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
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        const string key_folder = "Folder";
        const string key_file = "File";


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private void SetProperty<T>(ref T storage, T value,
                                [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value))
                return;
            storage = value;
            OnPropertyChanged(propertyName);
        }

        private ObservableCollection<Item> DataSource = new ObservableCollection<Item>();


        internal void FocusMove()
        {
            _ = FocusManager.TryFocusAsync(Edit_ScrollViewer, FocusState.Programmatic);
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
        public enum MoveUnitDef
        {
            few,
            normal,
            rough,

        }

        public Dictionary<MoveUnitDef, string> MoveUnitDictionary { get; } = new Dictionary<MoveUnitDef, string>();
        public MoveUnitDef MoveUnit {
            get { return m_viewInfo.MoveUnit; }
            set {
                m_viewInfo.MoveUnit = value;
            }
        }



        public enum PolygonUnit
        {
            none = 0,
            unit1,
            unit2,
            unit3,
            unit4,
            RulerOrigin,
            Symmetry,
        }
        public Dictionary<PolygonUnit, string> PolygonUnitDictionary { get; }
     = new Dictionary<PolygonUnit, string>();

        // 画面とバインドしたい列挙型のプロパティ
        private PolygonUnit _polygonUnitValue = PolygonUnit.unit4;
        public PolygonUnit PolygonUnitValue
        {
            get => _polygonUnitValue;
            set
            {
                SetProperty(ref _polygonUnitValue, value);
                if (_polygonUnitValue == PolygonUnit.Symmetry)
                {
                    if (m_viewInfo.TargetPathData != null)
                    {
                        _ = MessageYesNoAsync("中心線を選択された要素で再計算しますか？", (bool res) =>
                        {
                            InitializeRulerValue(false);

                            if (res)
                            {
                                m_viewInfo.TargetPathData.CalcReferenceLine(ref RulerStartPoint, ref RulerEndPoint);
                            }
                            m_viewInfo.TargetPathData.RulerShow(RulerStartPoint, RulerEndPoint);
                            MainCanvas.Invalidate();
                        });
                    }
                    else
                    {
                        InitializeRulerValue(false);
                        m_viewInfo.TargetPathData = new SvgPathData(null, PolygonUnit.none);
                        m_viewInfo.TargetPathData.RulerShow(RulerStartPoint, RulerEndPoint);
                        MainCanvas.Invalidate();
                    }
                }
                else
                {
                    if (m_viewInfo.TargetPathData != null)
                    {
                        m_viewInfo.TargetPathData.RulerHide();
                    }

                    MainCanvas.Invalidate();
                }
            }
        }

        bool RulerInitialize;

        void InitializeRulerValue(bool forcibly)
        {
            if (!RulerInitialize || forcibly)
            {
                RulerInitialize = true;
                RulerStartPoint = new Vector2(m_viewInfo.Width * 1 / 4, m_viewInfo.Height * 1 / 4);
                RulerEndPoint = new Vector2(m_viewInfo.Width * 3 / 4, m_viewInfo.Height * 3 / 4);
            }
        }

        Vector2 RulerStartPoint;
        Vector2 RulerEndPoint;

        bool _rulerVisible;
#if false
        bool RulerVisible
        {
            get { return _rulerVisible; }
            set
            {
                _rulerVisible = value;
                if (_rulerVisible)
                {
                    InitializeRulerValue(false);
                    if (m_viewInfo.TargetPathData == null)
                    {
                        m_viewInfo.TargetPathData = new SvgPathData(null, PolygonUnit.none);
                    }
                    m_viewInfo.TargetPathData.RulerShow(RulerStartPoint, RulerEndPoint);
                    MainCanvas.Invalidate();
                }
                else
                {
                    if (m_viewInfo.TargetPathData != null)
                    {
                        m_viewInfo.TargetPathData.RulerHide();
                        MainCanvas.Invalidate();
                    }
                }
            }
        }
#endif
        private bool _drawMode = false;
        private bool DrawMode
        {
            get { return _drawMode; }
            set
            {
                _drawMode = value;
                EditCanvas.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                OnPropertyChanged("DrawMode");
                if (_drawMode)
                {
                    //EditCanvas.Focus(FocusState.Keyboard);
                    //var x =  FocusManager.GetFocusedElement();
                    _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, (Windows.UI.Core.DispatchedHandler)(async () =>
                    {
                        var result = await FocusManager.TryFocusAsync(EditCanvas, FocusState.Keyboard);
                        if (result.Succeeded) System.Diagnostics.Debug.WriteLine("フォーカス成功");
                        else Debug.WriteLine("フォーカス失敗");
                    }));


                    if (m_viewInfo.TargetPathData != null)
                    {
                        _rulerVisible = false;
                        //                        OnPropertyChanged("RulerVisible");
                        // まぁ選択状態解除みたいな。
                        m_viewInfo.TargetPathData.RulerHide();
                        m_viewInfo.TargetPathData = null;
                        MainCanvas.Invalidate();
                    }
                    if (_makeLineDrawing != null)
                    {
                        _makeLineDrawing.Reset();
                    }
                }
                else
                {
                }
            }
        }

        private Command _menuCommand;

        string svgdata;
        int svgWidth = 40;
        int svgHeight = 40;
        int org40Width = 40;
        int org40Height = 40;
        byte[] folder40bytes;
        byte[] OrgImageBytes;
        byte[] OrgImage2xBytes;

        double _opacityValue = 50;


        ViewInfo m_viewInfo;
        public ViewInfo Info { get { return m_viewInfo; } }
        XmlDocument m_svgXmlDoc;
        private const string Unit4Text = "反復単位(4)";
        private const string RulerOriginText = "ルーラー原点基準";
        private const string SymmetryText = "線対称";

        public MainPage()
        {
            PolygonUnitDictionary.Add(PolygonUnit.none, "非多角形");
            PolygonUnitDictionary.Add(PolygonUnit.unit1, "反復単位(1)");
            PolygonUnitDictionary.Add(PolygonUnit.unit2, "反復単位(2)");
            PolygonUnitDictionary.Add(PolygonUnit.unit3, "反復単位(3)");
            PolygonUnitDictionary.Add(PolygonUnit.unit4, Unit4Text);
            PolygonUnitDictionary.Add(PolygonUnit.RulerOrigin, RulerOriginText);
            PolygonUnitDictionary.Add(PolygonUnit.Symmetry, SymmetryText);

            MoveUnitDictionary.Add(MoveUnitDef.few, "移動単位小( 0.1 px )");
            MoveUnitDictionary.Add(MoveUnitDef.normal, "移動単位中( 1 px )");
            MoveUnitDictionary.Add(MoveUnitDef.rough, "移動単位大( 5 px )");

            DataContext = this;
            this.InitializeComponent();
            //Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += OnAcceleratorKeyActivated;
            Window.Current.CoreWindow.PointerWheelChanged += CoreWindow_PointerWheelChanged;


            this.Loaded += MainPage_Loaded;
            //var task = testAsync();
            SetupCommand();

            m_viewInfo = new ViewInfo();


            OpacitySlider.Value = _opacityValue;

            InitializeTreeView();

            var appView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            appView.Title = "Assets/folder.svg";
            TargetFolder.Text = "Assets/folder.svg";


            this.ViewModel = new RecordingViewModel();

            ApplicationDataContainer container = ApplicationData.Current.LocalSettings;

            string path = container.Values["Folder"] as string;
            if (path != null)
            {
                appView.Title = path;
                TargetFolder.Text = path;
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
            string filepath = container.Values["File"] as string;
            if (filepath != null)
            {
                SelectPngFile(filepath);
            }
        }

        DateTime zoomtime=DateTime.Now;

        private void CoreWindow_PointerWheelChanged(CoreWindow sender, PointerEventArgs args)
        {
            if (sender.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
            {
                DateTime dt = DateTime.Now;
                TimeSpan span = dt - zoomtime;
                if (span.TotalMilliseconds < 100)
                    return;
                zoomtime = dt;
                args.Handled = true;
                var delta = args.CurrentPoint.Properties.MouseWheelDelta;

                if (delta > 0)
                {
                    m_viewInfo.Scale -= 2;
                    if (m_viewInfo.Scale < 6)
                    {
                        m_viewInfo.Scale = 6;
                    }
                    InvalidateAllCanvas();
                }
                else if (delta < 0)
                {
                    m_viewInfo.Scale += 2;
                    InvalidateAllCanvas();
                }

            }
        }

        const int KEYMODIFIER_ALT = 1;
        const int KEYMODIFIER_CONTROL = 2;
        const int KEYMODIFIER_SHIFT = 4;
        public static int GetKeyModifier()
        {
            int modifier = 0;
            var coreWindow = Window.Current.CoreWindow;
            if (coreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down))
                modifier |= KEYMODIFIER_ALT;
            if (coreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                modifier |= KEYMODIFIER_CONTROL;
            if (coreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                modifier |= KEYMODIFIER_SHIFT;
            if (coreWindow.GetKeyState(VirtualKey.LeftControl).HasFlag(CoreVirtualKeyStates.Down))
                modifier |= KEYMODIFIER_CONTROL;
            if (coreWindow.GetKeyState(VirtualKey.RightControl).HasFlag(CoreVirtualKeyStates.Down))
                modifier |= KEYMODIFIER_CONTROL;

            return modifier;
        }


        void OnAcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs e)
        {
            if (e.Handled)
                return;

            var focused = FocusManager.GetFocusedElement();
            if (focused is TextBox || focused is GridViewItem)
                return;

            bool handled = false;
            if (e.EventType == CoreAcceleratorKeyEventType.SystemKeyDown ||
                e.EventType == CoreAcceleratorKeyEventType.KeyDown)
            {
                KeyCommand KeyCmd = KeyCommand.Non;
                int modifier = GetKeyModifier();
                if (modifier == KEYMODIFIER_CONTROL)
                {
                    // Ctrl + ?
                    handled = true;
                    switch (e.VirtualKey)
                    {
                        case VirtualKey.Z:
                            break;
                        case VirtualKey.Y:
                            break;
                        case VirtualKey.C:
                            break;
                        case VirtualKey.V:
                            break;
                        case VirtualKey.X:
                            break;
                        case VirtualKey.A:
                            break;
                        case VirtualKey.B:
                            break;
                        case VirtualKey.I:
                            break;
                        case VirtualKey.U:
                            break;
                        default:
                            break;
                    }
                }
                else if (modifier == 0)
                {
                    switch (e.VirtualKey)
                    {
                        case VirtualKey.Delete:
                        case VirtualKey.Back:

                            break;
                        case VirtualKey.Escape:
                            KeyCmd = KeyCommand.Esc;
                            break;
                    }
                }

                switch (e.VirtualKey)
                {
                    case VirtualKey.Left:
                        KeyCmd = KeyCommand.Left;
                        break;
                    case VirtualKey.Up:
                        KeyCmd = KeyCommand.Up;
                        break;
                    case VirtualKey.Right:
                        KeyCmd = KeyCommand.Right;
                        break;
                    case VirtualKey.Down:
                        KeyCmd = KeyCommand.Down;
                        break;
                    case VirtualKey.Home:
                        KeyCmd = KeyCommand.Home;
                        break;
                    case VirtualKey.End:
                        KeyCmd = KeyCommand.End;
                        break;
                    case VirtualKey.PageUp:
                        KeyCmd = KeyCommand.PageUp;
                        break;
                    case VirtualKey.PageDown:
                        KeyCmd = KeyCommand.PageDown;
                        break;
                }
                if (KeyCmd != KeyCommand.Non)
                {
                    KeyCommandExec(KeyCmd);
                    handled = true;
                }
            }
            else if (e.EventType == CoreAcceleratorKeyEventType.KeyUp ||
                     e.EventType == CoreAcceleratorKeyEventType.SystemKeyUp)
            {

            }
            if (handled)
                e.Handled = true;
        }
        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            //Window.Current.forcus
            //this.activecon
        }

        /*
private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
{
   if (_makeLineDrawing != null)
   {
       switch (args.VirtualKey)
       {
           case Windows.System.VirtualKey.Escape:
               _makeLineDrawing.CancelEvent();
               break;
       }
   }
   else
   {
       if (Info.TargetItemIndex >= 0) {
           switch (args.VirtualKey)
           {
               case Windows.System.VirtualKey.Up:
                   break;
           }
       }
   }
}
*/
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
            PolygonUnitComboBox.SelectedIndex = 0;
            MoveUnitComboBox.SelectedIndex = 0;

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
                XmlElement root = m_svgXmlDoc.DocumentElement;



                try
                {
                    var w = root.GetAttribute("width");
                    w = w.Replace("px", "");
                    svgWidth = int.Parse(w);
                    var h = root.GetAttribute("height");
                    h = h.Replace("px", "");
                    svgHeight = int.Parse(h);
                    svgWidth /= 2;
                    svgHeight /= 2;
                }
                catch (Exception)
                {
                    svgWidth = 40;
                    svgHeight = 40;
                }                
                
                
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
                    Size size80 = new Size(svgWidth * 2, svgHeight * 2);

                    var i80 = await this.makeImageFromSvgAsync(size80,size80);
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
        /// ファイル変更または、テキスト直接編集による変更に伴う初期化
        /// </summary>
        /// <param name="pre"></param>
        void UpdateEtc(bool pre)
        {
            if (pre)
            {
                DrawMode = false;
                if (m_viewInfo.TargetPathData != null)
                {
                    _rulerVisible = false;
                    //                    OnPropertyChanged("RulerVisible");
                    m_viewInfo.TargetPathData.RulerHide();
                }
                m_viewInfo.TargetPathData = null;
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

        void CalcViewPos()
        {
            double aw = Edit_ScrollViewer.ActualWidth;
            double ah = Edit_ScrollViewer.ActualHeight;

            float w = m_viewInfo.Width * m_viewInfo.Scale;
            float h = m_viewInfo.Height * m_viewInfo.Scale;
            if (w + 100 > aw)
            {
            }
            else
            {
                int x = (int)((aw - w) / 2);
                m_viewInfo.OffsetX = x;
            }
            if (h + 100 > ah)
            {
            }
            else
            {
                int y = (int)((Edit_ScrollViewer.ActualHeight - h) / 2);
                m_viewInfo.OffsetY = y;
            }
        }

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
            CalcViewPos();
            Debug.WriteLine("offsetx {0:0.0f} scale:{1:0.0f}", m_viewInfo.OffsetX, m_viewInfo.Scale);
            float w = m_viewInfo.Width * m_viewInfo.Scale;
            float h = m_viewInfo.Height * m_viewInfo.Scale;
            int x = (int)m_viewInfo.OffsetX;
            int y = (int)m_viewInfo.OffsetY;
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
            var x = m_viewInfo.OffsetX;
            var y = m_viewInfo.OffsetY;
            if (m_ref80 && PngFile200!=null && OrgImage2xBytes != null) //if (PngFile200 != null)
            {
#if false
                args.DrawingSession.Transform = new Matrix3x2(m_viewInfo.Scale, 0, 0, m_viewInfo.Scale, x, y);
                CanvasBitmap bitmap = CanvasBitmap.CreateFromBytes(sender, PngFile200.PixelBuffer.ToArray(), 80, 80, Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized);
                args.DrawingSession.DrawImage(bitmap);
#else
                float w = m_viewInfo.Scale;

                int cl = m_viewInfo.Width;
                for (int row = 0; row < m_viewInfo.Height; row++)
                {
                    for (int col = 0; col < cl; col++)
                    {
                        int index = (row * cl + col) * 4;
                        if (index >= OrgImage2xBytes.Length) break;

                        byte r = OrgImage2xBytes[index + 2];
                        byte g = OrgImage2xBytes[index + 1];
                        byte b = OrgImage2xBytes[index + 0];
                        byte a = OrgImage2xBytes[index + 3];
                        if (a > 0)
                        {
                            r = (byte)(r * 255 / a);
                            g = (byte)(g * 255 / a);
                            b = (byte)(b * 255 / a);
                        }
                        //                        a = (byte)((float)a * _opacityValue / 100);
                        Rect rect = new Rect(x + col * w, y + row * w, w, w);
                        args.DrawingSession.FillRectangle(rect, Color.FromArgb(a, r, g, b));
                    }
                }

#endif

            }
            else if (OrgImageBytes != null)
            {

                float w = m_viewInfo.Scale * 2;

                int cl = (int)org40Width;
                for (int row = 0; row < org40Height; row++)
                {
                    for (int col = 0; col < cl; col++)
                    {
                        int index = (row * cl + col) * 4;
                        if (index >= OrgImageBytes.Length) break;

                        byte r = OrgImageBytes[index + 2];
                        byte g = OrgImageBytes[index + 1];
                        byte b = OrgImageBytes[index + 0];
                        byte a = OrgImageBytes[index + 3];
                        if (a > 0)
                        {
                            r = (byte)(r * 255 / a);
                            g = (byte)(g * 255 / a);
                            b = (byte)(b * 255 / a);
                        }
                        //                        a = (byte)((float)a * _opacityValue / 100);
                        Rect rect = new Rect(x + col * w, y + row * w, w, w);
                        args.DrawingSession.FillRectangle(rect, Color.FromArgb(a, r, g, b));
                    }
                }
            }
        }
        private void PrevCanvas_Draw(CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            args.DrawingSession.DrawEllipse(0, 0, 80, 30, Colors.Red, 3);
            args.DrawingSession.DrawText("Hello, world!", 100, 100, Colors.Yellow);

            var des = new CompositeEffect();
            //des.Mode = CanvasComposite.SourceAtop;
            //des.Mode = CanvasComposite.Xor;
            des.Mode = CanvasComposite.SourceIn;
            des.Sources.Add(getImage1(sender));
            des.Sources.Add(getImage2(sender));

            args.DrawingSession.DrawImage(des);
            /*
            float w = m_viewInfo.Width * m_viewInfo.Scale;
            float h = m_viewInfo.Height * m_viewInfo.Scale;
            int x = (int)((sender.ActualWidth - w) / 2);
            int y = (int)((sender.ActualHeight - h) / 2);
            m_viewInfo.OffsetX = x;
            m_viewInfo.OffsetY = y;
            */
            //
            if (svgdata != null)
            {
                args.DrawingSession.Transform = new Matrix3x2(m_viewInfo.Scale, 0, 0, m_viewInfo.Scale, m_viewInfo.OffsetX, m_viewInfo.OffsetY);
                try
                {
                    var doc = CanvasSvgDocument.LoadFromXml(sender, svgdata);
                    if (doc != null)
                    {
                        SvgDocOrg = doc;
                    }
                    if (SvgDocOrg != null)
                    {

                        args.DrawingSession.DrawSvg(SvgDocOrg, new Size(m_viewInfo.Width, m_viewInfo.Height), 0, 0);
                    }
                }
                catch
                {
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
            args.DrawingSession.DrawEllipse(0, 0, 83, 33, Colors.Aqua, 3);
            if (m_viewInfo.TargetPathData != null && m_viewInfo.TargetPathData.IsExists())
            {
                // 選択されているパスとアンカーの表示
                args.DrawingSession.Transform = new Matrix3x2(1, 0, 0, 1, m_viewInfo.OffsetX, m_viewInfo.OffsetY);
                m_viewInfo.TargetPathData.DrawCurrentSelectPath(args.DrawingSession, m_viewInfo);


                var it = m_viewInfo.TargetPathData.GetEnumerator() as ItemEnumerator;
                while (it.MoveNext())
                {
                    SvgPathItem item = it.Current as SvgPathItem;
                    item.DrawAnchor(args.DrawingSession, m_viewInfo, it.GetPathIndex(0));

                }
                if (PolygonUnitValue != PolygonUnit.none)
                {
                    m_viewInfo.TargetPathData.DrawPolygonCenter(m_viewInfo, args.DrawingSession, PolygonUnitValue);
                }
            }
        }

        private void EditCanvas_Draw(CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            if (_makeLineDrawing != null)
                _makeLineDrawing.Draw(args.DrawingSession, m_viewInfo);
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
        //        private List<SvgPathItem> m_path;
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
                int cl = org40Width;
                for (int row = 0; row < org40Height; row++)
                {
                    for (int col = 0; col < cl; col++)
                    {
                        int index = (row * cl + col) * 4;
                        if (index >= OrgImageBytes.Length) break;

                        byte a = OrgImageBytes[index + 3];
                        

                        byte r = OrgImageBytes[index + 2];
                        byte g = OrgImageBytes[index + 1];
                        byte b = OrgImageBytes[index + 0];
                        if (a > 0) {
                            r = (byte)(r * 255 / a);
                            g = (byte)(g * 255 / a);
                            b = (byte)(b * 255 / a);
                        }
                        a = (byte)((float)a * _opacityValue / 100);
                        Rect rect = new Rect(col * 8, row * 8, 8, 8);
                        args.DrawingSession.FillRectangle(rect, Color.FromArgb(a, r, g, b));
                    }
                }
            }

            if (folder40bytes != null)
            {
                int cl = svgWidth;
                for (int row = 0; row < svgHeight; row++)
                {
                    for (int col = 0; col < cl; col++)
                    {
                        int index = (row * cl + col) * 4;
                        if (index >= folder40bytes.Length) break;

                        byte r = folder40bytes[index + 2];
                        byte g = folder40bytes[index + 1];
                        byte b = folder40bytes[index + 0];
                        byte a = folder40bytes[index + 3];
                        if (a > 0)
                        {
                            r = (byte)(r * 255 / a);
                            g = (byte)(g * 255 / a);
                            b = (byte)(b * 255 / a);
                        }
                        a = (byte)((float)a * (100 - _opacityValue) / 100);
                        Rect rect = new Rect(col * 8, row * 8, 8, 8);
                        args.DrawingSession.FillRectangle(rect, Color.FromArgb(a, r, g, b));
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

            Size targetSize = new Size(svgWidth, svgHeight);
            Size orgSize = new Size(svgWidth*2, svgHeight*2);



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
#if true
                writeableBitmap = new WriteableBitmap((int)targetSize.Width, (int)targetSize.Height);
                await writeableBitmap.SetSourceAsync(stream);
#else
                stream.Seek(0);
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                PixelDataProvider pix = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Straight,
                new BitmapTransform(),
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.DoNotColorManage);

                byte[] bitmapbytes = pix.DetachPixelData();
                writeableBitmap = new WriteableBitmap((int)decoder.OrientedPixelWidth, (int)decoder.OrientedPixelHeight);
//                Stream pixStream = writeableBitmap.PixelBuffer.AsStream();
//                pixStream.Write(bitmapbytes, 0, (int)(decoder.OrientedPixelWidth * decoder.OrientedPixelHeight * 4));
#endif

            }

            if (writeableBitmap == null) return null;
            folder40bytes = writeableBitmap.PixelBuffer.ToArray();

            return writeableBitmap;
        }


        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateEtc(true);
            svgdata = svgText.Text;
            UpdateSvgByText();
        }

        void UpdateSvgByText()
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
            UpdateSvg(success);
        }

        void UpdateSvg(bool success)
        {
            //MainCanvas.Invalidate();
            PrevCanvas.Invalidate();
            _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, (Windows.UI.Core.DispatchedHandler)(async () =>
            {
                WriteableBitmap i40 = null;
                WriteableBitmap i80 = null;
                if (success)
                {
                    i40 = await SampleAsync();
                    var size = new Size(svgWidth * 2, svgHeight * 2);
                    i80 = await this.makeImageFromSvgAsync(size,size);
                }
                Magnification.Invalidate();
                Image40.Source = i40;
                Image80.Source = i80;
                Image40.Width = svgWidth;
                Image40.Height = svgHeight;
                Image80.Width = svgWidth * 2;
                Image80.Height = svgHeight * 2;

            }));
        }

        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            filePicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            filePicker.FileTypeFilter.Add(".png");
            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                try
                {
                    SelectPngFile(file.Path);
                }
                catch (Exception ex)
                {

                    CmUtils.DebugWriteLine(ex.ToString());
                }

            }
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
            SvgEditData folder = null;

            if (node.Content is SvgEditData && node.HasUnrealizedChildren == true)
            {
                folder = node.Content as SvgEditData;
            }
            else
            {
                return;
            }

            IReadOnlyList<SvgEditData> itemsList = folder.GetItems();

            if (itemsList.Count == 0)
            {
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

            // Check for input device
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                var properties = e.GetCurrentPoint(MainCanvas).Properties;
                if (properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased && Info?.TargetPathData!=null)
                {
                    var selectedItem = Info.TargetPathData.GetSelectedItem();
                    if (selectedItem != null)
                    {

                        var menu = new PopupMenu();
                        if (selectedItem.IsL())
                        {
                            menu.Commands.Add(new UICommand("ベジェに変換", (cmd) =>
                            {
                                if (Info.TargetPathData.ConvertToCurve())
                                {
                                    AllRelatedDataUpdate();
                                }
                            }));
                        }
                        menu.Commands.Add(new UICommand("角丸め", (cmd) =>
                        {
                            // クリックされたときに実行したい処理
                            if (m_viewInfo.TargetPathData.InsRoundCorner(_radius))
                            {
                                AllRelatedDataUpdate();
                            }
                        }));
                        menu.Commands.Add(new UICommand("削除", (cmd) =>
                        {
                        }));
                        //await menu.ShowForSelectionAsync(GetElementRect(element));
                        var pos = e.GetCurrentPoint(MainCanvas).Position;
                        var pos0 = GetElementRect(MainCanvas);
                        pos.X += pos0.X;
                        pos.Y += pos0.Y;

                        _ = menu.ShowForSelectionAsync(new Rect(pos.X, pos.Y, 4, 4));
                    }

                }
            }
        }

        bool moved = false;

        private void MainCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (m_viewInfo.PressIndex != null)
            {
                if (m_viewInfo.PressIndex.IsValid())
                {
                    var ptrPt = e.GetCurrentPoint(MainCanvas);// MainCanvas の座標に変換
                    var pos = ptrPt.Position;
                    pos.X -= m_viewInfo.OffsetX;
                    pos.Y -= m_viewInfo.OffsetY;
                    Vector2 v = new Vector2((float)pos.X / Info.Scale, (float)pos.Y / Info.Scale);
                    if (moved || CmUtils.Length(MousePressPoint, new Vector2((float)pos.X, (float)pos.Y)) > 5) {
                        if (Info.TargetPathData.MovePos(Info.PressIndex, v))
                        {
                            moved = true;
                            AllRelatedDataUpdate();
                        }
                    }
                }
            }
            else
            {
                findItem(e, MouseEventKind.Move);
            }
        }

        Vector2 MousePressPoint;


        void findItem(PointerRoutedEventArgs e, MouseEventKind kind)
        {
            var ptr = e.Pointer;
            if (ptr.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                var properties = e.GetCurrentPoint(MainCanvas).Properties;
                if (properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
                {
                    return;
                }
                bool invalidate = false;
                var ptrPt = e.GetCurrentPoint(MainCanvas);// MainCanvas の座標に変換
                if (m_viewInfo.TargetPathData != null)
                {
                    // パスが選択されていたら
                    int partindex = -1;
                    var pos = ptrPt.Position;
                    pos.X -= m_viewInfo.OffsetX;
                    pos.Y -= m_viewInfo.OffsetY;
                    SvgPathData.SvgPathIndex mouseIndex = new SvgPathData.SvgPathIndex();
                    //Debug.WriteLine("x;{0} y:{1}",pos.X,pos.Y);
                    var it = m_viewInfo.TargetPathData.GetEnumerator() as ItemEnumerator;
                    while (it.MoveNext())
                    {
                        var item = it.Current as SvgPathItem;
                        //   item.DrawAnchor(args.DrawingSession, viewInfo.Scale);
                        partindex = item.HitTest(pos, m_viewInfo.Scale);
                        if (partindex >= 0)
                        {
                            mouseIndex = it.GetPathIndex(partindex);
                            break;
                        }

                    }
                    switch (kind)
                    {
                        case MouseEventKind.Move:
                            {
                                if (m_viewInfo.HoverIndex.IsValid() || mouseIndex.IsValid())
                                {
                                    m_viewInfo.HoverIndex = mouseIndex;
                                    MainCanvas.Invalidate();
                                    invalidate = true;
                                }
                                break;
                            }
                        case MouseEventKind.Press:
                            {
                                if (ptrPt.Properties.IsLeftButtonPressed)
                                {
                                    MousePressPoint = new Vector2((float)pos.X, (float)pos.Y);
                                    moved = false;
                                    Edit_ScrollViewer.Focus(FocusState.Programmatic);
                                    if (m_viewInfo.HoverIndex.IsValid())
                                    {
                                        m_viewInfo.PressIndex = new SvgPathData.SvgPathIndex(m_viewInfo.HoverIndex);
                                    }
                                    else
                                    {
                                        m_viewInfo.PressIndex = new SvgPathData.SvgPathIndex();
                                    }
                                }
                                break;
                            }
                        case MouseEventKind.Release:
                            {
                                if (!moved && m_viewInfo.PressIndex != null && m_viewInfo.PressIndex.IsValid() && m_viewInfo.HoverIndex == m_viewInfo.PressIndex)
                                {
                                    Info.TargetPathData.SelectHandle(m_viewInfo.PressIndex);
                                    MainCanvas.Invalidate();
                                    invalidate = true;
                                }
                                else if (Info.TargetPathData.IsSelectHandle())
                                {
                                    Info.TargetPathData.DeSelected();
                                    MainCanvas.Invalidate();
                                    invalidate = true;
                                }
                                m_viewInfo.PressIndex = null;

                                UpdateCordinateInfo();
                                EditCanvas.Focus(FocusState.Keyboard);
                                break;
                            }
                    }
                    if (!invalidate) { 
                        // 線上チェック
                    }

                }
            }
            e.Handled = true;
        }


        /// <summary>
        /// 選択ハンドルの座標表示
        /// </summary>
        private void UpdateCordinateInfo()
        {
            m_editTargetPos.Text = m_viewInfo.TargetPathData.GetInfo(PolygonUnitValue);


        }


        void AllRelatedDataUpdate()
        {
            List<SvgPathItem> m_path = Info.TargetPathData.GetPathList();
            if (m_path != null && m_path.Count > 0)
            {
                m_viewInfo.TargetPathData.GetRulerPos(ref RulerStartPoint, ref RulerEndPoint);
                m_viewInfo.TargetItem.UpdateElement(m_path);
                svgdata = m_svgXmlDoc.GetXml();
                svgText.Text = svgdata;
                updateTree();
                UpdateSvg(true);
            }
            else
            {
                // 多分ルーラー
                m_viewInfo.TargetPathData.GetRulerPos(ref RulerStartPoint, ref RulerEndPoint);
            }
            MainCanvas.Invalidate();
            UpdateCordinateInfo();

        }


        void PointChange(KeyCommand keyCommand)
        {
            if (Info.TargetPathData.PointChange(keyCommand, PolygonUnitValue, Info))
            {
                AllRelatedDataUpdate();
            }

        }
        /// <summary>
        /// 角丸め
        /// </summary>
        /// <param name="step"></param>
        void RoundCorner(float step)
        {
            if (Info.TargetPathData.RoundCorner(step))
            {
                AllRelatedDataUpdate();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        void MovePath(float x, float y)
        {
            if (Info.TargetPathData.MovePath(x, y))
            {
                AllRelatedDataUpdate();
            }
        }

        void ResizePath(float ratio)
        {
            if (Info.TargetPathData.ResizePath(ratio))
            {
                AllRelatedDataUpdate();
            }
        }

        void NextHandle(bool IsShift)
        {
            if (Info.TargetPathData.NextHandle(IsShift))
            {
                MainCanvas.Invalidate();
                UpdateCordinateInfo();
            }
        }

        void NextItem(bool IsShift)
        {
            Info.TargetPathData.NextItem(IsShift);
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
            if (_makeLineDrawing != null)
            {
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
            pos.X -= m_viewInfo.OffsetX;
            pos.Y -= m_viewInfo.OffsetY;
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
                    pos.X -= m_viewInfo.OffsetX;
                    pos.Y -= m_viewInfo.OffsetY;
                    if (_makeLineDrawing == null)
                    {
                        _makeLineDrawing = new MakeLineDrawing(this);
                    }
                    _makeLineDrawing.PointerEvent(kind, new Vector2((float)pos.X, (float)pos.Y));
                }
            }
        }
        /*
        private void EditCanvas_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Escape:
                    break;
            }
        }
        */
        private void EditCanvas_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (Info.TargetPathData != null)
            {
                if (Info.TargetPathData.IsSelectHandle())
                {
                    if (PolygonUnitValue != PolygonUnit.none)
                    {
                        switch (e.Key)
                        {
                            case Windows.System.VirtualKey.Tab:
                                NextHandle(IsShiftKeyPressed);
                                e.Handled = true;
                                break;
                        }
                    }
                    else
                    {
                        switch (e.Key)
                        {
                            case Windows.System.VirtualKey.Tab:
                                NextHandle(IsShiftKeyPressed);
                                e.Handled = true;
                                break;
                        }
                    }

                }
                else
                {
                }
            }
            else if (_makeLineDrawing != null)
            {
            }

        }

        void KeyCommandExec(KeyCommand keyCmd)
        {
            if (Info.TargetPathData != null)
            {
                if (Info.TargetPathData.IsSelectHandle())
                {
                    if (!IsControlKeyPressed)
                    {
                        switch (keyCmd)
                        {
                            case KeyCommand.Home:
                            case KeyCommand.End:
                            case KeyCommand.PageUp:
                            case KeyCommand.PageDown:
                            case KeyCommand.Up:
                            case KeyCommand.Down:
                            case KeyCommand.Left:
                            case KeyCommand.Right:
                                PointChange(keyCmd);
                                break;
                            case KeyCommand.Tab:
                                NextHandle(IsShiftKeyPressed);
                                break;
                        }
                    }
                    else
                    {
                        float s = IsShiftKeyPressed ? 0.5f : 5f;
                        switch (keyCmd)
                        {
                            case KeyCommand.Up:
                                MovePath(0, -s);
                                break;
                            case KeyCommand.Down:
                                MovePath(0, s);
                                break;
                            case KeyCommand.Left:
                                MovePath(-s, 0);
                                break;
                            case KeyCommand.Right:
                                MovePath(s, 0);
                                break;
                            case KeyCommand.PageUp:
                                ResizePath(1.1f);
                                break;
                            case KeyCommand.PageDown:
                                ResizePath(0.9f);
                                break;
                            case KeyCommand.Tab:
                                break;
                        }
                    }
#if false
                    if (PolygonUnitValue != PolygonUnit.none)
                    {
                        switch (keyCmd)
                        {
                            case KeyCommand.Up:
                                PolygonChange(0.1f, 0);
                                break;
                            case KeyCommand.Down:
                                PolygonChange(-0.1f, 0);
                                break;
                            case KeyCommand.Left:
                                PolygonChange(0, -1);
                                break;
                            case KeyCommand.Right:
                                PolygonChange(0, 1);
                                break;
                            case KeyCommand.Tab:
                                NextHandle(IsShiftKeyPressed);
                                break;
                        }
                    }
                    else
                    {
                        float s = 0.1f;
                        switch (keyCmd)
                        {
                            case KeyCommand.Up:
                                ValueChange(0, -s);
                                break;
                            case KeyCommand.Down:
                                ValueChange(0, s);
                                break;
                            case KeyCommand.Left:
                                ValueChange(-s, 0);
                                break;
                            case KeyCommand.Right:
                                ValueChange(s, 0);
                                break;
                            case KeyCommand.Tab:
                                NextHandle(IsShiftKeyPressed);
                                break;
                            case KeyCommand.PageDown:
                                RoundCorner(-1);
                                break;
                            case KeyCommand.PageUp:
                                RoundCorner(1);
                                break;
                        }
                    }
#endif
                }
                else
                {
                    float s = IsShiftKeyPressed ? 0.5f : 5f;
                    switch (keyCmd)
                    {
                        case KeyCommand.Up:
                            MovePath(0, -s);
                            break;
                        case KeyCommand.Down:
                            MovePath(0, s);
                            break;
                        case KeyCommand.Left:
                            MovePath(-s, 0);
                            break;
                        case KeyCommand.Right:
                            MovePath(s, 0);
                            break;
                        case KeyCommand.PageUp:
                            ResizePath(1.1f);
                            break;
                        case KeyCommand.PageDown:
                            ResizePath(0.9f);
                            break;
                        case KeyCommand.Tab:
                            break;
                    }
                }
            }
            else if (_makeLineDrawing != null)
            {
                switch (keyCmd)
                {
                    case KeyCommand.Esc:
                        _makeLineDrawing.CancelEvent();
                        break;
                }
            }
        }

        private bool IsShiftKeyPressed
        {
            get
            {
                var state = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
                if ((state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down)
                {
                    return true;
                }
                return false;
            }
        }

        private bool IsControlKeyPressed
        {
            get
            {
                var state = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
                if ((state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down)
                {
                    return true;
                }
                return false;
            }
        }


#endregion
        private void SampleTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            var node = args.InvokedItem as TreeViewNode;

            if (node.Content is SvgEditData item)
            {
                //node.IsExpanded = !node.IsExpanded;
                m_viewInfo.TargetItem = item;
                m_viewInfo.TargetPathData = new SvgPathData(item, PolygonUnitValue);
                DrawMode = false;

                if (PolygonUnitValue == PolygonUnit.Symmetry)
                {

                    _ = MessageYesNoAsync("中心線を選択された要素で再計算しますか？", (bool res) =>
                    {
                        InitializeRulerValue(false);

                        if (res)
                        {
                            m_viewInfo.TargetPathData.CalcReferenceLine(ref RulerStartPoint, ref RulerEndPoint);
                        }
                        m_viewInfo.TargetPathData.RulerShow(RulerStartPoint, RulerEndPoint);
                        MainCanvas.Invalidate();
                    });
                }
                else
                {
                    _rulerVisible = false;
                    //                    OnPropertyChanged("RulerVisible");
                    MainCanvas.Invalidate();
                }
            }
        }

        public static async Task MessageYesNoAsync(string text, Action<bool> action)
        {
            var msg = new ContentDialog();
            msg.Content = text;
            msg.PrimaryButtonText = "はい";
            msg.SecondaryButtonText = "いいえ";
            var res = await msg.ShowAsync();
            if (action != null)
            {
                action(res == ContentDialogResult.Primary);
            }
        }

        public static async Task MessageAsync(string text)
        {
            var msg = new ContentDialog();
            msg.Content = text;
            msg.PrimaryButtonText = "OK";
            var res = await msg.ShowAsync();
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
        WriteableBitmap PngFile200;


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
                SelectPngFile(o.FullPath);
            }
        }

        private void SelectPngFile(string path)
        {
            UpdateEtc(true);
            Task.Run(async () =>
            {
                string folder = System.IO.Path.GetDirectoryName(path);
                m_viewInfo.FolderPath = folder;
                string name0 = Path.GetFileName(path);
                string name1 = null;
                if (App.ForMac)
                {
                    int num = name0.IndexOf(".png");
                    if (num > 0)
                    {
                        name1 = name0.Replace(".png", "@2x.png");
                    }
                }
                else
                {
                    int num = name0.IndexOf(".scale-100");
                    if (num > 0)
                    {
                        name1 = name0.Replace(".scale-100", ".scale-200");
                    }
                }
                try
                {
                    string[] spliststr = name0.Split('.');
                    m_viewInfo.FileName = spliststr[0];
                    var file = await CmUtils.FindFileAsync(folder, spliststr[0], "svg");
                    if (file != null)
                    {
                        XmlLoadSettings xmlLoadSettings = new XmlLoadSettings();
                        xmlLoadSettings.ElementContentWhiteSpace = false;
                        m_svgXmlDoc = await XmlDocument.LoadFromFileAsync(file, xmlLoadSettings);
                        svgdata = m_svgXmlDoc.GetXml();
                    }
                    else
                    {
                        //svgdata = @"<svg width=""80px"" height=""80px""><path d=""M51 47h21.5l5.875 7-5.875 7H51a2 2 0 0 1-2-2V49a2 2 0 0 1 2-2z"" fill=""none"" stroke-width=""1.8"" stroke=""#000""/></svg>";
                        svgdata = @"<svg version = ""1.1"" xmlns:xlink = ""http://www.w3.org/1999/xlink"" xmlns = ""http://www.w3.org/2000/svg"" width = ""80"" height = ""80"" stroke=""#000"" stroke-width=""2"" fill=""none"" stroke-linecap=""square""></svg>";

                    }

                    ApplicationDataContainer container = ApplicationData.Current.LocalSettings;
                    container.Values["File"] = path;

                }
                catch (Exception e)
                {
                    CmUtils.DebugWriteLine(e.ToString());
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
                    //appView.Title = o.FullPath;
                    appView.Title = m_viewInfo.FolderPath + " : " + m_viewInfo.FileName;
        

                    PngFile100 = null;
                    PngFile200 = null;
                    OrgImage100.Source = null;
                    OrgImage200.Source = null;
                    org40Width = 40;
                    org40Height = 40;
                    m_viewInfo.Width = 80;
                    m_viewInfo.Height = 80;

                    if (file0 != null)
                    {
                        await ReadPngFile(file0, false);
                    }
                    if (file1 != null)
                    {
                        await ReadPngFile(file1, true);
                    }
                    if (file1 == null) {
                        m_viewInfo.Width = org40Width * 2;
                        m_viewInfo.Height = org40Height * 2;
                    }
                    if (file0 == null) {
                        org40Width = m_viewInfo.Width / 2;
                        org40Height = m_viewInfo.Height / 2;
                    }
                    svgWidth = org40Width;
                    svgHeight = org40Height;
                    OrgImage100.Source = PngFile100;
                    OrgImage100.Width = org40Width;
                    OrgImage100.Height = org40Height;
                    OrgImage200.Source = PngFile200;
                    OrgImage200.Width = m_viewInfo.Width;
                    OrgImage200.Height = m_viewInfo.Height;



                    


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

                    /*
                    var i40 = await SampleAsync();
                    if (i40 != null)
                    {
                        Image40.Source = i40;
                    }
                    Size size80 = new Size(svgWidth * 2, svgHeight * 2);
                    var i80 = await this.makeImageFromSvgAsync(size80, size80);
                    if (i80 != null)
                    {
                        Image80.Source = i80;
                    }
                    */

                     
                    //Magnification.Invalidate(); UpdateSvgByText で更新されるはず。

                    InvalidateAllCanvas();

                    UpdateSvgByText();

                    updateInfo();


                }));
            });



        }

        private void updateInfo()
        {
            //TargetFolder.Text = m_viewInfo.FolderPath + " :  " + m_viewInfo.FileName + "[";
            TargetFolder.Text = string.Format("{0} : {1} [W:{2} H:{3}]", m_viewInfo.FolderPath, m_viewInfo.FileName, m_viewInfo.Width, m_viewInfo.Height);
        }

        async Task ReadPngFile(StorageFile file,bool png2x)
        {
            WriteableBitmap img;
            byte[] bitmapbytes;
#if true
            BitmapImage bitmap = new BitmapImage();
                        using (var s = await file.OpenReadAsync())
                        {
                            bitmap.SetSource(s);
                        }
//                        org40Width = bitmap.PixelWidth;
//                        org40Height = bitmap.PixelHeight;
                         img = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight);
                        using (var s = await file.OpenReadAsync())
                        {
                            img.SetSource(s);
                            bitmapbytes = img.PixelBuffer.ToArray();
                        }

#else
            using (IRandomAccessStream stream = await file.OpenReadAsync())
            {

                // Create a decoder from the stream. With the decoder, we can get 
                // the properties of the image.
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                PixelDataProvider pix = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Straight,
                new BitmapTransform(),
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.DoNotColorManage);

                bitmapbytes = pix.DetachPixelData();
                img = new WriteableBitmap((int)decoder.OrientedPixelWidth, (int)decoder.OrientedPixelHeight);
                Stream pixStream = img.PixelBuffer.AsStream();
                pixStream.Write(bitmapbytes, 0, (int)(decoder.OrientedPixelWidth * decoder.OrientedPixelHeight * 4));
            }
#endif
            if (png2x) {
                OrgImage2xBytes = bitmapbytes;
                PngFile200 = img;
                m_viewInfo.Width = img.PixelWidth;
                m_viewInfo.Height = img.PixelHeight;
            } else {
                OrgImageBytes = bitmapbytes;
                PngFile100 = img;
                org40Width = img.PixelWidth;
                org40Height = img.PixelHeight;
            }
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            _opacityValue = e.NewValue;

            OrgImage100.Opacity = _opacityValue / 100;
            OrgImage200.Opacity = _opacityValue / 100;
            RefCanvas.Opacity = _opacityValue / 100;
            

            var o = 100 - _opacityValue;

            Image40.Opacity = o / 100;
            Image80.Opacity = o / 100;
            PrevCanvas.Opacity = o / 100;

            Magnification.Invalidate();
        }

        internal void EditCanvasInvalidate()
        {
            EditCanvas.Invalidate();
        }



        /// <summary>
        /// パスの作成
        /// </summary>
        /// <param name="points"></param>
        internal void CreatePath(List<DrawingPoint> points, bool close)
        {
            if (points == null || points.Count < 1) return;
            string path = "";
            DrawingPoint befor = null;
            for (int ix = 0; ix < points.Count; ix++)
            {
                var p = points[ix];
                var pt = p.getPoint();
                if (ix == 0)
                {
                    path = "M " + v2s(pt);
                }
                else if (!p.IsHaveControlPoint && !befor.IsHaveControlPoint)
                {
                    path = path + " L " + v2s(pt);
                }
                else
                {
                    var b = befor.getControlPoint(false);
                    var c = p.getControlPoint(true);
                    path = path + " C " + v2s(b) + v2s(c) + v2s(pt);
                }

                befor = p;
                //path = path+p.
            }
            if (close)
            {
                path = path + "z";
            }
            else
            {
            }
            System.Diagnostics.Debug.WriteLine(path);
            //var childElement = m_svgXmlDoc.CreateElement("path");
            var childElement = m_svgXmlDoc.CreateElementNS("http://www.w3.org/2000/svg", "path");
            childElement.SetAttribute("d", path);
//            childElement.SetAttribute("stroke", "#000");
//            childElement.SetAttribute("stroke-width", "2");
//            childElement.SetAttribute("fill", "none");


            var root = m_svgXmlDoc.DocumentElement;
            root.AppendChild(childElement);
            // 画面更新
            svgdata = m_svgXmlDoc.GetXml();
            svgText.Text = svgdata;
            updateTree();
            UpdateSvg(true);
        }


        string v2s(Vector2 v)
        {
            float x = (v.X) / m_viewInfo.Scale;
            float y = (v.Y) / m_viewInfo.Scale;
            return string.Format("{0:0.00} {1:0.00} ", x, y);
        }


        private void AppBarSaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_svgXmlDoc != null)
            {
                if (m_viewInfo.FolderPath == null || m_viewInfo.FileName == null) return;
                Task.Run(async () =>
                {
                    try
                    {
                        string filename = m_viewInfo.FileName + ".svg";
                        var file = await CmUtils.FindFileAsync(m_viewInfo.FolderPath, m_viewInfo.FileName, "svg");
                        if (file != null)
                        {
                        }
                        StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(m_viewInfo.FolderPath);
                        file = await folder.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);
                        if (file != null)
                        {
                            await m_svgXmlDoc.SaveToFileAsync(file);
                        }

                    }
                    catch (Exception)
                    {
                    }
                });

            }
        }

        private int _radius = 4;

        private async void Chamfer_R_Button_Click(object sender, RoutedEventArgs e) {
                ChamferRadiusDialog dlg = new ChamferRadiusDialog();
            dlg.Radius = _radius;

                _ = await dlg.ShowAsync();
            if (dlg.Success) {
                _radius = dlg.Radius;
                ChamferRadiusButton.Content = string.Format("半径:{0}", _radius);

            }
        }



        private void AppBarZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            m_viewInfo.Scale -= 2;
            if (m_viewInfo.Scale < 6)
            {
                m_viewInfo.Scale = 6;
            }
            InvalidateAllCanvas();
        }

        private void AppBarZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            m_viewInfo.Scale += 2;
            InvalidateAllCanvas();
        }
        /// <summary>
        /// ズーム等による変更
        /// </summary>
        void InvalidateAllCanvas()
        {
            CalcViewPos();
            var w = Math.Max(Edit_ScrollViewer.ActualWidth, m_viewInfo.Width * m_viewInfo.Scale + 100);
            var h = Math.Max(Edit_ScrollViewer.ActualHeight, m_viewInfo.Height * m_viewInfo.Scale + 100);
            EditBase_Grid.Width = w;
            EditBase_Grid.Height = h;

            MatCanvas.Invalidate();
            RefCanvas.Invalidate();
            PrevCanvas.Invalidate();
            MainCanvas.Invalidate();
            EditCanvas.Invalidate();
        }

        bool m_ref80 = false;

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            m_ref80 = !m_ref80;
            RefCanvas.Invalidate();
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
                        if (c != ' ' && c != ',')
                        {
                            if (item != null)
                            {
                                svgPathItems.Add(item);
                            }
                            if (c == 'm' || c == 'M')
                            {
                                item = null;
                            }
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

        internal void UpdateElement(List<SvgPathItem> items)
        {
            string path = "";
            foreach (var item in items)
            {
                path = path + item.Encode();
            }

            element.SetAttribute("d", path);
        }


    }

}
