using System;
using System.Windows;
using System.Windows.Media.Animation;

/// <summary>
/// CornerRadius 用のアニメーションを定義するカスタム AnimationTimeline。
/// WPF には標準で CornerRadiusAnimation が無いので自作します。
/// </summary>
public class CornerRadiusAnimation : AnimationTimeline
{
    /// <summary>
    /// アニメーション対象の型は CornerRadius。
    /// </summary>
    public override Type TargetPropertyType => typeof(CornerRadius);

    // 定数
    private const double DefaultProgressIfNull = 0.0; // Progressがnullのときのデフォルト値
    private const double FullProgress = 1.0;         // 完了時の進捗

    #region From プロパティ
    /// <summary>
    /// アニメーション開始時の CornerRadius。
    /// </summary>
    public CornerRadius From
    {
        get => (CornerRadius)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    public static readonly DependencyProperty FromProperty =
        DependencyProperty.Register(
            nameof(From),
            typeof(CornerRadius),
            typeof(CornerRadiusAnimation)
        );
    #endregion

    #region To プロパティ
    /// <summary>
    /// アニメーション終了時の CornerRadius。
    /// </summary>
    public CornerRadius To
    {
        get => (CornerRadius)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    public static readonly DependencyProperty ToProperty =
        DependencyProperty.Register(
            nameof(To),
            typeof(CornerRadius),
            typeof(CornerRadiusAnimation)
        );
    #endregion

    /// <summary>
    /// アニメーションの現在の値を計算します。
    /// </summary>
    /// <param name="defaultOriginValue">既定の開始値</param>
    /// <param name="defaultDestinationValue">既定の終了値</param>
    /// <param name="animationClock">アニメーションクロック</param>
    /// <returns>現在の CornerRadius</returns>
    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
    {
        var from = From;
        var to = To;

        // 進捗（0.0〜1.0）を取得。nullなら0.0。
        double progress = animationClock.CurrentProgress ?? DefaultProgressIfNull;

        // 4つの角を補間する
        return new CornerRadius(
            Lerp(from.TopLeft, to.TopLeft, progress),
            Lerp(from.TopRight, to.TopRight, progress),
            Lerp(from.BottomRight, to.BottomRight, progress),
            Lerp(from.BottomLeft, to.BottomLeft, progress)
        );
    }

    /// <summary>
    /// Freezable のインスタンス生成
    /// </summary>
    protected override Freezable CreateInstanceCore()
    {
        return new CornerRadiusAnimation();
    }

    /// <summary>
    /// 線形補間（Lerp）ヘルパー
    /// </summary>
    private double Lerp(double start, double end, double progress)
    {
        return start + (end - start) * progress;
    }
}
