using System.Drawing.Drawing2D;

namespace ISO11820Simulator.UI.Controls;

/// <summary>
/// 校准热力图 - 3x3网格显示炉壁校准点温度分布
/// </summary>
public sealed class CalibrationHeatmap : UserControl
{
    private readonly double[] _temps = new double[9];
    private readonly string[] _cellLabels = new[] { "左上", "上中", "右上", "左中", "中心", "右中", "左下", "下中", "右下" };
    private bool _hasData;

    public CalibrationHeatmap()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Theme.Panel;
        MinimumSize = new Size(200, 220);
    }

    public void UpdateData(double[] temps9)
    {
        if (temps9 == null || temps9.Length < 9) return;
        Array.Copy(temps9, _temps, 9);
        _hasData = true;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var w = ClientSize.Width;
        var h = ClientSize.Height;
        if (w < 80 || h < 80) return;

        using var titleFont = new Font("Microsoft YaHei UI", 11, FontStyle.Bold);
        using var titleBrush = new SolidBrush(Theme.Text);
        g.DrawString("校准热力图", titleFont, titleBrush, 10, 6);

        var topMargin = 32;
        var bottomReserve = 80;
        var sideMargin = 15;
        var gridAreaW = w - sideMargin * 2;
        var gridAreaH = h - topMargin - bottomReserve;
        if (gridAreaW < 60 || gridAreaH < 60) return;

        var cellW = gridAreaW / 3f;
        var cellH = gridAreaH / 3f;
        var gridX = sideMargin;
        var gridY = topMargin;

        double minT, maxT, avgT;
        if (_hasData)
        {
            minT = _temps.Min();
            maxT = _temps.Max();
            avgT = _temps.Average();
        }
        else
        {
            minT = 0; maxT = 100; avgT = 50;
        }
        var range = maxT - minT;
        if (range < 1) range = 1;

        using var cellFont = new Font("Consolas", Math.Max(8, Math.Min(cellW, cellH) * 0.22f), FontStyle.Bold);
        using var posFont = new Font("Microsoft YaHei UI", Math.Max(6, Math.Min(cellW, cellH) * 0.12f));

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int idx = row * 3 + col;
                var cx = gridX + col * cellW;
                var cy = gridY + row * cellH;
                var cellRect = new RectangleF(cx + 1, cy + 1, cellW - 2, cellH - 2);

                Color cellColor;
                if (_hasData)
                {
                    var t = (_temps[idx] - minT) / range;
                    cellColor = TempToColor(t);
                }
                else
                {
                    cellColor = Color.FromArgb(30, Theme.Panel2);
                }

                using var cellBrush = new SolidBrush(cellColor);
                FillRoundedRect(g, cellBrush, cellRect, 4);

                using var cellPen = new Pen(Color.FromArgb(60, Theme.Border), 1);
                DrawRoundedRect(g, cellPen, cellRect, 4);

                if (_hasData)
                {
                    var tempText = $"{_temps[idx]:F1}";
                    var tempSize = g.MeasureString(tempText, cellFont);
                    var brightness = cellColor.R * 0.299 + cellColor.G * 0.587 + cellColor.B * 0.114;
                    using var textBrush = new SolidBrush(brightness > 128 ? Color.Black : Color.White);
                    g.DrawString(tempText, cellFont, textBrush,
                        cx + (cellW - tempSize.Width) / 2,
                        cy + (cellH - tempSize.Height) / 2 - 2);
                }

                using var posBrush = new SolidBrush(Color.FromArgb(150, Theme.Muted));
                g.DrawString(_cellLabels[idx], posFont, posBrush, cx + 3, cy + 2);
            }
        }

        var statsY = gridY + gridAreaH + 8;
        var statFontSize = w < 260 ? 7f : 9f;
        using var statFont = new Font("Microsoft YaHei UI", statFontSize);
        using var statBrush = new SolidBrush(Theme.Muted);

        if (_hasData)
        {
            var statsText = w < 260
                ? $"{minT:F1}~{maxT:F1}°C  avg:{avgT:F1}°C"
                : $"最低: {minT:F1}°C   最高: {maxT:F1}°C   平均: {avgT:F1}°C   偏差: ±{(maxT - minT) / 2:F1}°C";
            var statsSize = g.MeasureString(statsText, statFont);
            g.DrawString(statsText, statFont, statBrush, Math.Max(4, (w - statsSize.Width) / 2), statsY);
        }
        else
        {
            var noData = "暂无校准数据";
            var noDataSize = g.MeasureString(noData, statFont);
            g.DrawString(noData, statFont, statBrush, (w - noDataSize.Width) / 2, statsY);
        }

        var barY = statsY + 20;
        var barW = Math.Min(200, w - 80);
        var barH = 10;
        var barX = (w - barW) / 2;
        using var barBrush = new LinearGradientBrush(
            new RectangleF(barX, barY, barW, barH),
            Color.FromArgb(50, 100, 255), Color.FromArgb(255, 60, 60),
            LinearGradientMode.Horizontal);
        FillRoundedRect(g, barBrush, new RectangleF(barX, barY, barW, barH), 3);
        using var barBorderPen = new Pen(Theme.Border, 1);
        DrawRoundedRect(g, barBorderPen, new RectangleF(barX, barY, barW, barH), 3);

        using var barFont = new Font("Consolas", 8);
        if (_hasData)
        {
            g.DrawString($"{minT:F1}", barFont, statBrush, barX - 30, barY - 2);
            g.DrawString($"{maxT:F1}", barFont, statBrush, barX + barW + 3, barY - 2);
        }
        else
        {
            g.DrawString("冷", barFont, statBrush, barX - 14, barY - 2);
            g.DrawString("热", barFont, statBrush, barX + barW + 2, barY - 2);
        }
    }

    private static Color TempToColor(double t)
    {
        t = Math.Clamp(t, 0, 1);
        int r, g, b;
        if (t < 0.5)
        {
            var u = t * 2;
            r = (int)(50 + 205 * u);
            g = (int)(100 + 115 * u);
            b = (int)(255 - 215 * u);
        }
        else
        {
            var u = (t - 0.5) * 2;
            r = 255;
            g = (int)(215 - 155 * u);
            b = (int)(40 - 20 * u);
        }
        return Color.FromArgb(200, Math.Min(255, r), Math.Min(255, g), Math.Min(255, b));
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); Invalidate(); }

    private static void FillRoundedRect(Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using var path = CreateRoundedPath(rect, radius);
        g.FillPath(brush, path);
    }

    private static void DrawRoundedRect(Graphics g, Pen pen, RectangleF rect, float radius)
    {
        using var path = CreateRoundedPath(rect, radius);
        g.DrawPath(pen, path);
    }

    private static GraphicsPath CreateRoundedPath(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
