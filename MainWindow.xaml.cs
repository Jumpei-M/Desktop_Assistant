using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace desktop_assistant
{
    public partial class MainWindow : Window
    {
        // ========================
        // 展開時のサイズや角丸設定
        // ========================
        private const double ExpandedWidth = 300;          // 展開時の幅
        private const double ExpandedHeight = 600;         // 展開時の高さ
        private const double ExpandedTop = 50;             // 展開時の上位置
        private const double ExpandedCornerRadius = 30;    // 展開時の角丸

        // ========================
        // 縮小時のサイズや角丸設定
        // ========================
        private const double CollapsedWidth = 40;          // 縮小時の幅
        private const double HoverWidth = 60;              // 強調時の幅（マウスオーバー時）
        private const double CollapsedHeight = 100;        // 縮小時の高さ
        private const double CollapsedTop = 150;           // 縮小時の上位置
        private const double CollapsedCornerRadius = 50;   // 縮小時の角丸

        // ========================
        // その他定数
        // ========================
        private const double InitialCornerRadius = 300;    // 初期の丸み
        private const int AnimationDurationMs = 200;       // アニメーション時間(ms)
        private const int LockDurationSec = 3;             // 展開後のロック時間(秒)
        private const int MouseCheckIntervalMs = 100;      // マウスチェック間隔(ms)

        private bool isPinned = false;                     // ピン留め中かどうか

        // ========================
        // 色設定
        // ========================
        private static readonly Color[] OriginalColors =   // 元のグラデーションカラー
        {
            Color.FromArgb(128, 0x83, 0xCE, 0xF5),         // 青
            Color.FromArgb(128, Colors.DimGray.R, Colors.DimGray.G, Colors.DimGray.B), // 灰色
            Color.FromArgb(128, 0x67, 0x67, 0x67)          // 濃い灰
        };

        private static readonly Color TargetColor =        // 展開時の単色
            Color.FromArgb(128, Colors.DimGray.R, Colors.DimGray.G, Colors.DimGray.B);

        // ========================
        // 状態管理
        // ========================
        private bool isLocked = false;                     // 展開ロック中フラグ
        private DispatcherTimer lockTimer;                 // 展開後ロック用タイマー
        private DispatcherTimer mouseCheckTimer;           // マウス監視用タイマー

        public MainWindow()
        {
            InitializeComponent();

            // ウィンドウ全体を画面いっぱいに広げる
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            Left = 0;
            Top = 0;

            // 展開後ロックタイマー設定
            lockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(LockDurationSec)
            };
            lockTimer.Tick += (s, e) =>
            {
                isLocked = false;
                lockTimer.Stop();
            };

            // マウス監視タイマー設定
            mouseCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(MouseCheckIntervalMs)
            };
            mouseCheckTimer.Tick += (s, e) =>
            {
                if (!IsMouseOver && !isLocked && !isPinned)
                {
                    if (PageContainer.Content is IFocusAware page && page.HasFocus)
                    {
                        // フォーカスが残っている場合は閉じない
                        return;
                    }

                    Collapse();
                    mouseCheckTimer.Stop();
                }
            };

            SetCollapsedPosition();  // 初期位置を設定

            // イベント設定
            ExpandPanel.MouseEnter += (s, e) => Emphasize();            // マウスオーバー
            ExpandPanel.MouseLeave += (s, e) => Deemphasize();         // マウスアウト
            ExpandPanel.MouseLeftButtonDown += (s, e) => Expand();     // クリック

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete &&
                PageContainer.Content is MemoPage memo &&
                memo.HasSelectedImage)
            {
                memo.DeleteSelectedImage();
                e.Handled = true;
            }
        }


        /// <summary>
        /// ピンボタンクリックイベント
        /// </summary>
        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            isPinned = !isPinned;
            PinButton.Content = isPinned ? "📍" : "📌";  // ピン状態の表示切替
        }

        /// <summary>
        /// マウスオーバーで強調
        /// </summary>
        private void Emphasize()
        {
            if (isLocked || isPinned) return;
            Animate(ExpandPanel, WidthProperty, HoverWidth);
        }

        /// <summary>
        /// マウスアウトで元のサイズに戻す
        /// </summary>
        private void Deemphasize()
        {
            ActiveRelease();

            if (isLocked || isPinned) return;
            Animate(ExpandPanel, WidthProperty, CollapsedWidth);
        }

        /// <summary>
        /// 展開処理
        /// </summary>
        private void Expand()
        {
            if (isLocked || isPinned) return;

            isLocked = true;

            Animate(ExpandPanel, WidthProperty, ExpandedWidth);
            Animate(ExpandPanel, HeightProperty, ExpandedHeight);
            AnimateCanvasTop(ExpandPanel, ExpandedTop);
            Animate(ExpandPanel, Canvas.LeftProperty, GetExpandedLeft());

            AnimateCornerRadius(ExpandPanel, new CornerRadius(InitialCornerRadius), new CornerRadius(ExpandedCornerRadius));

            AnimateGradientToSingleColor(TargetColor);

            PageButtonsPanel.Visibility = Visibility.Visible;
            PageContainer.Visibility = Visibility.Visible;
            PinButton.Visibility = Visibility.Visible;

            // 初回のみページ初期化
            if (PageButtonsPanel.Children.Count == 0)
            {
                InitializePages();
            }

            lockTimer.Start();
            mouseCheckTimer.Start();
        }

        /// <summary>
        /// 縮小処理
        /// </summary>
        private void Collapse()
        {
            ActiveRelease();

            PageButtonsPanel.Visibility = Visibility.Collapsed;
            PageContainer.Visibility = Visibility.Collapsed;
            PinButton.Visibility = Visibility.Collapsed;

            Animate(ExpandPanel, WidthProperty, CollapsedWidth);
            Animate(ExpandPanel, HeightProperty, CollapsedHeight);
            AnimateCanvasTop(ExpandPanel, CollapsedTop);
            Animate(ExpandPanel, Canvas.LeftProperty, GetCollapsedLeft());

            AnimateCornerRadius(ExpandPanel, new CornerRadius(ExpandedCornerRadius), new CornerRadius(CollapsedCornerRadius));

            AnimateGradientToOriginalColors();
        }

        /// <summary>
        /// 初期位置設定
        /// </summary>
        private void SetCollapsedPosition()
        {
            Canvas.SetLeft(ExpandPanel, GetCollapsedLeft());
            Canvas.SetTop(ExpandPanel, CollapsedTop);
        }

        /// <summary>
        /// 展開時の左座標計算
        /// </summary>
        private double GetExpandedLeft()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            return screenWidth - ExpandedWidth - 50;
        }

        /// <summary>
        /// 縮小時の左座標計算
        /// </summary>
        private double GetCollapsedLeft()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            return screenWidth - (CollapsedWidth / 2);
        }

        /// <summary>
        /// グラデーションを単色に
        /// </summary>
        private void AnimateGradientToSingleColor(Color toColor)
        {
            if (ExpandPanel.Background is RadialGradientBrush brush)
            {
                foreach (var stop in brush.GradientStops)
                {
                    AnimateGradientStop(stop, toColor);
                }
            }
        }

        /// <summary>
        /// グラデーションを元に戻す
        /// </summary>
        private void AnimateGradientToOriginalColors()
        {
            if (ExpandPanel.Background is RadialGradientBrush brush)
            {
                for (int i = 0; i < brush.GradientStops.Count; i++)
                {
                    AnimateGradientStop(brush.GradientStops[i], OriginalColors[i]);
                }
            }
        }

        /// <summary>
        /// GradientStopの色をアニメーション
        /// </summary>
        private void AnimateGradientStop(GradientStop stop, Color toColor)
        {
            var colorAnim = new ColorAnimation
            {
                To = toColor,
                Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
                FillBehavior = FillBehavior.HoldEnd
            };
            stop.BeginAnimation(GradientStop.ColorProperty, colorAnim);
        }

        /// <summary>
        /// 数値アニメーション
        /// </summary>
        private void Animate(UIElement target, DependencyProperty property, double toValue)
        {
            var anim = new DoubleAnimation
            {
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
                FillBehavior = FillBehavior.HoldEnd
            };
            target.BeginAnimation(property, anim);
        }

        /// <summary>
        /// Canvas.Topのアニメーション
        /// </summary>
        private void AnimateCanvasTop(FrameworkElement element, double toValue)
        {
            var anim = new DoubleAnimation
            {
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
                FillBehavior = FillBehavior.HoldEnd
            };
            anim.Completed += (s, e) => Canvas.SetTop(element, toValue);
            element.BeginAnimation(Canvas.TopProperty, anim);
        }

        /// <summary>
        /// 角丸のアニメーション
        /// </summary>
        private void AnimateCornerRadius(Border border, CornerRadius from, CornerRadius to)
        {
            var anim = new CornerRadiusAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
                FillBehavior = FillBehavior.HoldEnd
            };
            border.BeginAnimation(Border.CornerRadiusProperty, anim);
        }

        // ページの種類
        public enum PageType
        {
            Memo,
            Task
        }

        // ページインスタンス管理
        private readonly Dictionary<PageType, UserControl> pageInstances = new();

        /// <summary>
        /// ページ初期化
        /// </summary>
        private void InitializePages()
        {
            foreach (PageType page in Enum.GetValues(typeof(PageType)))
            {
                var btn = new Button
                {
                    Content = page.ToString(),
                    Margin = new Thickness(5),
                    Tag = page
                };

                btn.Style = (Style)FindResource("RoundedButton");

                btn.Click += PageButton_Click;
                PageButtonsPanel.Children.Add(btn);

                pageInstances[page] = CreatePage(page);
            }

            ShowPage(PageType.Memo);
        }

        /// <summary>
        /// ページ切り替えボタン処理
        /// </summary>
        private void PageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PageType page)
            {
                ShowPage(page);
            }
        }

        /// <summary>
        /// ページ表示
        /// </summary>
        private void ShowPage(PageType page)
        {
            ActiveRelease();

            if (pageInstances.TryGetValue(page, out var control))
            {
                PageContainer.Content = control;
            }
        }

        /// <summary>
        /// ページ作成
        /// </summary>
        private UserControl CreatePage(PageType page)
        {
            switch (page)
            {
                case PageType.Memo: return new MemoPage();
                case PageType.Task: return new TaskPage();
                default: return new UserControl();
            }
        }

        /// <summary>
        /// アクティブ解除
        /// </summary>
        private void ActiveRelease()
        {
            if (PageContainer.Content is MemoPage memo)
            {
                memo.DeselectImage();  // 前ページがMemoPageなら選択解除
            }
        }
    }
}
