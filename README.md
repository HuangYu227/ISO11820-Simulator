# ISO 11820 建筑材料不燃性试验仿真系统

这是一个基于 **C# / .NET 8 / WinForms / SQLite** 的本地桌面仿真系统，用于模拟 ISO 11820 建筑材料不燃性试验流程。系统不依赖真实串口、炉体、摄像头或网络服务，温度数据由软件仿真生成，适合课程工程实践、课堂演示和答辩展示。

## 一、项目已实现功能

| 模块 | 功能说明 |
|---|---|
| 登录模块 | 管理员 / 试验员角色选择；默认账号：admin / 123456、experimenter / 123456 |
| 数据库模块 | 首次运行自动创建 SQLite 数据库、6 张业务表、初始账号、设备和传感器数据 |
| 新建试验 | 填写样品编号、试验ID、样品名称、规格、尺寸、环境温湿度、试验前质量和时长 |
| 温度仿真 | 5 通道温度：炉温1、炉温2、表面温、中心温、校准温；每 800ms 更新 |
| 状态机 | Idle、Preparing、Ready、Recording、Complete 五态流转 |
| 实时显示 | 温度卡片、状态标签、计时器、温漂、系统消息日志、OxyPlot 实时曲线 |
| 试验记录 | 记录火焰现象、火焰发生时刻、持续时间、试验后质量、备注 |
| 结果计算 | 自动计算失重量、失重率、温升、最大值、最终值、判定结论 |
| 数据导出 | 自动/手动生成 CSV、Excel、PDF；支持查询结果导出为 Excel |
| 历史查询 | 按日期范围、样品编号、操作员筛选；双击查看完整详情 |
| 设备校准 | 显示校准温、记录多个校准点、保存校准历史 |
| 参数设置 | 修改目标炉温、升温速度、温度波动、稳定阈值、演示时长和基础目录 |
| 日志 | Serilog 写入本地滚动日志，便于调试和答辩说明 |

## 二、项目结构

```text
ISO11820-Simulator/
├── ISO11820Simulator.sln
├── README.md
├── docs/
│   ├── 功能清单与前后端交互设计.md
│   └── 基础测试清单.md
├── scripts/
│   └── run.ps1
└── src/
    └── ISO11820Simulator/
        ├── ISO11820Simulator.csproj
        ├── appsettings.json
        ├── Program.cs
        ├── App/
        │   └── GlobalApp.cs
        ├── Config/
        │   ├── AppConfig.cs
        │   └── AppSettings.cs
        ├── Data/
        │   └── DbHelper.cs
        ├── Models/
        ├── Services/
        │   ├── SensorSimulator.cs
        │   ├── TestController.cs
        │   ├── TrendService.cs
        │   └── ExportService.cs
        └── UI/
            ├── LoginForm.cs
            ├── MainForm.cs
            ├── NewTestForm.cs
            ├── PhenomenonForm.cs
            ├── SettingsForm.cs
            ├── TestDetailForm.cs
            └── Theme.cs
```

## 三、环境配置

### 1. 操作系统

推荐：

- Windows 10 / Windows 11
- Visual Studio 2022
- .NET 8 SDK

> WinForms 是 Windows 桌面 UI 技术，因此最终运行建议在 Windows 环境中完成。

### 2. 安装 .NET 8 SDK

进入 .NET 官网下载安装 .NET 8 SDK。安装后在 PowerShell 中检查：

```powershell
dotnet --version
```

如果输出 `8.x.x`，说明 SDK 安装成功。

### 3. 还原依赖

项目依赖都写在 `src/ISO11820Simulator/ISO11820Simulator.csproj` 中，首次运行前执行：

```powershell
dotnet restore .\ISO11820Simulator.sln
```

依赖包括：

| 依赖 | 用途 |
|---|---|
| Microsoft.Data.Sqlite | SQLite 数据库访问 |
| Microsoft.Extensions.Configuration.Json | 读取 appsettings.json |
| OxyPlot.WindowsForms | WinForms 实时曲线图 |
| EPPlus | Excel 报告和曲线图导出 |
| PDFsharp-MigraDoc | PDF 报告生成 |
| Serilog / Serilog.Sinks.File | 本地日志 |
| MathNet.Numerics | 数值计算扩展，项目当前保留依赖以便后续替换温漂算法 |

### 4. 编译运行

方式一：Visual Studio 2022

1. 双击打开 `ISO11820Simulator.sln`。
2. 右键解决方案 → 还原 NuGet 包。
3. 设置 `ISO11820Simulator` 为启动项目。
4. 点击“启动”。

方式二：PowerShell

```powershell
.\scripts\run.ps1
```

或者手动执行：

```powershell
dotnet restore .\ISO11820Simulator.sln
dotnet build .\ISO11820Simulator.sln -c Debug
dotnet run --project .\src\ISO11820Simulator\ISO11820Simulator.csproj
```

## 四、默认账号

| 角色 | 用户名 | 密码 |
|---|---|---|
| 管理员 | admin | 123456 |
| 试验员 | experimenter | 123456 |

登录界面没有用户名输入框，只需要选择角色并输入密码。

## 五、配置文件说明

配置文件位置：

```text
src/ISO11820Simulator/appsettings.json
```

关键配置：

```json
{
  "Database": {
    "SqlitePath": "Data\\ISO11820.db"
  },
  "Simulation": {
    "InitialFurnaceTemp": 720.0,
    "TargetFurnaceTemp": 750.0,
    "HeatingRatePerSecond": 40.0,
    "TempFluctuation": 0.5,
    "StableThreshold": 3.0,
    "StableTickCount": 4
  },
  "Experiment": {
    "StandardDurationSeconds": 3600,
    "DemoDurationSeconds": 90,
    "UseDemoDurationByDefault": true,
    "ChartWindowSeconds": 600
  },
  "Report": {
    "OutputDirectory": "Reports",
    "EnablePdfExport": true,
    "EnableExcelExport": true
  }
}
```

说明：

- `UseDemoDurationByDefault=true`：默认演示时长较短，方便课堂答辩快速跑通。
- 要严格按标准 60 分钟试验，可在新建试验窗口勾选“标准 60 分钟模式”。
- `InitialFurnaceTemp=720` 是为了快速升到 Ready；如果想模拟真实冷炉升温，可改成 25。

## 六、前后端交互方式

本项目中“前端”是 WinForms 界面，“后端”是业务核心层、服务层和数据层。

```text
UI窗体
  ↓ 调用
TestController 试验状态机
  ↓ 调用
SensorSimulator 仿真温度
  ↓ 数据广播事件 DataBroadcast
MainForm 切回 UI 线程刷新界面
  ↓
DbHelper 写入 SQLite
ExportService 生成 CSV / Excel / PDF
```

关键原则：

1. UI 不直接生成温度数据，只调用控制器。
2. 控制器不直接操作 UI，只通过事件广播数据。
3. 数据库操作集中在 `DbHelper`，避免 SQL 到处散落。
4. 导出逻辑集中在 `ExportService`，便于后续更换报告模板。
5. UI 线程通过 `BeginInvoke(new Action(...))` 更新控件，避免跨线程异常。

## 七、界面美化设计

为了减少“作业感”和“AI味”，界面没有使用默认灰色 WinForms 样式，而是采用现代仪表盘风格：

- 深色背景 + 信息卡片。
- 温度大字号 LED 风格显示。
- 状态颜色区分：绿色就绪、蓝色记录、黄色完成、红色异常。
- 左侧固定操作区，右侧 Tab 工作区。
- 主界面实时显示曲线、消息、温漂和当前样品。
- 记录查询和校准记录统一使用深色 DataGridView。

## 八、完整演示流程

1. 启动程序。
2. 选择“管理员”，输入 `123456` 登录。
3. 点击“新建试验”，填写样品信息并保存。
4. 点击“开始升温”。
5. 温度稳定后状态自动变为“就绪”。
6. 点击“开始记录”。
7. 等待演示时长结束，或点击“停止记录”。
8. 状态变为“完成”后，点击“试验记录”。
9. 填写试验后质量、火焰信息、备注。
10. 保存后自动生成 CSV / Excel / PDF。
11. 切换到“记录查询”，查看刚才的试验记录。
12. 双击记录查看详情，或导出查询结果。

## 九、数据输出位置

默认输出目录位于程序运行目录下：

```text
Data/ISO11820.db                         SQLite 数据库
Logs/iso11820-日期.log                   程序日志
TestData/{ProductId}/{TestId}/sensor_data.csv
Reports/{TestId}_报告.xlsx
Reports/{TestId}_报告.pdf
Reports/查询结果_yyyyMMdd_HHmmss.xlsx
```

## 十、可扩展的 AI/LLM 功能

这些功能适合作为课程答辩中的“后续创新方向”，也可以继续在本项目基础上增加：

| AI 功能 | 实现思路 | 价值 |
|---|---|---|
| 自动报告摘要 | 将试验结果、温升、失重率、火焰记录传给 LLM，生成自然语言结论 | 减少人工写报告 |
| 异常温度诊断 | 将最近曲线、温漂和波动统计输入 LLM，判断传感器异常或参数异常 | 提升系统智能化 |
| 自然语言查询 | 用户输入“查上周不通过的岩棉板试验”，转换成 SQL 查询 | 提升查询体验 |
| 参数推荐 | 根据历史 Ready 时间、温漂、噪声，推荐升温速度和稳定阈值 | 提高演示效率 |
| 报告问答助手 | 对 PDF/Excel 报告进行问答，例如“为什么不通过” | 适合答辩展示 |
| 风险提示助手 | 根据样品数据和试验过程，自动提示数据缺失、质量异常、火焰持续过长 | 提升可靠性 |

建议实现方式：

- 增加 `AiAssistantService.cs`。
- 将 `testmaster` 结构化字段和 CSV 统计值拼成 Prompt。
- 支持 OpenAI、Azure OpenAI、本地 Ollama 三种后端。
- README 中保留 API Key 配置项，但不要把密钥写进代码仓库。

## 十一、注意事项

1. 这是仿真系统，不连接真实硬件。
2. SQLite 首次运行自动创建，不需要手动建库。
3. EPPlus 在非商业学习场景下设置为 `NonCommercial`。
4. 如果运行时报 PDF 字体问题，可先关闭 `EnablePdfExport`，Excel 和 CSV 不受影响。
5. 如果 Visual Studio 提示缺少 Windows targeting，请确认安装的是 .NET 8 SDK，并使用 Windows 系统打开项目。

## 十二、评分对应说明

- **技术可行性**：本地 WinForms + SQLite，无硬件依赖，适合课堂演示。
- **概览设计完整性**：UI、业务、服务、数据、导出分层清晰。
- **详细设计可靠性**：状态机保护、未保存试验保护、异常提示、日志记录。
- **源代码创新**：现代仪表盘 UI、仿真引擎、自动报告、查询导出、AI 扩展方案。
- **功能创新**：参数设置、校准历史、查询结果导出、系统消息颜色分级。
