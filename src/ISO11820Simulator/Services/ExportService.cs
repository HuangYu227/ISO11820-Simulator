using ISO11820Simulator.Config;
using ISO11820Simulator.Models;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using PdfSharp.Fonts;
using Serilog;

namespace ISO11820Simulator.Services;

public sealed class ExportService
{
    private readonly AppSettings _settings;

    public ExportService(AppSettings settings)
    {
        _settings = settings;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        // 注册字体解析器
        if (GlobalFontSettings.FontResolver == null)
        {
            GlobalFontSettings.FontResolver = new SimpleFontResolver();
        }
    }

    public string GetTestDirectory(TestSession test)
    {
        var baseDir = PathResolver.Resolve(_settings.FileStorage.TestDataDirectory);
        var dir = Path.Combine(baseDir, test.ProductId, test.TestId);
        Directory.CreateDirectory(dir);
        return dir;
    }

    public string SaveCsv(TestSession test, IReadOnlyList<TemperatureSample> samples)
    {
        var file = Path.Combine(GetTestDirectory(test), "sensor_data.csv");
        using var writer = new StreamWriter(file, false, System.Text.Encoding.UTF8);
        writer.WriteLine("Time,Temp1,Temp2,TempSurface,TempCenter,TempCalibration");
        foreach (var s in samples)
        {
            writer.WriteLine($"{s.TimeSeconds},{s.Tf1:F2},{s.Tf2:F2},{s.Surface:F2},{s.Center:F2},{s.Calibration:F2}");
        }
        return file;
    }

    public string ExportExcelReport(TestSession test, TestResult result, IReadOnlyList<TemperatureSample> samples)
    {
        var outDir = PathResolver.Resolve(_settings.Report.OutputDirectory);
        Directory.CreateDirectory(outDir);
        var file = Path.Combine(outDir, $"{test.TestId}_报告.xlsx");
        using var package = new ExcelPackage();

        var info = package.Workbook.Worksheets.Add("试验信息");
        info.Cells[1, 1].Value = "ISO 11820 建筑材料不燃性试验报告";
        info.Cells[1, 1, 1, 4].Merge = true;
        info.Cells[1, 1].Style.Font.Size = 16;
        info.Cells[1, 1].Style.Font.Bold = true;
        var rows = new (string Name, object? Value)[]
        {
            ("样品编号", test.ProductId), ("试验ID", test.TestId), ("样品名称", test.ProductName), ("规格型号", test.Specific),
            ("直径(mm)", test.Diameter), ("高度(mm)", test.Height), ("环境温度(℃)", test.AmbientTemperature), ("环境湿度(%)", test.AmbientHumidity),
            ("操作员", test.Operator), ("设备", $"{test.ApparatusId} / {test.ApparatusName}"), ("试验前质量(g)", test.PreWeight),
            ("试验后质量(g)", result.PostWeight), ("失重率(%)", result.LostWeightPercent), ("样品温升(℃)", result.DeltaTf),
            ("火焰持续时间(s)", result.FlameDuration), ("判定", result.Passed ? "通过" : "不通过"), ("备注", result.Memo)
        };
        for (var i = 0; i < rows.Length; i++)
        {
            info.Cells[i + 3, 1].Value = rows[i].Name;
            info.Cells[i + 3, 2].Value = rows[i].Value;
        }
        info.Cells[info.Dimension.Address].AutoFitColumns();

        var data = package.Workbook.Worksheets.Add("温度数据");
        var headers = new[] { "Time", "TF1", "TF2", "Surface", "Center", "Calibration" };
        for (var i = 0; i < headers.Length; i++) data.Cells[1, i + 1].Value = headers[i];
        for (var i = 0; i < samples.Count; i++)
        {
            var r = i + 2;
            data.Cells[r, 1].Value = samples[i].TimeSeconds;
            data.Cells[r, 2].Value = samples[i].Tf1;
            data.Cells[r, 3].Value = samples[i].Tf2;
            data.Cells[r, 4].Value = samples[i].Surface;
            data.Cells[r, 5].Value = samples[i].Center;
            data.Cells[r, 6].Value = samples[i].Calibration;
        }
        data.Cells[data.Dimension.Address].AutoFitColumns();

        var chartSheet = package.Workbook.Worksheets.Add("温度曲线");
        var chart = chartSheet.Drawings.AddChart("TemperatureCurve", eChartType.Line) as ExcelLineChart;
        chart!.Title.Text = "温度曲线";
        chart.SetPosition(1, 0, 1, 0);
        chart.SetSize(1000, 560);
        var lastRow = samples.Count + 1;
        if (samples.Count > 1)
        {
            chart.Series.Add(data.Cells[$"B2:B{lastRow}"], data.Cells[$"A2:A{lastRow}"]).Header = "炉温1";
            chart.Series.Add(data.Cells[$"C2:C{lastRow}"], data.Cells[$"A2:A{lastRow}"]).Header = "炉温2";
            chart.Series.Add(data.Cells[$"D2:D{lastRow}"], data.Cells[$"A2:A{lastRow}"]).Header = "表面温";
            chart.Series.Add(data.Cells[$"E2:E{lastRow}"], data.Cells[$"A2:A{lastRow}"]).Header = "中心温";
        }
        package.SaveAs(new FileInfo(file));
        return file;
    }

    public string? ExportPdfReport(TestSession test, TestResult result, IReadOnlyList<TemperatureSample> samples)
    {
        try
        {
            var outDir = PathResolver.Resolve(_settings.Report.OutputDirectory);
            Directory.CreateDirectory(outDir);
            var file = Path.Combine(outDir, $"{test.TestId}_报告.pdf");

            var doc = new Document();
            doc.Info.Title = "ISO 11820 试验报告";
            var section = doc.AddSection();
            section.PageSetup.TopMargin = Unit.FromCentimeter(1.5);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(1.5);

            // 标题
            var title = section.AddParagraph("ISO 11820 建筑材料不燃性试验报告");
            title.Format.Font.Size = 18;
            title.Format.Font.Bold = true;
            title.Format.SpaceAfter = Unit.FromCentimeter(0.5);

            // 试验信息表
            var table = section.AddTable();
            table.Borders.Width = 0.5;
            table.AddColumn(Unit.FromCentimeter(4));
            table.AddColumn(Unit.FromCentimeter(12));

            void AddRow(string k, string v)
            {
                var row = table.AddRow();
                row.Cells[0].AddParagraph(k);
                row.Cells[1].AddParagraph(v);
            }

            AddRow("样品编号", test.ProductId);
            AddRow("试验ID", test.TestId);
            AddRow("样品名称", test.ProductName);
            AddRow("规格型号", test.Specific);
            AddRow("操作员", test.Operator);
            AddRow("试验依据", test.According);
            AddRow("环境温度", $"{test.AmbientTemperature:F1} ℃");
            AddRow("环境湿度", $"{test.AmbientHumidity:F1} %");
            AddRow("试验前质量", $"{test.PreWeight:F2} g");
            AddRow("试验后质量", $"{result.PostWeight:F2} g");
            AddRow("失重率", $"{result.LostWeightPercent:F2} %");
            AddRow("样品温升", $"{result.DeltaTf:F2} ℃");
            AddRow("炉温1温升", $"{result.DeltaTf1:F2} ℃");
            AddRow("炉温2温升", $"{result.DeltaTf2:F2} ℃");
            AddRow("表面温升", $"{result.DeltaTs:F2} ℃");
            AddRow("中心温升", $"{result.DeltaTc:F2} ℃");
            AddRow("火焰持续时间", $"{result.FlameDuration} s");
            AddRow("试验时长", $"{result.TotalTestTime} s");
            AddRow("判定结果", result.Passed ? "通过" : "不通过");
            AddRow("备注", result.Memo ?? "");

            // 温度曲线图
            section.AddParagraph().AddLineBreak();
            var chartTitle = section.AddParagraph("温度曲线");
            chartTitle.Format.Font.Size = 14;
            chartTitle.Format.Font.Bold = true;
            chartTitle.Format.SpaceAfter = Unit.FromCentimeter(0.3);

            var chartImagePath = GenerateChartImage(samples);
            if (chartImagePath != null && File.Exists(chartImagePath))
            {
                section.AddImage(chartImagePath);
                File.Delete(chartImagePath); // 清理临时文件
            }
            else
            {
                section.AddParagraph("（曲线图生成失败，请参阅 Excel 报告）");
            }

            var renderer = new PdfDocumentRenderer { Document = doc };
            renderer.RenderDocument();
            renderer.PdfDocument.Save(file);
            return file;
        }
        catch (Exception ex)
        {
            // PDF生成失败不影响主流程，记录日志后返回null
            Log.Warning(ex, "PDF export failed, skipping PDF generation");
            return null;
        }
    }

    /// <summary>
    /// 使用 OxyPlot 生成温度曲线图 PNG 临时文件，返回文件路径。
    /// </summary>
    private string? GenerateChartImage(IReadOnlyList<TemperatureSample> samples)
    {
        try
        {
            if (samples.Count < 2) return null;

            var model = new PlotModel { Title = "温度曲线" };
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "时间(s)",
                Minimum = 0,
                Maximum = samples.Last().TimeSeconds
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "温度(℃)",
                Minimum = 0,
                Maximum = 800
            });

            var tf1 = new LineSeries { Title = "炉温1", StrokeThickness = 1.5 };
            var tf2 = new LineSeries { Title = "炉温2", StrokeThickness = 1.5 };
            var ts = new LineSeries { Title = "表面温", StrokeThickness = 1.5 };
            var tc = new LineSeries { Title = "中心温", StrokeThickness = 1.5 };

            foreach (var s in samples)
            {
                tf1.Points.Add(new DataPoint(s.TimeSeconds, s.Tf1));
                tf2.Points.Add(new DataPoint(s.TimeSeconds, s.Tf2));
                ts.Points.Add(new DataPoint(s.TimeSeconds, s.Surface));
                tc.Points.Add(new DataPoint(s.TimeSeconds, s.Center));
            }

            model.Series.Add(tf1);
            model.Series.Add(tf2);
            model.Series.Add(ts);
            model.Series.Add(tc);
            model.Legends.Add(new OxyPlot.Legends.Legend
            {
                LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
                LegendPlacement = OxyPlot.Legends.LegendPlacement.Outside,
                LegendOrientation = OxyPlot.Legends.LegendOrientation.Horizontal
            });

            var tmpFile = Path.Combine(Path.GetTempPath(), $"iso11820_chart_{Guid.NewGuid():N}.png");
            using var stream = File.Create(tmpFile);
            var exporter = new OxyPlot.WindowsForms.PngExporter { Width = 900, Height = 400 };
            exporter.Export(model, stream);
            return tmpFile;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Chart image generation failed");
            return null;
        }
    }

    public string ExportQueryResult(IEnumerable<TestRecordSummary> rows)
    {
        var outDir = PathResolver.Resolve(_settings.Report.OutputDirectory);
        Directory.CreateDirectory(outDir);
        var file = Path.Combine(outDir, $"QueryResult_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Query Result");
        var headers = new[] { "Product ID", "Test ID", "Date", "Operator", "Product Name", "Weight Loss %", "Temp Rise", "Duration", "Status", "Result" };
        for (var i = 0; i < headers.Length; i++) ws.Cells[1, i + 1].Value = headers[i];
        var r = 2;
        foreach (var item in rows)
        {
            ws.Cells[r, 1].Value = item.ProductId;
            ws.Cells[r, 2].Value = item.TestId;
            ws.Cells[r, 3].Value = item.TestDate.ToString("yyyy-MM-dd");
            ws.Cells[r, 4].Value = item.Operator;
            ws.Cells[r, 5].Value = item.ProductName;
            ws.Cells[r, 6].Value = item.LostWeightPercent;
            ws.Cells[r, 7].Value = item.DeltaTf;
            ws.Cells[r, 8].Value = item.TotalTestTime;
            ws.Cells[r, 9].Value = item.Flag == "10000000" ? "Saved" : "Unsaved";
            ws.Cells[r, 10].Value = item.ResultDisplay;
            r++;
        }
        ws.Cells[ws.Dimension.Address].AutoFitColumns();
        package.SaveAs(new FileInfo(file));
        return file;
    }
}

/// <summary>
/// 简化的字体解析器 - 使用系统 Arial 字体
/// </summary>
public class SimpleFontResolver : IFontResolver
{
    private byte[]? _arialFont;
    private readonly object _lock = new();

    public string DefaultFontName => "Arial";

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        // 统一使用 Arial
        return new FontResolverInfo("Arial", isBold, isItalic);
    }

    public byte[] GetFont(string faceName)
    {
        lock (_lock)
        {
            if (_arialFont != null) return _arialFont;

            try
            {
                var fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
                var fontPath = Path.Combine(fontsDir, "arial.ttf");
                if (File.Exists(fontPath))
                {
                    _arialFont = File.ReadAllBytes(fontPath);
                    return _arialFont;
                }
            }
            catch
            {
                // ignore
            }

            return Array.Empty<byte>();
        }
    }
}
