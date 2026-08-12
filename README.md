<p align="center">
  <img src="https://raw.githubusercontent.com/wobushiafa/Aviora/main/assets/aviora.svg" width="168" height="168" alt="Aviora logo" />
</p>

# Aviora

简体中文 | [English](https://github.com/wobushiafa/Aviora/blob/main/README.en.md)

Aviora 是一个面向 [Avalonia](https://avaloniaui.net/) 的现代化、跨平台开源控件库，提供图表、仪表、加载指示器、抽屉、对话框、Toast 通知和通用容器控件。

> 项目目前处于早期开发阶段，首个稳定版本发布前公共 API 仍可能调整。

## 控件

- `AvioraCard`：用于组织相关内容的主题化容器。
- `ColumnChart`：支持阈值、坐标轴、选择、动画和 Tooltip 的柱状图。
- `LineChart`：支持平滑曲线、区域填充、数据点和交互的折线图。
- `Thermometer`：支持范围、刻度、标签、渐变映射和过渡动画的温度计。
- `DialGauge`：支持分段刻度、标签、指针和过渡动画的圆形仪表。
- `ProgressRing`：支持确定进度和不确定动画的环形进度条。
- `Loading`：内置 Ring、Dots、Pulse、Bars、Wave、Orbit 和 DoubleRing 样式，并支持自定义内容。
- `LoadingOverlay`：支持异步作用域、并发请求、多宿主路由和 MVVM 服务调用的全局加载遮罩。
- `Drawer`：支持多方向、遮罩、Push/Overlay 模式及异步服务调用的抽屉。
- `Dialog`：支持异步结果、会话关闭、请求排队、多宿主路由和自定义内容的模态对话框。
- `ToastHost`：支持并发堆叠、六方位显示、超时/动作/取消关闭、动画和外部模板的全局通知宿主。

## 安装

```powershell
dotnet add package Aviora.Controls
```

只需要与 UI 框架无关的图表模型和算法时，可以引用：

```powershell
dotnet add package Aviora.Core
```

只需要在 ViewModel 中使用抽屉等展示服务契约时，可以引用：

```powershell
dotnet add package Aviora.Presentation.Abstractions
```

## 配置

在 `App.axaml` 中加载控件主题：

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceInclude Source="avares://Aviora.Controls/Themes/Generic.axaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

在使用控件的 AXAML 文件中声明命名空间：

```xml
xmlns:aviora="https://github.com/wobushiafa/Aviora"
```

## 使用示例

### AvioraCard

```xml
<aviora:AvioraCard Padding="20">
  <StackPanel Spacing="8">
    <TextBlock FontSize="18" FontWeight="SemiBold" Text="系统状态" />
    <TextBlock Opacity="0.65" Text="所有服务运行正常" />
  </StackPanel>
</aviora:AvioraCard>
```

### ColumnChart

```xml
<aviora:ColumnChart Height="280"
                    ItemsSource="{Binding Sales}"
                    AutoRange="True"
                    XAxisLabelMode="All"
                    IsToolTipEnabled="True"
                    SelectedItem="{Binding SelectedSale}" />
```

```csharp
public IReadOnlyList<IChartDataPoint> Sales { get; } =
[
    new ChartDataPoint { Label = "一月", Value = 42, ToolTip = "一月：42" },
    new ChartDataPoint { Label = "二月", Value = 56, ToolTip = "二月：56" },
    new ChartDataPoint { Label = "三月", Value = 71, ToolTip = "三月：71" },
];
```

### LineChart

```xml
<aviora:LineChart Height="280"
                  Values="{Binding TrendValues}"
                  XAxisLabelsSource="{Binding TrendLabels}"
                  InterpolationMode="Smooth"
                  ShowPoints="True"
                  AreaFillBrush="#332563EB"
                  IsAnimationEnabled="True" />
```

### Thermometer

```xml
<aviora:Thermometer Width="120"
                    Height="300"
                    Minimum="-20"
                    Maximum="120"
                    Value="{Binding Temperature}"
                    TickCount="7"
                    ShowTickLabels="True"
                    TickLabelFormat="0"
                    LiquidBrushMappingMode="FullRange" />
```

### DialGauge

```xml
<aviora:DialGauge Width="280"
                  Height="220"
                  Minimum="0"
                  Maximum="100"
                  Value="{Binding Usage}"
                  TickCount="11"
                  ShowTickLabels="True"
                  TickColorMode="Range" />
```

### Loading

环形进度条支持确定值，也可以切换为与 Loading Ring 一致的不确定动画：

```xml
<aviora:ProgressRing Width="52"
                     Height="52"
                     Value="65"
                     StrokeThickness="5" />

<aviora:ProgressRing IsIndeterminate="True" />
```

使用内置样式，并按需设置颜色、尺寸、粗细和动画周期：

```xml
<aviora:Loading Width="44"
                Height="44"
                IndicatorStyle="Dots"
                IndicatorBrush="#0F766E"
                StrokeThickness="4"
                AnimationDuration="0:0:0.7" />
```

设置 `Content`（可搭配 `ContentTemplate`）时会替换内置绘制，因此可以使用任意 Avalonia 控件：

```xml
<aviora:Loading Width="180" Height="6">
  <ProgressBar IsIndeterminate="True" />
</aviora:Loading>
```

通过 `IsActive="False"` 可以停止动画并隐藏指示器。

#### 全局加载遮罩

将 `LoadingOverlay` 放在窗口最外层；它会拦截遮罩区域的输入，并覆盖内部的页面、Drawer 和 Dialog：

```xml
<aviora:LoadingOverlay x:Name="LoadingHost"
                       ShowDelay="0:0:0.1"
                       CloseDelay="0:0:0.2"
                       MinimumShowDuration="0:0:0.35">
  <!-- 页面主要内容 -->
</aviora:LoadingOverlay>
```

在组合根创建服务并连接宿主，然后把 `ILoadingService` 注入 ViewModel：

```csharp
var loadingService = new LoadingService();
LoadingHost.Service = loadingService;
DataContext = new MainViewModel(loadingService);
```

ViewModel 只依赖 `Aviora.Presentation.Loadings`。`RunAsync` 会在成功、异常或取消时自动关闭本次加载会话：

```csharp
public sealed class MainViewModel(ILoadingService loadingService)
{
    public Task RefreshAsync(CancellationToken cancellationToken) =>
        loadingService.RunAsync(
            token => RefreshDataAsync(token),
            new LoadingRequest("正在刷新数据"),
            cancellationToken);
}
```

需要跨越多段代码时，可以使用手动作用域：

```csharp
using ILoadingSession loading = loadingService.Show(
    new LoadingRequest("正在同步工作区"));
await SynchronizeAsync();
```

多个会话可以重叠；任一会话结束只关闭自身，最后一个会话结束后遮罩才消失。`HostId` 可用于多窗口路由，`LoadingContentTemplate` 可展示自定义消息 ViewModel，且内容的字体、颜色、间距完全由该模板或内容控件决定。`ShowDelay` 用于过滤短任务，`MinimumShowDuration` 保证遮罩一旦出现后的最短展示时间，`CloseDelay` 则让最后一个任务结束后延迟关闭；等待关闭期间出现新任务时，旧关闭计时会自动取消。

### Drawer

在视图中放置 Drawer 宿主：

```xml
<aviora:Drawer x:Name="DrawerHost"
               Placement="Right"
               DrawerSize="380">
  <Grid>
    <!-- 页面主要内容 -->
  </Grid>
</aviora:Drawer>
```

未设置 `DrawerSize` 时，Drawer 会按内容在弹出方向的期望尺寸自动测量；左右方向测量宽度，上下方向测量高度。设置具体数值时保持固定尺寸。遮罩使用 `OverlayBrush`；抽屉承载面使用统一的 `SurfaceBackground`、`SurfaceBorderBrush`、`SurfaceBorderThickness`、`SurfaceCornerRadius`、`SurfaceBoxShadow`、`SurfacePadding` 和 `SurfaceMargin`。`SurfaceBackground` 默认白色，也可设置为任意画刷或 `Transparent`。

在应用组合根创建 Avalonia 实现，把宿主接口交给 View，并把客户端接口注入 ViewModel：

```csharp
var drawerService = new DrawerService();
DrawerHost.Service = drawerService;
DataContext = new MainWindowViewModel(drawerService);
```

ViewModel 只依赖 `Aviora.Presentation.Drawers`：

```csharp
using Aviora.Presentation.Drawers;

public sealed class SettingsViewModel(IDrawerService drawerService)
{
    public Task<DrawerResult> OpenAsync() =>
        drawerService.ShowAsync(new DrawerRequest(this)
        {
            Placement = DrawerPlacement.Right,
            Size = 380,
        });
}
```

### Dialog

在窗口最外层放置 Dialog 宿主，并在组合根连接服务：

```xml
<aviora:Dialog x:Name="DialogHost">
  <!-- 页面主要内容 -->
</aviora:Dialog>
```

```csharp
var dialogService = new DialogService(new DialogOptions
{
    IsAnimationEnabled = true,
    IsEscapeKeyEnabled = false,
    IsLightDismissEnabled = false,
    IsOverlayVisible = true,
});
DialogHost.Service = dialogService;
DataContext = new MainWindowViewModel(dialogService);
```

以上也是内建默认值；单次 `DialogRequest` 仍可按需覆盖这些全局设置。遮罩使用 `OverlayBrush`；对话框承载面使用统一的 `SurfaceBackground`、`SurfaceBorderBrush`、`SurfaceBorderThickness`、`SurfaceCornerRadius`、`SurfaceBoxShadow`、`SurfacePadding` 和 `SurfaceMargin`。`SurfaceBackground` 默认白色，也可设置为任意画刷或 `Transparent`。

ViewModel 可以直接展示内容；需要从内容内部关闭并返回结果时使用会话工厂：

```csharp
await dialogService.ShowAsync(new MessageViewModel());

DialogResult result = await dialogService.ShowAsync(
    session => new EditProfileViewModel(session));
```

Dialog 内需要继续弹出子 Dialog 时，可使用 `Navigate` 只显示当前层，或使用 `Stack` 将子 Dialog 叠加在父层之上。关闭后都会恢复父 Dialog；未设置时仍按原顺序排队：

```csharp
DialogResult childResult = await dialogService.ShowAsync(new DialogRequest(childViewModel)
{
    PresentationMode = DialogPresentationMode.Stack, // or Navigate
});
```

### Toast

将 `ToastHost` 放在窗口最外层并连接全局服务。宿主默认位于右上角，通知显示 4 秒，默认不限制同时显示的数量；设置 `MaxVisible` 为正数即可启用排队限制：

```xml
<aviora:ToastHost x:Name="ToastHost"
                  Placement="TopRight"
                  MaxVisible="0"
                  IsClickDismissEnabled="True"
                  AnimationDuration="0:0:0.22"
                  ExitAnimationDuration="0:0:0.15">
  <!-- 页面、Drawer、Dialog 和 LoadingOverlay -->
</aviora:ToastHost>
```

```csharp
var toastService = new ToastService();
ToastHost.Service = toastService;

toastService.ShowSuccess("配置已同步", "保存成功");
toastService.ShowError("请检查网络后重试", "上传失败", ToastPlacement.BottomRight);
```

请求可以覆盖位置、时长、是否允许关闭或点击内容关闭，并提供动作按钮；返回的会话可主动关闭，也可等待具体关闭原因：

```csharp
IToastSession session = toastService.Show(new ToastRequest("文件已移入归档")
{
    Title = "已归档",
    Severity = ToastSeverity.Information,
    Placement = ToastPlacement.BottomCenter,
    Duration = Timeout.InfiniteTimeSpan,
    IsClickDismissEnabled = false,
    ActionText = "撤销",
    ActionCommand = UndoCommand,
});

ToastDismissReason reason = await session.Completion;
```

设置 `ToastTemplate` 可替换通知内容区域；设置 `ToastTheme` 可替换整条通知的 ControlTheme。默认点击通知的非交互内容区域即可关闭；动作按钮和关闭按钮不会触发该行为，复杂自定义内容可设置 `IsClickDismissEnabled="False"`。自定义主题可使用 `:information`、`:success`、`:warning`、`:error`、`:dismissible`、`:actionable` 和 `:untitled` 伪类。`AnimationDuration` 控制进入时长，`ExitAnimationDuration` 控制更快的退出时长，`ReflowAnimationDuration` 控制新增或关闭通知后其余通知的平滑重排；将 `IsAnimationEnabled` 绑定到应用的“减少动态效果”设置即可关闭位移和淡入淡出。

## 从源码构建

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet run --project samples/Aviora.Demo
```

更多内容参见 [文档目录](https://github.com/wobushiafa/Aviora/tree/main/docs) 和 Demo 应用。

## 参与贡献

提交 Issue 或 Pull Request 前，请阅读 [CONTRIBUTING.md](https://github.com/wobushiafa/Aviora/blob/main/CONTRIBUTING.md)。

## 许可证

Aviora 使用 [MIT License](https://github.com/wobushiafa/Aviora/blob/main/LICENSE)。
