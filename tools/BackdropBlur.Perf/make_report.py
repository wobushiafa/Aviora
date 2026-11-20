"""Generate the Markdown performance report from the retained measurements."""
import hashlib
import json
from datetime import datetime, timedelta, timezone
from pathlib import Path

root = Path(__file__).resolve().parents[2]
data_dir = root / "artifacts/backdrop-blur-perf-2026-10-08"
main = json.loads((data_dir / "paired/results.json").read_text(encoding="utf-8-sig"))
stages = json.loads((data_dir / "stages/results.json").read_text(encoding="utf-8-sig"))
hardware = json.loads((data_dir / "hardware.json").read_text(encoding="utf-8-sig"))
rows = {row["scenario"]["Name"]: row for row in main["rows"]}
labels = {
    "baseline-1080p": "1080p 背景基线",
    "radius-0": "400×240，半径 0",
    "radius-4": "400×240，半径 4",
    "radius-12": "400×240，半径 12",
    "radius-24": "400×240，半径 24",
    "radius-48": "400×240，半径 48",
    "small-200x120": "200×120，半径 12",
    "large-800x480": "800×480，半径 12",
    "four-panels": "4 个 400×240 面板",
    "nine-panels": "9 个 250×140 面板",
    "four-overlapping": "4 个重叠 400×240 面板",
    "dpi-200-percent": "200% DPI：800×480 物理面板，sigma 24，4K Surface",
    "changing-radius": "400×240，每帧半径 12/12.5 交替",
    "static-background-repaint": "400×240，固定背景但强制重绘",
    "baseline-4k": "4K 背景基线",
    "same-panel-4k-surface": "400×240 不变，仅 Surface 改为 4K",
}
single = rows["radius-12"]
four = rows["four-panels"]
nine = rows["nine-panels"]
dpi = rows["dpi-200-percent"]
fourk = rows["same-panel-4k-surface"]
churn = rows["changing-radius"]
fingerprint = hashlib.sha256((root / "src/Aviora.Controls/Surfaces/BackdropBlur.cs").read_bytes()).hexdigest()
measured_local = datetime.fromisoformat(main["measuredAtUtc"]).astimezone(timezone(timedelta(hours=8))).strftime("%Y-%m-%d %H:%M:%S UTC+8")

lines = [
    "# BackdropBlur 性能探测 · 2026-10-08",
    "",
    "本轮只增加独立探针与报告，未修改 BackdropBlur 的实现。以下数字来自真实桌面 Skia GPU 后端、Release 构建的同步渲染测量。",
    "",
    "## 主要发现",
    "",
    f"- 1080p Surface 上，单个 400×240、sigma 12 面板的配对额外耗时中位数为 **{single['medianAddedMs']:.2f} ms**；4 个面板为 **{four['medianAddedMs']:.2f} ms**，9 个小面板为 **{nine['medianAddedMs']:.2f} ms**。",
    f"- 面板仍为 400×240，仅 Surface 改为 3840×2160，额外耗时中位数升至 **{fourk['medianAddedMs']:.2f} ms**。目标 Surface 大小会影响成本，不能只按控件像素面积估算。",
    f"- 200% DPI 场景同时放大 Surface、面板物理面积与 sigma。同步场景平均 **{dpi['meanMs']:.2f} ms**、P95 **{dpi['p95Ms']:.2f} ms**，配对额外耗时中位数 **{dpi['medianAddedMs']:.2f} ms**。该场景的渲染耗时已占用较多 16.67 ms 帧预算，但这里没有计入 UI、合成及呈现，不能据此承诺 60 FPS。",
    "- 分段诊断中，截图复制和缓存图片滤镜在 4K Surface 上均比 1080p 更贵。证据支持优先检查目标 Surface、纹理复制/绘制及驱动同步路径，不能将瓶颈只归为 Gaussian 算法。两个独立诊断场景不能相加当作同一帧的精确分段成本。",
    f"- 固定参数单面板托管分配约 **{single['managedBytesPerFrame']:.0f} B/帧**，其中同场景基线约 64 B，差额约 192 B。每帧改半径约 **{churn['managedBytesPerFrame']:.0f} B/帧**，比基线多约 728 B。分配明显增加，但本轮耗时差异受负载波动影响，不能断言参数动画的时间倍率。",
    "",
    "## 环境与方法",
    "",
    f"- CPU：{hardware['cpu'][0]}；内存：{hardware['memoryGiB']} GiB。",
    "- 检测到 Intel UHD Graphics 750，驱动 31.0.101.5334；另有 Oray/ToDesk 虚拟显示驱动。实际 Skia 后端为 OpenGL，Surface 已验证为纹理图像；未读取 GL renderer 字符串，因此未严格确认使用了哪个物理适配器。",
    f"- {main['runtime']}；{main['os']}；Avalonia 12.1.0；Release。测量时间：{measured_local}（Asia/Shanghai）。",
    "- 主扫 16 组、分段诊断 4 组；每组 3 轮，每轮 20 帧预热、120 帧计入统计，共 360 个样本/组。固定随机种子打乱每轮场景顺序。",
    "- 每个场景帧紧邻一个相同背景的无模糊基线帧。两者都用 GRContext.Flush(submit: true, synchronous: true) 等待 GPU 完成。额外耗时统计按逐帧差值计算，不是分别统计中位数后相减。",
    "- 计时包含 48 条背景色块、生产 BackdropOperation 和 GPU 完成等待；不包含 Border 表面、UI 布局/绑定、合成、窗口呈现/VSync及前景内容。负载数据是渲染成本，不是整页 FPS。",
    "- 通过反射构造生产绘制操作，并在计时前编译构造委托。固定参数保留操作对象；半径变化时按失效模式重建操作。未复制模糊实现。",
    "- 托管字节只计当前渲染线程，不代表原生纹理、GPU 工作集或整个应用内存。",
    "",
    "## 完整主扫",
    "",
    "尺寸为物理像素，200% DPI 场景单独标明。默认 Surface 为 1920×1080，默认 sigma 为 12。额外中位数允许出现接近零的负值，用于显示同步与调度噪声。",
    "",
    "| 场景 | 平均总耗时 ms | P95 总耗时 ms | 配对额外中位数 ms | 托管 B/帧 |",
    "|---|---:|---:|---:|---:|",
]
for row in main["rows"]:
    lines.append(f"| {labels[row['scenario']['Name']]} | {row['meanMs']:.2f} | {row['p95Ms']:.2f} | {row['medianAddedMs']:.2f} | {row['managedBytesPerFrame']:.0f} |")
lines += [
    "",
    "## 分段诊断",
    "",
    "两个诊断均使用 472×312 输入裁剪、400×240 输出裁剪。截图复制场景使用 Surface.Snapshot(crop) 后不加滤镜绘回；滤镜场景使用预先取得且保留的 GPU 图像、sigma 12，仅做滤镜绘制。均额外绘制背景、同步 GPU，并与相邻无效果背景帧配对。",
    "",
    "| 场景 | 平均总耗时 ms | P95 总耗时 ms | 配对额外中位数 ms |",
    "|---|---:|---:|---:|",
]
for row in stages["rows"]:
    lines.append(f"| {row['scenario']['Name']} | {row['meanMs']:.2f} | {row['p95Ms']:.2f} | {row['medianAddedMs']:.2f} |")
lines += [
    "",
    "## 波动与解释边界",
    "",
    "探索阶段的无模糊基线波动明显，因此正式结果采用配对数据。正式三轮仍有变化，例如单面板的轮均值如下，表明这台正在使用的机器存在 GPU 频率/系统调度/其他负载等未隔离因素。未停止用户应用或改变电源、驱动配置。",
    "",
    "| 场景 | 第 1 轮均值 ms | 第 2 轮均值 ms | 第 3 轮均值 ms |",
    "|---|---:|---:|---:|",
]
for name in ["radius-12", "four-panels", "nine-panels", "same-panel-4k-surface", "dpi-200-percent"]:
    row = rows[name]
    lines.append(f"| {labels[name]} | " + " | ".join(f"{value:.2f}" for value in row["roundMeansMs"]) + " |")
lines += [
    "",
    "半径 4 与 12 的细微排序，以及固定/动态背景的差异，不宜据此当作稳定规律。静态背景场景仍强制重绘，未测窗口静止时的空闲耗电。第一帧数据是新 Surface/操作的首帧，并非进程冷启动；主统计已排除预热。",
    "",
    "提交 CPU 时间通常远小于同步总耗时。提交后等待包含 GPU、驱动与同步成本；未使用 GPU timestamp 查询，不能把等待值当成纯 shader 时间。",
    "",
    "## 建议的后续优化顺序",
    "",
    "1. 优先评估缩小实际离屏工作 Surface、降低输入采样分辨率，以及按物理像素限制模糊强度；保持 GPU 和圆角回归验证。对低分辨率采样必须额外检查文字背景、边缘与缩放质量。",
    "2. 多面板场景评估共享输入/采样工作。先定义重叠语义：后画的面板当前会采样先画面板，共享快照可能改变视觉结果，不能直接替换。",
    "3. 若需要频繁动画模糊半径，评估保留操作资源的更新机制，减少整组重建。避免跨 UI/渲染线程共享可变 Skia 资源。",
    "4. 在隔离负载的目标设备上补做整页呈现帧时间和 GPU timestamp；本轮数据可用于定位方向，不能代替最终 FPS/功耗验收。",
    "",
    "## 数据与复跑",
    "",
    "- [探针说明](../../tools/BackdropBlur.Perf/README.md)",
    "- [正式配对 JSON](../../artifacts/backdrop-blur-perf-2026-10-08/paired/results.json)",
    "- [正式逐帧 CSV](../../artifacts/backdrop-blur-perf-2026-10-08/paired/frames.csv)",
    "- [分段 JSON](../../artifacts/backdrop-blur-perf-2026-10-08/stages/results.json)",
    "- [硬件元数据](../../artifacts/backdrop-blur-perf-2026-10-08/hardware.json)",
    "- 探索阶段数据保留在 artifacts/backdrop-blur-perf-2026-10-08/results.json；正式结论使用 paired/ 与 stages/。原始 artifacts 被 Git 忽略，仅保留在本地工作区。",
    f"- BackdropBlur.cs SHA-256：`{fingerprint}`。",
    "",
]
report = root / "docs/performance/2026-10-08-backdrop-blur.md"
report.parent.mkdir(parents=True, exist_ok=True)
report.write_text("\n".join(lines), encoding="utf-8")
print(report)
