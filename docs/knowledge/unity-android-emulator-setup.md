# Unity 自带 Android SDK 搭建并启动 Android 模拟器（AVD）

适用场景：Unity 6（如 `6000.3.25f1`）已安装 Android 模块，但缺少 Emulator / 系统镜像，需要手动创建并启动一台 Android 虚拟机供 Unity `Build & Run` 使用。

> 关键认知：**Unity 安装 Android 模块时自带 SDK/NDK/JDK，但默认不装 Emulator 和系统镜像**，需要补齐组件；且 sdkmanager 的 license 在非交互环境（脚本/管道）下不会自动写入，必须处理权限与 license 文件。

---

## 1. 定位 Unity 的 Android SDK

默认路径（版本号随实际安装变化）：

```
C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK
```

其下的 `cmdline-tools\16.0\bin` 目录包含 `sdkmanager.bat` / `avdmanager.bat`。

PowerShell 自动查找：

```powershell
$sdk = Get-ChildItem -Path "C:\Program Files\Unity\Hub\Editor" -Recurse -Filter sdkmanager.bat -ErrorAction SilentlyContinue | Select-Object -First 1 | ForEach-Object { Split-Path (Split-Path $_.FullName) }
echo $sdk
```

---

## 2. 安装 Emulator 与系统镜像（核心踩坑）

### 2.1 常见错误做法（均无效）

| 命令 | 结果 | 原因 |
|------|------|------|
| `sdkmanager "emulator" "platform-tools" -y` | `Warning: Failed to find package '-y'` | sdkmanager **没有 `-y` 参数**（那是 apt 的用法） |
| `echo y \| sdkmanager ...` 或 `"y"\|...` | `Skipping following packages as the license is not accepted` | sdkmanager 检测到非 TTY 输入时**自动选 N 跳过许可** |
| `sdkmanager --accept-license ...` | `Error: Unknown argument "--accept-license"` | cmdline-tools 16.0 该版本不支持此参数 |

### 2.2 真正原因：license 文件未写入 + Program Files 权限

- `sdkmanager --licenses` 在非交互环境下同样不会真正写入 license 文件（显示 "All SDK package licenses accepted" 是误导）。
- 诊断可见 SDK 根目录**没有 `licenses` 文件夹**（用户目录也没有）：

```powershell
$sdk = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK"
Test-Path "$sdk\licenses"   # 预期应为 False
```

- `C:\Program Files` 是受保护目录，普通 PowerShell 无法在其下创建 `licenses` 目录（会报 `PermissionDenied`）。

### 2.3 正确方案：管理员提权 + 手动写入 license 文件

Android SDK 的标准 `android-sdk-license` 哈希是固定的，手动创建即可：

```powershell
$sdk = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK"
New-Item -ItemType Directory -Force -Path "$sdk\licenses" | Out-Null
Set-Content -Path "$sdk\licenses\android-sdk-license" -Value "24333f8a63b6825ea9c5514f83c2829b004d1fee" -NoNewline -Encoding ASCII
```

> 注意：`Set-Content` 必须带 `-Encoding ASCII`（否则被安全策略拦截），且文件内容就是这一行哈希、无多余字符。

由于写 `C:\Program Files` 需要管理员，用 `RunAs` 提权执行安装脚本（脚本内自行写日志，因为 `-Verb RunAs` 不能与 `-RedirectStandardOutput` 同用）：

```powershell
$scriptPath = "$env:TEMP\install_sdk.ps1"
$scriptContent = @'
$sdk = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK"
$log = "$env:TEMP\sdk_install.log"
"=== start ===" | Out-File $log -Encoding ascii
New-Item -ItemType Directory -Force -Path "$sdk\licenses" | Out-Null
Set-Content -Path "$sdk\licenses\android-sdk-license" -Value "24333f8a63b6825ea9c5514f83c2829b004d1fee" -NoNewline -Encoding ASCII
cd "$sdk\cmdline-tools\16.0\bin"
.\sdkmanager.bat "emulator"                   2>&1 | Out-File $log -Append -Encoding ascii
.\sdkmanager.bat "platform-tools"             2>&1 | Out-File $log -Append -Encoding ascii
.\sdkmanager.bat "system-images;android-34;google_apis;x86_64" 2>&1 | Out-File $log -Append -Encoding ascii
.\sdkmanager.bat "platforms;android-34"       2>&1 | Out-File $log -Append -Encoding ascii
"=== done ===" | Out-File $log -Append -Encoding ascii
'@
Set-Content -Path $scriptPath -Value $scriptContent -Encoding ASCII
Start-Process -Verb RunAs -Wait -FilePath powershell -ArgumentList "-NoProfile","-ExecutionPolicy","Bypass","-File",$scriptPath
Get-Content "$env:TEMP\sdk_install.log" | Select-Object -Last 20
```

说明：
- 选 `android-34`(Android 14) + `x86_64` ABI：PC 上跑模拟器最快；避免选 `arm64`（极慢）。需更高版本把 `34` 换成 `35`/`36`。
- 安装完会有 `platform-tools.backup` 警告（sdkmanager 备份了 Unity 自带的旧 platform-tools），**无害**，可提权删除：

```powershell
Start-Process -Verb RunAs powershell "-Command Remove-Item 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools.backup' -Recurse"
```

---

## 3. 创建 AVD（虚拟机）

写的是用户目录 `~/.android/avd`，**不需要管理员权限**：

```powershell
cd "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\cmdline-tools\16.0\bin"
"" | .\avdmanager.bat create avd -n MyAvd -k "system-images;android-34;google_apis;x86_64" -d pixel_5
```

> 注意：PowerShell 里 `echo.` 不是合法命令（那是 cmd 语法），用 `"" |` 提供空输入以采用默认硬件配置。创建成功会输出 `Auto-selecting single ABI x86_64`。

验证 AVD 被识别：

```powershell
$sdk = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK"
& "$sdk\emulator\emulator.exe" -list-avds
```

---

## 4. 启动模拟器

模拟器是长期运行进程，启动后 adb 才能识别设备。**必须先开机，Unity 才能看到它**。

```powershell
$sdk = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK"
Start-Process -FilePath "$sdk\emulator\emulator.exe" -ArgumentList "-avd","MyAvd" -WindowStyle Normal
```

等待约 30–60 秒（首次启动更久），检查是否被 adb 识别：

```powershell
$sdk = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK"
& "$sdk\platform-tools\adb.exe" devices
# 期望输出：emulator-5554    device
```

---

## 5. 在 Unity 中使用

1. **保持模拟器窗口运行**，不要关闭。
2. Unity 菜单 `File > Build Settings` → 选 `Android` 平台 → 点 **Refresh**。
3. `Run Device` 下拉会出现 `emulator-5554`（或 `Android Emulator`）。
4. 选中 → **Build & Run**，Unity 部署到该模拟器。

> 重要：Unity 的 `Run Device` **只列出正在运行且已被 adb 识别的设备**，不会列出关机的 AVD。点 Refresh 看不到设备，多半是模拟器还没启动。

若 Unity 仍刷不出设备，检查 `Edit > Preferences > External Tools > Android`，确认 SDK 路径指向 `.../AndroidPlayer/SDK`（或勾选 "Android SDK Tools Installed with Unity"），改完重启 Unity。

---

## 6. 启动前必做（否则黑屏/报错）

- **开启 CPU 虚拟化**：BIOS 中打开 VT-x（Intel）或 SVM（AMD）。
- **Windows 加速二选一**：
  - 控制面板 → 程序 → 启用或关闭 Windows 功能 → 勾选 **Windows Hypervisor Platform**（Win10 1809+ 推荐）；或
  - 安装 **Intel HAXM**：`sdkmanager "extras;intel;Hardware_Accelerated_Execution_Manager"`（Win11 已弃用 HAXM，优先 WHPX）。
- 始终选 `x86_64` 镜像，PC 端性能最佳。

---

## 7. 一键复现脚本（封装启动）

如需桌面一键启动，可保存为 `start-emulator.ps1`：

```powershell
$sdk = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK"
& "$sdk\emulator\emulator.exe" -avd MyAvd
```

---

## 速查表

| 步骤 | 命令要点 |
|------|----------|
| 许可文件 | 手动写 `licenses/android-sdk-license` = `24333f8a63b6825ea9c5514f83c2829b004d1fee`（ASCII，管理员） |
| 装组件 | `sdkmanager emulator platform-tools system-images;android-34;google_apis;x86_64 platforms;android-34`（需 RunAs 提权） |
| 建 AVD | `avdmanager create avd -n MyAvd -k system-images;android-34;google_apis;x86_64 -d pixel_5`（用 `"" \|` 提供默认） |
| 启动 | `emulator -avd MyAvd` |
| 验证 | `adb devices` → `emulator-5554  device` |
| Unity | 先启动模拟器，再 Build Settings → Refresh → 选 `emulator-5554` → Build & Run |

---

## 8. 故障排查

### 8.1 模拟器启动报 "requires hardware acceleration" / 卡黑屏不动
- 现象：`ERROR: x86_64 emulation currently requires hardware acceleration` 或窗口一直黑屏。
- 原因：未开启 CPU 虚拟化或 Windows 加速后端。
- 解决：
  1. 进 BIOS 开启 **VT-x**（Intel）/ **SVM**（AMD）。
  2. Windows 功能里勾选 **Windows Hypervisor Platform**（或旧机用 HAXM）。
  3. 用 `emulator -avd MyAvd -verbose` 看具体后端选择日志。

### 8.2 adb 显示 `emulator-5554 offline` 一直不在线
- 模拟器正在 boot，等 30–90 秒变 `device` 即可。
- 若长时间 offline：`adb kill-server; adb start-server` 重启 adb 守护进程后重试。

### 8.3 Unity 的 Refresh 看不到 `emulator-5554`
按顺序排查：
1. **模拟器是否真的在运行**（桌面有窗口，且 `adb devices` 能看到 `emulator-5554  device`）。Unity 只显示运行中的设备。
2. Unity 的 SDK 路径是否指错：`Edit > Preferences > External Tools > Android`，SDK 应指向 `.../AndroidPlayer/SDK`（或勾选 "Installed with Unity"），改完**重启 Unity**。
3. 端口冲突：默认 `5554`，若被占用模拟器会用 `5556` 等，以 `adb devices` 实际显示为准。

### 8.4 `adb server version (xx) doesn't match this client (yy)`
- 机器上存在多个 `adb.exe`（Android Studio、Unity、其它 SDK），版本不一致互相踢。
- 统一使用 Unity SDK 自带的 `platform-tools/adb.exe`；或把所有环境的 `adb` 更新到同一版本。

### 8.5 创建 AVD 报 "No system images installed for this package"
- 没装对应 `system-images;android-XX;...`。回到第 2.3 节补装对应 API 级别的镜像。

### 8.6 模拟器启动极慢（几分钟才出画面）
- 几乎都是用了 `arm64` 镜像跑在 x86 PC 上 → 改用 `x86_64` 镜像。
- 或硬件加速未生效（见 8.1）。

### 8.7 license 仍报 "not accepted"
- 确认 `licenses` 目录在**正确的 SDK 根**下（与 `sdkmanager.bat` 所在 SDK 一致），不是用户目录。
- 文件 `android-sdk-license` 内容为 `24333f8a63b6825ea9c5514f83c2829b004d1fee`，**ASCII 编码、无 BOM、无多余换行**。
- 若用 `--sdk_root=xxx` 指定过根目录，license 也要写到那个根下。

### 8.8 HAXM 安装失败（Win11）
- Win11 已弃用 HAXM，且 Hyper-V/WHPX 与 HAXM 互斥。直接改用 **Windows Hypervisor Platform**，不要装 HAXM。

---

## 9. 最小验证清单（排错时逐项核对）
1. `adb devices` → 出现 `emulator-5554  device`（否则模拟器没起来或加速未开）。
2. `emulator -list-avds` → 含 `MyAvd`。
3. `Test-Path "$sdk\licenses\android-sdk-license"` → `True`。
4. Unity `Preferences > External Tools` 的 Android SDK 路径正确。
5. BIOS 虚拟化 + WHPX/HAXM 已启用。
