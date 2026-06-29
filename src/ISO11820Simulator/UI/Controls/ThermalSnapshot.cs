using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ISO11820Simulator.UI.Controls;

/// <summary>
/// 纯 UI 数据帧：主界面收到 DataBroadcast 后转换成这个结构，再分发给动态控件。
/// 这样控件不直接依赖控制器，后续要替换数据源也更容易。
/// </summary>
public enum ThermalVisualState
{
    Idle,
    Preparing,
    Ready,
    Recording,
    Complete
}

public readonly record struct ThermalSnapshot(
    double Tf1,
    double Tf2,
    double Surface,
    double Center,
    double Calibration,
    double DriftTf1Per10Min,
    double DriftTf2Per10Min,
    int StableTicks,
    int RecordSeconds,
    string SampleNo,
    string StateText,
    ThermalVisualState State
);
