using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace ISO11820Simulator.UI.Controls;

internal static class ControlPaintUtil
{
    public static GraphicsPath RoundedRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        // 矩形太小时直接画普通矩形，避免 AddArc 参数无效
        if (rect.Width < 1f || rect.Height < 1f)
        {
            path.AddRectangle(rect);
            return path;
        }
        radius = Math.Max(0, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2));
        float d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void FillRounded(Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using var path = RoundedRect(rect, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRounded(Graphics g, Pen pen, RectangleF rect, float radius)
    {
        using var path = RoundedRect(rect, radius);
        g.DrawPath(pen, path);
    }

    public static Color WithAlpha(Color color, int alpha)
        => Color.FromArgb(Math.Clamp(alpha, 0, 255), color.R, color.G, color.B);

    public static Color Blend(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb(
            (int)(a.A + (b.A - a.A) * t),
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

    public static Color HeatColor(double temp, int alpha = 255)
    {
        // 20~800°C 映射为 冷蓝 → 青 → 黄 → 橙 → 红
        float t = (float)Math.Clamp((temp - 20.0) / 780.0, 0.0, 1.0);
        Color cold = Color.FromArgb(alpha, 47, 140, 255);
        Color cyan = Color.FromArgb(alpha, 24, 215, 255);
        Color yellow = Color.FromArgb(alpha, 255, 220, 70);
        Color orange = Color.FromArgb(alpha, 255, 139, 35);
        Color red = Color.FromArgb(alpha, 255, 77, 79);

        if (t < 0.25f) return Blend(cold, cyan, t / 0.25f);
        if (t < 0.55f) return Blend(cyan, yellow, (t - 0.25f) / 0.30f);
        if (t < 0.80f) return Blend(yellow, orange, (t - 0.55f) / 0.25f);
        return Blend(orange, red, (t - 0.80f) / 0.20f);
    }

    public static PointF Polar(PointF center, float radius, float degrees)
    {
        double a = degrees * Math.PI / 180.0;
        return new PointF(center.X + (float)Math.Cos(a) * radius, center.Y + (float)Math.Sin(a) * radius);
    }
}
