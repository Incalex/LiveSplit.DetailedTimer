﻿using LiveSplit.Model;
using LiveSplit.Model.Comparisons;
using LiveSplit.TimeFormatters;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.UI.Components
{
    public class DetailedTimer : IComponent
    {
        public LiveSplit.UI.Components.Timer InternalComponent { get; set; }
        public SegmentTimer SegmentTimer { get; set; }
        public SimpleLabel LabelSegment { get; set; }
        public SimpleLabel LabelBest { get; set; }
        public SimpleLabel SegmentTime { get; set; }
        public SimpleLabel BestSegmentTime { get; set; }
        public SimpleLabel SplitName { get; set; }
        public DetailedTimerSettings Settings { get; set; }
        public GraphicsCache Cache { get; set; }

        public String Comparison { get; set; }
        public String Comparison2 { get; set; }
        public String ComparisonName { get; set; }
        public String ComparisonName2 { get; set; }
        public bool HideComparison { get; set; }

        protected int FrameCount { get; set; }

        protected int IconWidth { get; set; }

        public Image ShadowImage { get; set; }
        protected Image OldImage { get; set; }

        public static readonly Image NoIconImage = LiveSplit.Properties.Resources.DefaultSplitIcon.ToBitmap();

        public float PaddingTop { get { return 0f; } }
        public float PaddingLeft { get { return 7f; } }
        public float PaddingBottom { get { return 0f; } }
        public float PaddingRight { get { return 7f; } }

        public float VerticalHeight
        {
            get { return Settings.Height; }
        }

        public float HorizontalWidth
        {
            get { return Settings.Width; }
        }

        public float MinimumWidth
        {
            get { return 20; }
        }

        public float MinimumHeight
        {
            get { return 20; }
        }

        public IDictionary<string, Action> ContextMenuControls
        {
            get { return null; }
        }

        public DetailedTimer(LiveSplitState state)
        {
            InternalComponent = new LiveSplit.UI.Components.Timer();
            SegmentTimer = new SegmentTimer();
            Settings = new DetailedTimerSettings()
            {
                CurrentState = state
            };
            IconWidth = 0;
            Cache = new GraphicsCache();
            LabelSegment = new SimpleLabel();
            LabelBest = new SimpleLabel();
            SegmentTime = new SimpleLabel();
            BestSegmentTime = new SimpleLabel();
            SplitName = new SimpleLabel();
        }

        public void DrawGeneral(Graphics g, LiveSplitState state, float width, float height)
        {
            if (Settings.BackgroundColor.ToArgb() != Color.Transparent.ToArgb()
            || Settings.BackgroundGradient != GradientType.Plain
            && Settings.BackgroundColor2.ToArgb() != Color.Transparent.ToArgb())
                {
                    var gradientBrush = new LinearGradientBrush(
                                new PointF(0, 0),
                                Settings.BackgroundGradient == GradientType.Horizontal
                                ? new PointF(width, 0)
                                : new PointF(0, height),
                                Settings.BackgroundColor,
                                Settings.BackgroundGradient == GradientType.Plain
                                ? Settings.BackgroundColor
                                : Settings.BackgroundColor2);
                    g.FillRectangle(gradientBrush, 0, 0, width, height);
                }
                var lastSplitOffset = state.CurrentSplitIndex == state.Run.Count ? -1 : 0;
                var originalDrawSize = Settings.IconSize;
                if (Settings.DisplayIcon && state.CurrentSplitIndex >= 0)
                {
                    var icon = state.Run[state.CurrentSplitIndex + lastSplitOffset].Icon ?? NoIconImage;
                    if (OldImage != icon)
                    {
                        ImageAnimator.Animate(icon, (s, o) => { });
                        OldImage = icon;
                    }
                    var drawWidth = originalDrawSize;
                    var drawHeight = originalDrawSize;
                    if (icon.Width > icon.Height)
                    {
                        var ratio = icon.Height / (float)icon.Width;
                        drawHeight *= ratio;
                    }
                    else
                    {
                        var ratio = icon.Width / (float)icon.Height;
                        drawWidth *= ratio;
                    }

                    ImageAnimator.UpdateFrames(icon);

                    var oldClip = g.Clip;
                    g.IntersectClip(new RectangleF(0, 0, width - Math.Max(InternalComponent.ActualWidth, SegmentTimer.ActualWidth), height));
                    g.DrawImage(
                        icon,
                        7 + (originalDrawSize - drawWidth) / 2,
                        (height - originalDrawSize) / 2.0f + (originalDrawSize - drawHeight) / 2,
                        drawWidth,
                        drawHeight);
                    g.Clip = oldClip;
                }

                IconWidth = Settings.DisplayIcon ? (int)(originalDrawSize + 7.5f) : 0;

                InternalComponent.Settings.ShowGradient = Settings.TimerShowGradient;
                InternalComponent.Settings.OverrideSplitColors = Settings.OverrideTimerColors;
                InternalComponent.Settings.TimerColor = Settings.TimerColor;
                InternalComponent.Settings.TimerAccuracy = Settings.TimerAccuracy;
                SegmentTimer.Settings.ShowGradient = Settings.SegmentTimerShowGradient;
                SegmentTimer.Settings.OverrideSplitColors = true;
                SegmentTimer.Settings.TimerColor = Settings.SegmentTimerColor;
                SegmentTimer.Settings.TimerAccuracy = Settings.SegmentTimerAccuracy;

                var formatter = new SegmentTimesFormatter(Settings.SegmentTimesAccuracy);

                if (state.CurrentSplitIndex >= 0)
                {
                    var labelsFont = new Font(Settings.SegmentLabelsFont.FontFamily, Settings.SegmentLabelsFont.Size, Settings.SegmentLabelsFont.Style);
                    var timesFont = new Font(Settings.SegmentTimesFont.FontFamily, Settings.SegmentTimesFont.Size, Settings.SegmentTimesFont.Style);
                    LabelSegment.Font = labelsFont;
                    LabelSegment.X = 5 + IconWidth;
                    LabelSegment.Y = height * ((100f - Settings.SegmentTimerSizeRatio) / 100f);
                    LabelSegment.Width = width - SegmentTimer.ActualWidth - 5 - IconWidth;
                    LabelSegment.Height = height * (Settings.SegmentTimerSizeRatio / 200f) * (!HideComparison ? 1f : 2f);
                    LabelSegment.HorizontalAlignment = StringAlignment.Near;
                    LabelSegment.VerticalAlignment = StringAlignment.Center;
                    LabelSegment.ForeColor = Settings.SegmentLabelsColor;
                    LabelSegment.HasShadow = state.LayoutSettings.DropShadows;
                    LabelSegment.ShadowColor = state.LayoutSettings.ShadowsColor;
                    if (Comparison != "None")
                        LabelSegment.Draw(g);

                    LabelBest.Font = labelsFont;
                    LabelBest.X = 5 + IconWidth;
                    LabelBest.Y = height * ((100f - Settings.SegmentTimerSizeRatio / 2f) / 100f);
                    LabelBest.Width = width - SegmentTimer.ActualWidth - 5 - IconWidth;
                    LabelBest.Height = height * (Settings.SegmentTimerSizeRatio / 200f);
                    LabelBest.HorizontalAlignment = StringAlignment.Near;
                    LabelBest.VerticalAlignment = StringAlignment.Center;
                    LabelBest.ForeColor = Settings.SegmentLabelsColor;
                    LabelBest.HasShadow = state.LayoutSettings.DropShadows;
                    LabelBest.ShadowColor = state.LayoutSettings.ShadowsColor;
                    if (!HideComparison)
                        LabelBest.Draw(g);

                    var offset = Math.Max(LabelSegment.ActualWidth, HideComparison ? 0 : LabelBest.ActualWidth) + 10;

                    if (Comparison != "None")
                    {
                        SegmentTime.Font = timesFont;
                        SegmentTime.X = offset + IconWidth;
                        SegmentTime.Y = height * ((100f - Settings.SegmentTimerSizeRatio) / 100f);
                        SegmentTime.Width = width - SegmentTimer.ActualWidth - offset - IconWidth;
                        SegmentTime.Height = height * (Settings.SegmentTimerSizeRatio / 200f) * (!HideComparison ? 1f : 2f);
                        SegmentTime.HorizontalAlignment = StringAlignment.Near;
                        SegmentTime.VerticalAlignment = StringAlignment.Center;
                        SegmentTime.ForeColor = Settings.SegmentTimesColor;
                        SegmentTime.HasShadow = state.LayoutSettings.DropShadows;
                        SegmentTime.ShadowColor = state.LayoutSettings.ShadowsColor;
                        SegmentTime.IsMonospaced = true;
                        SegmentTime.Draw(g);
                    }

                    if (!HideComparison)
                    {
                        BestSegmentTime.X = offset + IconWidth;
                        BestSegmentTime.Y = height * ((100f - Settings.SegmentTimerSizeRatio / 2f) / 100f);
                        BestSegmentTime.Width = width - SegmentTimer.ActualWidth - offset - IconWidth;
                        BestSegmentTime.Height = height * (Settings.SegmentTimerSizeRatio / 200f);
                        BestSegmentTime.HorizontalAlignment = StringAlignment.Near;
                        BestSegmentTime.VerticalAlignment = StringAlignment.Center;
                        BestSegmentTime.ForeColor = Settings.SegmentTimesColor;
                        BestSegmentTime.HasShadow = state.LayoutSettings.DropShadows;
                        BestSegmentTime.ShadowColor = state.LayoutSettings.ShadowsColor;
                        BestSegmentTime.IsMonospaced = true;
                        BestSegmentTime.ShadowColor = state.LayoutSettings.ShadowsColor;
                        BestSegmentTime.Font = timesFont;
                        BestSegmentTime.Draw(g);
                    }
                    SplitName.Font = Settings.SplitNameFont;
                    SplitName.X = IconWidth + 5;
                    SplitName.Y = 0;
                    SplitName.Width = width - InternalComponent.ActualWidth - IconWidth - 5;
                    SplitName.Height = height * ((100f - Settings.SegmentTimerSizeRatio) / 100f);
                    SplitName.HorizontalAlignment = StringAlignment.Near;
                    SplitName.VerticalAlignment = StringAlignment.Center;
                    SplitName.ForeColor = Settings.SplitNameColor;
                    SplitName.HasShadow = state.LayoutSettings.DropShadows;
                    SplitName.ShadowColor = state.LayoutSettings.ShadowsColor;
                    if (Settings.ShowSplitName)
                        SplitName.Draw(g);
                    SegmentTime.ShadowColor = state.LayoutSettings.ShadowsColor;
                    SplitName.ShadowColor = state.LayoutSettings.ShadowsColor;
                }
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            DrawGeneral(g, state, width, VerticalHeight);
            var oldMatrix = g.Transform;
            InternalComponent.Settings.TimerHeight = VerticalHeight * ((100f - Settings.SegmentTimerSizeRatio) / 100f);
            InternalComponent.DrawVertical(g, state, width, clipRegion);
            g.Transform = oldMatrix;
            g.TranslateTransform(0, VerticalHeight * ((100f - Settings.SegmentTimerSizeRatio) / 100f));
            SegmentTimer.Settings.TimerHeight = VerticalHeight * (Settings.SegmentTimerSizeRatio / 100f);
            SegmentTimer.DrawVertical(g, state, width, clipRegion);
            g.Transform = oldMatrix;
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            DrawGeneral(g, state, HorizontalWidth, height);
            var oldMatrix = g.Transform;
            InternalComponent.Settings.TimerWidth = HorizontalWidth;
            InternalComponent.DrawHorizontal(g, state, height * ((100f - Settings.SegmentTimerSizeRatio) / 100f), clipRegion);
            g.Transform = oldMatrix;
            g.TranslateTransform(0, height * ((100f - Settings.SegmentTimerSizeRatio) / 100f));
            SegmentTimer.DrawHorizontal(g, state, height * (Settings.SegmentTimerSizeRatio / 100f), clipRegion);
            SegmentTimer.Settings.TimerWidth = HorizontalWidth;
            g.Transform = oldMatrix;
        }

        public string ComponentName
        {
            get { return "Detailed Timer"; }
        }


        public Control GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public void SetSettings(System.Xml.XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            return Settings.GetSettings(document);
        }


        public void RenameComparison(string oldName, string newName)
        {
            if (Settings.Comparison == oldName)
                Settings.Comparison = newName;
            if (Settings.Comparison2 == oldName)
                Settings.Comparison2 = newName;
        }


        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            var lastSplitOffset = state.CurrentSplitIndex == state.Run.Count ? -1 : 0;

            var timingMethod = state.CurrentTimingMethod;
            if (Settings.TimingMethod == "Real Time")
                timingMethod = TimingMethod.RealTime;
            else if (Settings.TimingMethod == "Game Time")
                timingMethod = TimingMethod.GameTime;

            var formatter = new SegmentTimesFormatter(Settings.SegmentTimesAccuracy);

            if (state.CurrentSplitIndex >= 0)
            {
                Comparison = Settings.Comparison == "Current Comparison" ? state.CurrentComparison : Settings.Comparison;
                Comparison2 = Settings.Comparison2 == "Current Comparison" ? state.CurrentComparison : Settings.Comparison2;
                HideComparison = Settings.HideComparison;

                if (HideComparison || !state.Run.Comparisons.Contains(Comparison2) || Comparison2 == "None")
                {
                    HideComparison = true;
                    if (!state.Run.Comparisons.Contains(Comparison) || Comparison == "None")
                        Comparison = state.CurrentComparison;
                }
                else if (!state.Run.Comparisons.Contains(Comparison) || Comparison == "None")
                {
                    HideComparison = true;
                    Comparison = Comparison2;
                }
                else if (Comparison == Comparison2)
                    HideComparison = true;

                ComparisonName = CompositeComparisons.GetShortComparisonName(Comparison);
                ComparisonName2 = CompositeComparisons.GetShortComparisonName(Comparison2);

                TimeSpan? segmentTime = null;

                if (Comparison == BestSegmentsComparisonGenerator.ComparisonName)
                    segmentTime = state.Run[state.CurrentSplitIndex + lastSplitOffset].BestSegmentTime[timingMethod];
                else
                {
                    if (state.CurrentSplitIndex == 0 || (state.CurrentSplitIndex == 1 && lastSplitOffset == -1))
                        segmentTime = state.Run[0].Comparisons[Comparison][timingMethod];
                    else if (state.CurrentSplitIndex > 0)
                        segmentTime = state.Run[state.CurrentSplitIndex + lastSplitOffset].Comparisons[Comparison][timingMethod]
                            - state.Run[state.CurrentSplitIndex - 1 + lastSplitOffset].Comparisons[Comparison][timingMethod];
                }

                LabelSegment.Text = ComparisonName + ":";

                LabelBest.Text = ComparisonName2 + ":";

                if (Comparison != "None")
                {
                    if (segmentTime != null)
                        SegmentTime.Text = formatter.Format(segmentTime);
                    else
                        SegmentTime.Text = "-";
                }

                if (!HideComparison)
                {
                    TimeSpan? bestSegmentTime = null;
                    if (Comparison2 == BestSegmentsComparisonGenerator.ComparisonName)
                        bestSegmentTime = state.Run[state.CurrentSplitIndex + lastSplitOffset].BestSegmentTime[timingMethod];
                    else
                    {
                    if (state.CurrentSplitIndex == 0 || (state.CurrentSplitIndex == 1 && lastSplitOffset == -1))
                        bestSegmentTime = state.Run[0].Comparisons[Comparison2][timingMethod];
                    else if (state.CurrentSplitIndex > 0)
                        bestSegmentTime = state.Run[state.CurrentSplitIndex + lastSplitOffset].Comparisons[Comparison2][timingMethod]
                            - state.Run[state.CurrentSplitIndex - 1 + lastSplitOffset].Comparisons[Comparison2][timingMethod];
                    }

                    if (bestSegmentTime != null)
                        BestSegmentTime.Text = formatter.Format(bestSegmentTime);
                    else
                        BestSegmentTime.Text = "-";
                }
                if (state.CurrentSplitIndex >= 0)
                    SplitName.Text = state.Run[state.CurrentSplitIndex + lastSplitOffset].Name;
                else
                    SplitName.Text = "";
            }

            SegmentTimer.Settings.TimingMethod = Settings.TimingMethod;
            InternalComponent.Settings.TimingMethod = Settings.TimingMethod;
            SegmentTimer.Update(null, state, width, height, mode);
            InternalComponent.Update(null, state, width, height, mode);

            var icon = state.CurrentSplitIndex >= 0 ? state.Run[state.CurrentSplitIndex + lastSplitOffset].Icon : null;

            Cache.Restart();
            Cache["SplitIcon"] = icon;
            if (Cache.HasChanged)
            {
                if (icon == null)
                    FrameCount = 0;
                else
                    FrameCount = icon.GetFrameCount(new FrameDimension(icon.FrameDimensionsList[0]));
            }
            Cache["SplitName"] = SplitName.Text;
            Cache["LabelSegment"] = LabelSegment.Text;
            Cache["LabelBest"] = LabelBest.Text;
            Cache["SegmentTime"] = SegmentTime.Text;
            Cache["BestSegmentTime"] = BestSegmentTime.Text;
            Cache["SegmentTimerText"] = SegmentTimer.BigTextLabel.Text + SegmentTimer.SmallTextLabel.Text;
            Cache["InternalComponentText"] = InternalComponent.BigTextLabel.Text + InternalComponent.SmallTextLabel.Text;
            if (InternalComponent.BigTextLabel.Brush != null && invalidator != null)
            {
                if (InternalComponent.BigTextLabel.Brush is LinearGradientBrush)
                    Cache["TimerColor"] = ((LinearGradientBrush)InternalComponent.BigTextLabel.Brush).LinearColors.First().ToArgb();
                else
                    Cache["TimerColor"] = InternalComponent.BigTextLabel.ForeColor.ToArgb();
            }

            if (invalidator != null && Cache.HasChanged || FrameCount > 1)
            {
                invalidator.Invalidate(0, 0, width, height);
            }
        }

        public void Dispose()
        {
        }
    }
}
