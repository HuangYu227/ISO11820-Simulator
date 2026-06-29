using System.Drawing.Drawing2D;
using ISO11820Simulator.Models;

namespace ISO11820Simulator.UI.Controls;

/// <summary>
/// 分析仪表盘 - 汇总卡片 + 产品对比柱状图
/// </summary>
public sealed class AnalyticsDashboard : UserControl
{
    private int _totalTests;
    private double _passRate;
    private double _avgTempRise;
    private double _avgWeightLoss;
    private readonly List<ProductBar> _bars = [];

    public AnalyticsDashboard()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Theme.Panel;
        MinimumSize = new Size(360, 280);
    }

    public void UpdateData(List<TestRecordSummary> records)
    {
        if (records == null || records.Count == 0)
        {
            _totalTests = 0; _passRate = 0; _avgTempRise = 0; _avgWeightLoss = 0;
            _bars.Clear();
            Invalidate();
            return;
        }

        _totalTests = records.Count;
        var saved = records.Where(r => r.Flag == "10000000").ToList();
        if (saved.Count > 0)
        {
            var passed = saved.Count(r => r.DeltaTf <= 50 && r.LostWeightPercent <= 50);
            _passRate = (double)passed / saved.Count * 100;
            _avgTempRise = saved.Average(r => r.DeltaTf);
            _avgWeightLoss = saved.Average(r => r.LostWeightPercent);
        }

        _bars.Clear();
        var groups = records
            .Where(r => !string.IsNullOrEmpty(r.ProductName))
            .GroupBy(r => r.ProductName)
            .OrderByDescending(g => g.Count())
            .Take(8);

        foreach (var g in groups)
        {
            _bars.Add(new ProductBar
            {
                Name = g.Key.Length > 6 ? g.Key[..6] : g.Key,
                Count = g.Count(),
                AvgDeltaT = g.Average(r => r.DeltaTf),
                AvgWeightLoss = g.Average(r => r.LostWeightPercent)
            });
        }

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
        if (w < 100 || h < 100) return;

        var cardH = Math.Min(80, h * 0.3f);
        var cardY = 8;
        var cardGap = 8;
        var cardW = (w - cardGap * 5) / 4f;

        DrawSummaryCard(g, cardGap, cardY, cardW, cardH,
            "试验总数", _totalTests.ToString(), Theme.Accent, "次");

        DrawSummaryCard(g, cardGap * 2 + cardW, cardY, cardW, cardH,
            "通过率", $"{_passRate:F1}", _passRate >= 80 ? Theme.Success : _passRate >= 60 ? Theme.Warning : Theme.Danger, "%");

        DrawSummaryCard(g, cardGap * 3 + cardW * 2, cardY, cardW, cardH,
            "平均温升", $"{_avgTempRise:F1}", _avgTempRise <= 50 ? Theme.Success : Theme.Warning, "°C");

        DrawSummaryCard(g, cardGap * 4 + cardW * 3, cardY, cardW, cardH,
            "平均失重", $"{_avgWeightLoss:F2}", _avgWeightLoss <= 5 ? Theme.Success : Theme.Warning, "%");

        var chartTop = cardY + cardH + 16;
        var chartBottom = h - 10;
        var chartLeft = 50;
        var chartRight = w - 15;
        var chartH = chartBottom - chartTop;
        var chartW = chartRight - chartLeft;
        if (chartH < 40 || chartW < 40) return;

        using var chartTitleFont = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);
        using var chartTitleBrush = new SolidBrush(Theme.Text);
        g.DrawString("产品对比", chartTitleFont, chartTitleBrush, chartLeft, chartTop - 2);

        if (_bars.Count == 0)
        {
            using var noDataFont = new Font("Microsoft YaHei UI", 10);
            using var noDataBrush = new SolidBrush(Theme.Muted);
            var noData = "暂无试验数据";
            var ndSize = g.MeasureString(noData, noDataFont);
            g.DrawString(noData, noDataFont, noDataBrush,
                chartLeft + (chartW - ndSize.Width) / 2,
                chartTop + (chartH - ndSize.Height) / 2);
            return;
        }

        var barAreaTop = chartTop + 18;
        var barAreaH = chartBottom - barAreaTop - 22;
        var barAreaW = chartW;

        var maxVal = _bars.Max(b => Math.Max(b.AvgDeltaT, b.AvgWeightLoss));
        if (maxVal < 1) maxVal = 1;
        maxVal = Math.Ceiling(maxVal / 10) * 10;

        using var gridPen = new Pen(Color.FromArgb(25, Theme.Border), 1);
        using var axisFont = new Font("Consolas", 7);
        using var axisBrush = new SolidBrush(Theme.Muted);
        var gridSteps = 4;
        for (int i = 0; i <= gridSteps; i++)
        {
            var yy = barAreaTop + barAreaH - (float)i / gridSteps * barAreaH;
            g.DrawLine(gridPen, chartLeft, yy, chartRight, yy);
            var val = maxVal * i / gridSteps;
            g.DrawString($"{val:F0}", axisFont, axisBrush, chartLeft - 30, yy - 6);
        }

        var barCount = _bars.Count;
        var groupW = barAreaW / barCount;
        var barW = Math.Max(6, groupW * 0.35f);
        var barGap = 3;

        using var nameFont = new Font("Microsoft YaHei UI", 7);
        for (int i = 0; i < barCount; i++)
        {
            var bar = _bars[i];
            var groupX = chartLeft + i * groupW;

            var h1 = (float)(bar.AvgDeltaT / maxVal) * barAreaH;
            var barRect1 = new RectangleF(groupX + groupW / 2 - barW - barGap / 2, barAreaTop + barAreaH - h1, barW, h1);
            using var brush1 = new LinearGradientBrush(barRect1, Theme.Accent, Color.FromArgb(180, Theme.Accent), 90);
            FillRoundedRect(g, brush1, barRect1, 2);

            var h2 = (float)(bar.AvgWeightLoss / maxVal) * barAreaH;
            var barRect2 = new RectangleF(groupX + groupW / 2 + barGap / 2, barAreaTop + barAreaH - h2, barW, h2);
            using var brush2 = new LinearGradientBrush(barRect2, Theme.Success, Color.FromArgb(180, Theme.Success), 90);
            FillRoundedRect(g, brush2, barRect2, 2);

            var nameSize = g.MeasureString(bar.Name, nameFont);
            g.DrawString(bar.Name, nameFont, axisBrush,
                groupX + (groupW - nameSize.Width) / 2, barAreaTop + barAreaH + 4);
        }

        using var legendFont = new Font("Microsoft YaHei UI", 8);
        var legendY = chartTop + 2;
        var legendX = chartRight - 160;
        if (legendX < chartLeft + 60) legendX = chartLeft + 60;
        using var blueBrush = new SolidBrush(Theme.Accent);
        using var greenBrush = new SolidBrush(Theme.Success);
        g.FillRectangle(blueBrush, legendX, legendY, 10, 10);
        g.DrawString("平均温升(°C)", legendFont, axisBrush, legendX + 13, legendY - 1);
        g.FillRectangle(greenBrush, legendX + 85, legendY, 10, 10);
        g.DrawString("平均失重(%)", legendFont, axisBrush, legendX + 98, legendY - 1);
    }

    private void DrawSummaryCard(Graphics g, float x, float y, float w, float h,
        string title, string value, Color accent, string unit)
    {
        var rect = new RectangleF(x, y, w, h);
        using var bgBrush = new SolidBrush(Color.FromArgb(200, Theme.Panel2));
        FillRoundedRect(g, bgBrush, rect, 6);
        using var borderPen = new Pen(Color.FromArgb(60, Theme.Border), 1);
        DrawRoundedRect(g, borderPen, rect, 6);

        using var accentBrush = new SolidBrush(accent);
        FillRoundedRect(g, accentBrush, new RectangleF(x, y + 6, 3, h - 12), 1);

        using var titleFont = new Font("Microsoft YaHei UI", 8);
        using var titleBrush = new SolidBrush(Theme.Muted);
        g.DrawString(title, titleFont, titleBrush, x + 10, y + 8);

        var fontSize = Math.Max(10, Math.Min(18, h * 0.28f));
        using var valFont = new Font("Consolas", fontSize, FontStyle.Bold);
        using var valBrush = new SolidBrush(accent);
        var valSize = g.MeasureString(value, valFont);
        var valY = y + Math.Max(22, h * 0.35f);
        if (valY + valSize.Height > y + h - 4) valY = y + h - valSize.Height - 4;
        g.DrawString(value, valFont, valBrush, x + 10, valY);

        using var unitFont = new Font("Microsoft YaHei UI", 8);
        using var unitBrush = new SolidBrush(Theme.Muted);
        var unitY = valY + valSize.Height - 12;
        if (unitY > y + h - 14) unitY = y + h - 14;
        g.DrawString(unit, unitFont, unitBrush, x + 10 + valSize.Width + 2, unitY);
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

    private sealed class ProductBar
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public double AvgDeltaT { get; set; }
        public double AvgWeightLoss { get; set; }
    }
}
