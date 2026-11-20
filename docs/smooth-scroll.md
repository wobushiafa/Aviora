# SmoothScrollAssist

`SmoothScrollAssist` 是一组适用于 Avalonia 控件的通用附加属性（Attached Properties），可为离散鼠标滚轮提供帧率无关的平滑滚动插值，同时支持高精度触控板直通、纵/横双轴独立或协同插值、内置手感预设以及全参数微调。

除了直接挂载在 `ScrollViewer` 上，`SmoothScrollAssist` 也支持直接挂载在 `ListBox`、`TreeView`、`ItemsControl`、`DataGrid` 等内部包含 `ScrollViewer` 的复合控件上，无需修改原有控件模板。

## 快速上手

先在 XAML 根元素声明 Aviora 命名空间：

```xml
xmlns:aviora="https://github.com/wobushiafa/Aviora"
```

### 1. 在 `ScrollViewer` 上启用并选择预设

```xml
<ScrollViewer aviora:SmoothScrollAssist.IsEnabled="True"
              aviora:SmoothScrollAssist.Preset="Silky">
  <!-- 滚动内容 -->
</ScrollViewer>
```

### 2. 直接挂载在 `ListBox` / `TreeView` 等复合控件

```xml
<ListBox ItemsSource="{Binding Items}"
         aviora:SmoothScrollAssist.IsEnabled="True"
         aviora:SmoothScrollAssist.StepSize="120"
         aviora:SmoothScrollAssist.DecayRate="16" />
```

### 3. 通过全局 `Style` 一键启用

```xml
<Application.Styles>
  <FluentTheme />
  <Style Selector="ScrollViewer">
    <Setter Property="aviora:SmoothScrollAssist.IsEnabled" Value="True" />
    <Setter Property="aviora:SmoothScrollAssist.Preset" Value="Default" />
  </Style>
</Application.Styles>
```

在某一个特定 `ScrollViewer` 或 `ListBox` 上显式设置的附加属性会自动覆盖全局样式中的同名参数或 `Preset` 默认值。

### 4. 纯横向画廊（普通滚轮自动横向滚动）

```xml
<ScrollViewer HorizontalScrollBarVisibility="Auto"
              VerticalScrollBarVisibility="Disabled"
              aviora:SmoothScrollAssist.IsEnabled="True"
              aviora:SmoothScrollAssist.AutoHorizontalWhenVerticalDisabled="True" />
```

### 5. 使用固定时长 + 缓动曲线（`Easing` 模式）

```xml
<ScrollViewer aviora:SmoothScrollAssist.IsEnabled="True"
              aviora:SmoothScrollAssist.CurveMode="Easing"
              aviora:SmoothScrollAssist.Duration="0:0:0.26">
  <aviora:SmoothScrollAssist.Easing>
    <CubicEaseOut />
  </aviora:SmoothScrollAssist.Easing>
  <!-- 滚动内容 -->
</ScrollViewer>
```

## 附加属性一览

所有附加属性均注册在 `Control` 上，既可设置于 `ScrollViewer`，也可设置于封装了 `ScrollViewer` 的任意复合控件：

| 属性 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `IsEnabled` | `bool` | `false` | 启用或关闭平滑滚动。设为 `false` 时立即解除事件监听与动画调度。 |
| `Preset` | `SmoothScrollPreset` | `Default` | 内置手感预设（`Default`、`Snappy`、`Silky`）。任何显式设置的细粒度参数都会优先于预设基准值。 |
| `CurveMode` | `SmoothScrollCurveMode` | `Exponential` | 插值曲线模式：`Exponential`（指数阻尼）或 `Easing`（固定时长 + 缓动函数）。 |
| `Orientation` | `SmoothScrollOrientation` | `Both` | 控制平滑插值生效的轴向：`Both`、`Vertical` 或 `Horizontal`。 |
| `StepSize` | `double` | `96` | 单格滚轮步长（单位 DIP，必须为正有限值）。 |
| `InitialStepFactor` | `double` | `0.22` | `Exponential` 模式下滚轮触发瞬间推进的初始比例，范围 `[0, 1]`。设为 `0` 可完全交由动画帧推进。 |
| `DecayRate` | `double` | `18.0` | `Exponential` 模式下的指数阻尼衰减率（必须 > 0）。值越大收敛越快、越跟手；值越小滑行越柔顺。 |
| `MinSpeed` | `double` | `48.0` | `Exponential` 模式尾段最低收敛速度（DIP/秒，必须 >= 0），避免动画尾端拖曳过久。 |
| `Duration` | `TimeSpan` | `00:00:00.220` | `Easing` 模式下的动画持续时长（必须 >= 0）。 |
| `Easing` | `Easing?` | `null`（回退为 `CubicEaseOut`） | `Easing` 模式下使用的 Avalonia 缓动函数。 |
| `SettleThreshold` | `double` | `0.75` | 距目标点小于等于该距离（DIP，必须 > 0）时直接吸附到精确目标并结束帧调度。 |
| `MaxQueueDistance` | `double` | `PositiveInfinity` | 快速连滚时允许提前累积在当前位置前方的最大距离上限（DIP）。 |
| `TrackpadThreshold` | `double` | `0.5` | 滚轮 `Delta` 绝对值小于该阈值时视为高精度触控板微步事件，绕过插值直接应用偏移（设为 `0` 可禁用直通）。 |
| `IsShiftWheelHorizontalEnabled` | `bool` | `true` | 按住 `Shift` 滚动纵向滚轮时，是否自动映射为横向平滑滚动。 |
| `AutoHorizontalWhenVerticalDisabled` | `bool` | `false` | 当控件纵向不可滚动且横向可滚动时，是否将普通纵向滚轮自动映射为横向平滑滚动。 |
| `RespectNestedScrollViewers` | `bool` | `true` | 是否优先让内部仍可滚动的子 `ScrollViewer` 响应；内层触边后自动由外层平滑接管。 |

## 内置预设对照表

`Preset` 仅在未显式覆盖对应属性时提供基准物理参数，因此可以在选择 `Silky` 后单独调整 `StepSize` 等任意单项：

| 预设 (`SmoothScrollPreset`) | `StepSize` | `InitialStepFactor` | `DecayRate` | `MinSpeed` | `SettleThreshold` | `Duration` | 手感特征 |
|---|---|---|---|---|---|---|---|
| `Default` | `96` | `0.22` | `18.0` | `48.0` | `0.75` | `220 ms` | 响应与阻尼均衡，适合绝大多数桌面页面 |
| `Snappy` | `96` | `0.32` | `26.0` | `72.0` | `0.75` | `150 ms` | 起步干脆、低延迟、几乎无拖尾，适合高频列表查阅 |
| `Silky` | `112` | `0.14` | `11.5` | `32.0` | `0.5` | `340 ms` | 更长的柔顺惯性滑行，适合阅读长文、画廊或展示页 |

## 双曲线模式选型建议

- **`Exponential`（默认推荐）**：采用帧率无关的指数阻尼积分 `1 - exp(-DecayRate * dt)`。连续快速拨动滚轮时速度曲线自然衔接，不会在每次输入时重新从零加速，适合高频率、不规则连滚操作。
- **`Easing`**：每次滚轮事件根据设定 `Duration` 与 `Easing`（例如 `CubicEaseOut`、`SineEaseOut`、`ExponentialEaseOut`）在起止偏移之间进行确定性插值，适合追求严格固定动画时长或特定品牌动效曲线的界面。

## 交互细节与性能说明

- **自动放行快捷键**：`Ctrl` / `Alt` / `Meta` 修饰键滚轮事件一律放行，不干扰缩放或其他快捷键。
- **零抢手打断**：当用户拖拽滚动条滑块、使用键盘翻页或通过代码直接修改 `ScrollViewer.Offset` 时，正在进行的插值会立即取消并同步到当前实际偏移。
- **边界滚动链**：当当前 `ScrollViewer` 已滚到边界且目标不再变化时，不会吞掉 `PointerWheelChanged` 事件，使外层容器仍能自然接管滚动。
- **帧调度按需启用**：仅在动画进行期间向宿主 `TopLevel.RequestAnimationFrame` 请求帧回调；动画收敛或控件脱离视觉树后立即停止调度，空闲状态下零额外 CPU 占用。
