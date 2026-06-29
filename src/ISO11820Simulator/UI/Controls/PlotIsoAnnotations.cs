using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ISO11820Simulator.UI;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;

namespace ISO11820Simulator.UI.Controls;

/// <summary>
/// OxyPlot 统一样式与 ISO11820 稳定区间标注。
/// </summary>
public static class PlotIsoAnnotations
{
    public static void ApplyTemperaturePlotTheme(PlotModel model)
    {
        model.Title = "温度曲线";
        model.Subtitle = "745–755°C 稳定区间 / 750°C 目标线";
        model.TextColor = OxyColor.FromRgb(148, 163, 184);
        model.TitleColor = OxyColor.FromRgb(234, 242, 255);
        model.SubtitleColor = OxyColor.FromRgb(148, 163, 184);
        model.PlotAreaBorderColor = OxyColor.FromRgb(71, 85, 105);
        model.Background = OxyColor.FromRgb(17, 29, 47);

        model.Annotations.Clear();
        model.Annotations.Add(new RectangleAnnotation
        {
            MinimumY = 745,
            MaximumY = 755,
            Fill = OxyColor.FromArgb(30, Theme.Success.R, Theme.Success.G, Theme.Success.B),
            Layer = AnnotationLayer.BelowSeries,
            Text = "稳定区间 745–755°C",
            TextColor = OxyColor.FromArgb(160, Theme.Success.R, Theme.Success.G, Theme.Success.B)
        });

        model.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = 750,
            Text = "目标 750°C",
            Color = OxyColor.FromArgb(190, Theme.Warning.R, Theme.Warning.G, Theme.Warning.B),
            TextColor = OxyColor.FromArgb(220, Theme.Warning.R, Theme.Warning.G, Theme.Warning.B),
            LineStyle = LineStyle.Dash,
            StrokeThickness = 1.3,
            Layer = AnnotationLayer.BelowSeries
        });

        foreach (var axis in model.Axes.OfType<LinearAxis>())
        {
            axis.TextColor = OxyColor.FromRgb(210, 220, 235);
            axis.TitleColor = OxyColor.FromRgb(148, 163, 184);
            axis.AxislineColor = OxyColor.FromRgb(71, 85, 105);
            axis.TicklineColor = OxyColor.FromRgb(71, 85, 105);
            axis.MajorGridlineColor = OxyColor.FromArgb(36, 148, 163, 184);
            axis.MinorGridlineColor = OxyColor.FromArgb(18, 148, 163, 184);
            axis.MajorGridlineStyle = LineStyle.Solid;
            axis.MinorGridlineStyle = LineStyle.Dot;
        }
    }
}
