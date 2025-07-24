using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace desktop_assistant
{
    public partial class MemoPage : UserControl
    {
        private bool isDragging = false;
        private Point clickPosition;
        private UIElement draggedElement;
        private Image selectedImage;
        public bool HasFocus => TextLayer.IsKeyboardFocusWithin;
        public bool HasSelectedImage => selectedImage != null;

        public MemoPage()
        {
            InitializeComponent();

            // ドラッグ＆ドロップ有効化
            TextLayer.AllowDrop = true;
            TextLayer.PreviewDrop += MemoPage_Drop;
            TextLayer.PreviewDragOver += MemoPage_DragOver;
            TextLayer.PreviewMouseDown += (s, e) => DeselectImage();
        }

        private void MemoPage_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        public void DeleteSelectedImage()
        {
            if (selectedImage != null)
            {
                ImageLayer.Children.Remove(selectedImage);
                selectedImage = null;
            }
        }

        private void MemoPage_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    AddImageOnCanvas(file);
                }
            }
        }

        /// <summary>
        /// 画像を Canvas 上に配置する
        /// </summary>
        private void AddImageOnCanvas(string path)
        {
            var img = new Image
            {
                Source = new BitmapImage(new Uri(path)),
                Width = 100,
                Height = 100,
                Cursor = Cursors.Hand
            };

            Canvas.SetLeft(img, 50);
            Canvas.SetTop(img, 50);

            img.MouseLeftButtonDown += Img_MouseLeftButtonDown;
            img.MouseMove += Img_MouseMove;
            img.MouseLeftButtonUp += Img_MouseLeftButtonUp;
            img.IsHitTestVisible = true;

            ImageLayer.Children.Add(img);
        }

        private void Img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var img = sender as Image;

            // 前の選択を解除
            if (selectedImage != null)
            {
                selectedImage.Effect = null;
            }

            // 今回の画像を選択
            selectedImage = img;

            // 青い枠っぽい効果をつける
            var effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Blue,
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 1
            };

            // Ctrl + マウスホイールでサイズ変更
            selectedImage.PreviewMouseWheel += selectedImage_PreviewMouseWheel;
            selectedImage.PreviewKeyDown += selectedImage_PreviewKeyDown;

            img.Effect = effect;

            // このままドラッグ開始もOK
            isDragging = true;
            draggedElement = img;

            Point mousePos = e.GetPosition(ImageLayer);
            double left = Canvas.GetLeft(img);
            double top = Canvas.GetTop(img);

            clickPosition = new Point(mousePos.X - left, mousePos.Y - top);

            img.CaptureMouse();
        }

        public void DeselectImage()
        {
            if (selectedImage != null)
            {
                selectedImage.Effect = null;
                selectedImage = null;
            }
        }

        private void Img_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && draggedElement != null)
            {
                var img = draggedElement as Image;
                if (img == null) return;

                Point currentPosition = e.GetPosition(ImageLayer);

                double newLeft = currentPosition.X - clickPosition.X;
                double newTop = currentPosition.Y - clickPosition.Y;

                // テキストボックスの表示領域の矩形を取得
                Rect bounds = GetTextLayerContentBounds();

                double minX = bounds.Left;
                double minY = bounds.Top;
                double maxX = bounds.Right - img.ActualWidth;
                double maxY = bounds.Bottom - img.ActualHeight;

                newLeft = Math.Max(minX, Math.Min(newLeft, maxX));
                newTop = Math.Max(minY, Math.Min(newTop, maxY));

                Canvas.SetLeft(img, newLeft);
                Canvas.SetTop(img, newTop);
            }
        }

        private Rect GetTextLayerContentBounds()
        {
            var scrollViewer = FindScrollViewer(TextLayer);

            // TextLayer の左上座標を ImageLayer の座標系で取得
            var textTopLeft = TextLayer.TranslatePoint(new Point(0, 0), ImageLayer);

            double minX = textTopLeft.X + TextLayer.Padding.Left;
            double minY = textTopLeft.Y + TextLayer.Padding.Top;

            double viewWidth = scrollViewer?.ViewportWidth ?? TextLayer.ActualWidth;
            double viewHeight = scrollViewer?.ViewportHeight ?? TextLayer.ActualHeight;

            double maxX = minX + viewWidth - TextLayer.Padding.Right;
            double maxY = minY + viewHeight - TextLayer.Padding.Bottom;

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private ScrollViewer FindScrollViewer(DependencyObject d)
        {
            if (d is ScrollViewer sv) return sv;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var child = VisualTreeHelper.GetChild(d, i);
                var result = FindScrollViewer(child);
                if (result != null) return result;
            }

            return null;
        }

        private void Img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            if (draggedElement != null)
            {
                draggedElement.ReleaseMouseCapture();
                draggedElement = null;
            }
        }

        private void selectedImage_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (selectedImage == null) return;

            // Ctrl が押されているか確認
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                double scale = e.Delta > 0 ? 1.1 : 0.9;

                double newWidth = selectedImage.Width * scale;
                double newHeight = selectedImage.Height * scale;

                // 最小サイズの制限
                if (newWidth < 20 || newHeight < 20) return;

                selectedImage.Width = newWidth;
                selectedImage.Height = newHeight;

                e.Handled = true; // 他の処理に渡さない
            }
        }

        private void selectedImage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && selectedImage != null)
            {
                ImageLayer.Children.Remove(selectedImage);
                selectedImage = null;
                e.Handled = true;
            }
        }
    }
}
