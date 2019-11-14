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




// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace IconEditSvg
{
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


    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public struct ViewInfo
        {
            public float Width;
            public float Height;
            public float Scale;
            public float OffsetX;
            public float OffsetY;
        };

        private Command _menuCommand;

        string svgdata;
        WriteableBitmap folder40;
        byte[] folderImage;
        //Size IconInfo = new Size(80, 80);
        ViewInfo viewInfo;
        XmlDocument m_svgXmlDoc;

        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += MainPage_Loaded;
            //var task = testAsync();
            SetupCommand();

            viewInfo.Width = 80;
            viewInfo.Height = 80;
            viewInfo.Scale = 3;



        }


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
            Task.Run((Func<Task>)(async () =>{

                // Assetsからのファイル取り出し
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/folder.svg"));

#if false
                // ファイルの読み込み
                svgdata = await FileIO.ReadTextAsync(file);
#else
                m_svgXmlDoc = await XmlDocument.LoadFromFileAsync(file);
                svgdata = m_svgXmlDoc.GetXml();
#endif

                _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, (Windows.UI.Core.DispatchedHandler)(async () =>
                {

                    var i40 = await SampleAsync();
                    if (i40 != null)
                    {
                        folder40 = i40;
                        Image40.Source = i40;

                        Magnification.Invalidate();
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

            var root = m_svgXmlDoc.DocumentElement;

            TreeViewNode trvRoot = new TreeViewNode();

            MakeTree(root,ref trvRoot);
            
            SvgTreeView.RootNodes.Add(trvRoot);
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
                                attr=attr+a.GetXml();
                            }
                        }


                        trvChild.Content = xmlChild.TagName+" "+attr;
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
        private void CanvasControl_Draw(CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
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

            if (svgdata != null)
            {
                args.DrawingSession.Transform = new Matrix3x2(viewInfo.Scale, 0, 0, viewInfo.Scale, x, y);
                var doc = CanvasSvgDocument.LoadFromXml(sender, svgdata);
                if (doc != null)
                {
                    SvgDocOrg = doc;
                }
                if(SvgDocOrg!=null){
                    
                    args.DrawingSession.DrawSvg(SvgDocOrg, new Size(viewInfo.Width, viewInfo.Height), 0, 0);
                }
            }


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
            if (folderImage == null) return;

            int cl = 40;
            for (int row = 0; row < 40; row++)
            {
                for (int col = 0; col < cl; col++)
                {
                    int index = (row * cl + col) * 4;
                    if (index >= folderImage.Length) return;

                    byte r = folderImage[index + 0];
                    byte g = folderImage[index + 1];
                    byte b = folderImage[index + 2];
                    byte a = folderImage[index + 3];

                    Rect rect = new Rect(col * 8, row * 8, 8, 8);
                    args.DrawingSession.FillRectangle(rect, Color.FromArgb(a, r, g, b));

                    //Debug.WriteLine("a {0} r {1} g {2} b {3}",a,r,g,b);
                }
            }

        }

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
            folderImage = writeableBitmap.PixelBuffer.ToArray();
            return writeableBitmap;
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            svgdata = svgText.Text;

            MainCanvas.Invalidate();
            _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, (Windows.UI.Core.DispatchedHandler)(async () => {
                var i40 =  await SampleAsync();
                Magnification.Invalidate();
                Image40.Source = i40;
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
                }
                else
                {
                    TargetFolder.Text = "Operation cancelled.";
                }


            }

        }
    }
}
