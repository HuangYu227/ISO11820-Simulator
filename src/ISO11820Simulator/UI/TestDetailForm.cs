namespace ISO11820Simulator.UI;

public sealed class TestDetailForm : Form
{
    private static readonly Dictionary<string, string> FieldNames = new()
    {
        ["productid"] = "样品编号",
        ["testid"] = "试验ID",
        ["testdate"] = "试验日期",
        ["ambtemp"] = "环境温度(℃)",
        ["ambhumi"] = "环境湿度(%)",
        ["according"] = "试验依据",
        ["operator"] = "操作员",
        ["apparatusid"] = "设备编号",
        ["apparatusname"] = "设备名称",
        ["apparatuschkdate"] = "设备检定日期",
        ["rptno"] = "报告编号",
        ["preweight"] = "试验前质量(g)",
        ["postweight"] = "试验后质量(g)",
        ["lostweight"] = "失重量(g)",
        ["lostweight_per"] = "失重率(%)",
        ["totaltesttime"] = "总试验时长(s)",
        ["constpower"] = "恒功率值",
        ["phenocode"] = "现象编码",
        ["flametime"] = "火焰发生时刻(s)",
        ["flameduration"] = "火焰持续时间(s)",
        ["maxtf1"] = "炉温1最大值(℃)",
        ["maxtf2"] = "炉温2最大值(℃)",
        ["maxts"] = "表面温最大值(℃)",
        ["maxtc"] = "中心温最大值(℃)",
        ["maxtf1_time"] = "炉温1最大值时刻(s)",
        ["maxtf2_time"] = "炉温2最大值时刻(s)",
        ["maxts_time"] = "表面温最大值时刻(s)",
        ["maxtc_time"] = "中心温最大值时刻(s)",
        ["finaltf1"] = "最终炉温1(℃)",
        ["finaltf2"] = "最终炉温2(℃)",
        ["finalts"] = "最终表面温(℃)",
        ["finaltc"] = "最终中心温(℃)",
        ["finaltf1_time"] = "最终炉温1时刻(s)",
        ["finaltf2_time"] = "最终炉温2时刻(s)",
        ["finalts_time"] = "最终表面温时刻(s)",
        ["finaltc_time"] = "最终中心温时刻(s)",
        ["deltatf1"] = "炉温1温升(℃)",
        ["deltatf2"] = "炉温2温升(℃)",
        ["deltatf"] = "样品温升(℃)",
        ["deltats"] = "表面温温升(℃)",
        ["deltatc"] = "中心温温升(℃)",
        ["memo"] = "备注",
        ["flag"] = "保存标记",
    };

    public TestDetailForm(Dictionary<string, object?> detail)
    {
        Text = "试验详情";
        ClientSize = new Size(760, 680);
        MinimumSize = new Size(600, 500);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Background;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;

        var grid = new DataGridView { Dock = DockStyle.Fill };
        Theme.StyleGrid(grid);
        Controls.Add(grid);

        var displayList = detail
            .Where(kv => FieldNames.ContainsKey(kv.Key))
            .Select(kv => new
            {
                字段 = FieldNames[kv.Key],
                值 = FormatValue(kv.Key, kv.Value)
            })
            .ToList();
        grid.DataSource = displayList;
    }

    private static string FormatValue(string key, object? value)
    {
        if (value == null) return "--";
        var s = value.ToString() ?? "";
        if (key.Contains("weight") && double.TryParse(s, out var w)) return $"{w:F2} g";
        if ((key.Contains("temp") || key.StartsWith("delta")) && double.TryParse(s, out var t)) return $"{t:F2} ℃";
        if (key.EndsWith("_time") && int.TryParse(s, out var sec)) return $"{sec} s";
        if (key == "lostweight_per" && double.TryParse(s, out var p)) return $"{p:F2} %";
        if (key == "flag") return s == "10000000" ? "已保存" : s;
        return s;
    }
}
