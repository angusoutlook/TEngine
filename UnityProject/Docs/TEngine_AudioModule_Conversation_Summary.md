TEngine AudioModule 模块讨论总结
===============================

> 说明：本文件是对 `TEngine_AudioModule_Conversation_Raw.md` 的精简总结，保留关键信息与设计意图，便于快速阅读与回顾。详细原文请查看同目录下的 Raw 文件。

---

### 1. 模块整体结构与职责分层

- **接口层：`IAudioModule`**
  - 提供对外统一入口：
    - 全局控制：`Volume`（走 `AudioListener.volume`）、`Enable`（总开关）。
    - 分类音量与开关：`Music/Sound/UISound/Voice` 的 Volume 与 Enable。
    - 播放与管理：`Play` / `Stop` / `StopAll`，以及 `AudioClipPool`（YooAsset `AssetHandle` 池）的增删与清空。
  - 设计意图：外部只关心「按类别播放/控制」，不直接接触 `AudioSource` 和 `AudioMixerGroup`。

- **实现层：`AudioModule`**
  - 依赖：
    - `IResourceModule`（资源加载，基于 YooAsset）。
    - `AudioSetting`（ScriptableObject）中的 `AudioGroupConfig[]`（音轨配置）。
    - `Resources/AudioMixer`（`AudioMixer.mixer`）。
  - 初始化逻辑：
    - 创建或绑定 `InstanceRoot`（`[AudioModule Instances]`，`DontDestroyOnLoad`）。
    - 检测 `AudioSettings.unityAudioDisabled`（编辑器调试时可整体关闭 Unity 音频）。
    - 使用传入或 `Resources.Load<AudioMixer>("AudioMixer")` 装载 Mixer。
    - 针对每个 `AudioType`：
      - 找到对应的 `AudioGroupConfig`。
      - 创建一个 `AudioCategory`（音频类别/轨道管理器）。
      - 记录初始分类音量 `_categoriesVolume[index]`。
  - 音量与开关：
    - 分类 Volume：0–1 映射到 Mixer 暴露参数（`MusicVolume` / `SoundVolume` / `UISoundVolume` / `VoiceVolume`），通过 `20 * log10(v)` 转换为 dB。
    - `MusicEnable` 特殊：通过修改 `MusicVolume` 参数到正常值或 -80dB 来实现静音和恢复（避免停播后的复杂恢复逻辑）。
    - 其他分类 Enable：控制对应 `AudioCategory.Enable`，内部停止该类所有 `AudioAgent`。
  - 更新：
    - `Update` 遍历 `_audioCategories`，调用每个 `AudioCategory.Update`，驱动 `AudioAgent` 状态机（播放/淡出等）。

- **分类层：`AudioCategory`**
  - 构造时：
    - 从 `AudioMixer` 查找 `Master/<AudioType>` 对应的 `AudioMixerGroup`；找不到则回退到 `Master`。
    - 在 `AudioModule.InstanceRoot` 下创建 `Audio Category - <GroupName>` GameObject 作为本分类的根。
    - 根据 `AudioGroupConfig.AgentHelperCount` 创建多条通道，每条通道对应一个 `AudioAgent`。
  - 播放逻辑 `Play(string path, bool bAsync, bool bInPool)`：
    - 若分类未开启，直接返回 `null`。
    - 遍历所有 `AudioAgent`：
      - 优先选择「`AudioData` 为空或 `IsFree`」的空闲通道。
      - 若无空闲，则记录并复用「`Duration` 最大」（播放时间最长）的通道。
    - 若有可用通道：
      - 没有实例则调用 `AudioAgent.Create` 创建并初始化；
      - 已有实例则调用 `Load`，内部处理淡出和切歌；
      - 返回该 `AudioAgent`。
    - 无可用通道（异常情况）：打印错误日志并返回 `null`。

- **代理层：`AudioAgent`**
  - 核心职责：封装「单个声音通道」：
    - 持有 `AudioSource` 和 `Transform`，挂在对应 `AudioCategory.InstanceRoot` 下。
    - 将 `AudioSource.outputAudioMixerGroup` 绑定到 `AudioMixer` 中的具体叶子组：`Master/<CategoryName>/<CategoryName> - <index>`。
    - 使用 `AudioGroupConfig` 配置 3D 参数：`audioRolloffMode` / `minDistance` / `maxDistance`。
  - 资源加载：
    - 首选从 `AudioModule.AudioClipPool`（如果 `bInPool == true` 且已存在）复用 `AssetHandle`。
    - 否则根据 `bAsync` 调用 `LoadAssetAsyncHandle` 或 `LoadAssetSyncHandle`。
    - 通过 `AssetHandle.Completed` 事件回调 `OnAssetLoadComplete`，分配 `AudioData`，设置剪辑并 `Play`。
  - 状态机与淡出：
    - 使用 `AudioAgentRuntimeState`（`None/Loading/Playing/FadingOut/End`）驱动逻辑。
    - `Stop(true)` 进入 `FadingOut`，在 `Update` 中按固定时间（`FADEOUT_DURATION = 0.2f`）线性减小音量，结束后统一切换到 `End` 并根据 `_pendingLoad` 决定是否加载新音频。
    - 通过 `Duration`（累计播放时长）为 `AudioCategory` 的通道选择提供依据。
  - `_pendingLoad`：
    - 仅是一个单对象 `LoadRequest`，语义为「最新一次的待播放请求」，而非队列。
    - 当 Agent 正在 Loading/Playing/FadingOut 时再次调用 `Load`：
      - 覆盖 `_pendingLoad`，表示只保留「最终一次」用户意图（例如连续切歌时只播最后一首）。
      - 若在 `Playing` 状态，则触发淡出；淡出结束后在 `Update` / 回调中按 `_pendingLoad` 再次调用 `Load`。
  - 返回 `AudioAgent` 的原因：
    - 便于上层针对这次播放做精细控制：
      - 属性：`Volume`、`IsLoop`、`Position` 等。
      - 行为：`Stop(fadeout)`、`Pause/UnPause`、状态查询等。
    - 作为「声音实例句柄」，解耦业务逻辑与 Unity `AudioSource`，并方便未来扩展实例级功能（优先级、标签、特殊淡入淡出规则等）。

- **配置层：`AudioGroupConfig` + `AudioSetting` + `AudioType`**
  - `AudioType`：`Sound/UISound/Music/Voice/Max`，要求名称与 `AudioMixer` 分组名一致。
  - `AudioGroupConfig`：
    - 每类声音的名称、初始音量、是否静音、代理通道数（`AgentHelperCount`）、3D 衰减与距离参数。
    - 约定：`AgentHelperCount` 要与 `AudioMixer` 中 `xxx - 0..N` 的子组数量一致。
  - `AudioSetting`（ScriptableObject）：
    - 仅持有 `AudioGroupConfig[]`，由 `Settings.AudioSetting` 提供给 `AudioModule.OnInit` 使用。

---

### 2. 与 `AudioMixer.mixer` 的强绑定设计

- **分组层级约定：**
  - Mixer 结构为：
    - `Master` 根组；
    - 其下有 `Music` / `Sound` / `UISound` / `Voice` 四个一级分组；
    - 每个分组下按 `名称 - index` 建立多个叶子分组（如 `Sound - 0` 至 `Sound - 3`）。
  - 代码中：
    - `AudioCategory` 通过 `FindMatchingGroups("Master/" + AudioType)` 找到分类组。
    - `AudioAgent` 通过 `FindMatchingGroups("Master/<CategoryName>/<CategoryName> - <index>")` 绑定到具体叶子组。

- **暴露参数约定：**
  - Mixer 暴露参数名包含：`MasterVolume`、`MusicVolume`、`SoundVolume`、`UISoundVolume`、`VoiceVolume` 等。
  - 代码中：
    - 使用常量 `MUSIC_VOLUME_NAME = "MusicVolume"`、`UI_SOUND_VOLUME_NAME = "UISoundVolume"`、`VOICE_VOLUME_NAME = "VoiceVolume"`，以及字符串 `"SoundVolume"` 与之对应。
  - `AudioModule` 的 `MusicVolume`/`SoundVolume` 等属性通过 `SetFloat` 直接控制对应 Mixer 暴露参数，从而实现分类推子控制。

- **Snapshot 初始值：**
  - `AudioMixer.mixer` 中 Snapshot 对应参数设置了默认 dB 值（如 Master / Music / Sound 等）。
  - 运行时 `AudioModule` 会使用自身的 Volume 属性覆盖这些值，从而与游戏内设置联动。

> 总体设计思想：**配置和结构都与 Mixer 强绑定**，通过「命名与层级约定」简化运行时逻辑，避免大量硬编码。

---

### 3. `_pendingLoad` 单对象而非列表的设计意图

- `_pendingLoad` 语义为「当前这条通道上**最新一次**待切换的播放请求」：
  - 若在 Loading/Playing/FadingOut 过程中多次调用 `Load`，每次都会覆盖 `_pendingLoad`。
  - 这样可以保证只执行最后一次请求，避免「所有中间请求都要排队播放」带来的 UX 混乱。
- 典型场景：
  - 正在播放或加载 BGM，连续三次切歌：
    - 前两次请求只会不断更新 `_pendingLoad`；
    - 最终真正生效的是第三次（最后一次）请求。
- 若项目需要「严格按顺序排队播放所有请求」，则需要把 `_pendingLoad` 改成 `List<LoadRequest>` 并配合队列逻辑；  
  但当前 TEngine AudioModule 的默认设计是「只关心最新意图」，所以单对象是刻意为之且逻辑自洽。

---

### 4. 异步加载与 `Completed` 回调的安全性

- 在一般纯 .NET 场景下：
  - 若事件 `Completed` 是普通事件（`event Action<T>`），且在你订阅之前就已经触发过，后订阅者不会收到已经发生的那次回调。
  - 解决思路包括：
    - 订阅前检查 `IsDone`，已完成则立刻同步调用回调；
    - 在 `Completed` 的 `add` 访问器内判断 `IsDone`，已完成则立即 `Invoke`；
    - 改为 `Task/async-await` 模式。

- 在 YooAsset 中当前 `AssetHandle.Completed` 的实现：

  ```csharp
  public event System.Action<AssetHandle> Completed
  {
      add
      {
          if (IsValidWithWarning == false)
              throw new System.Exception($"{nameof(AssetHandle)} is invalid");

          if (Provider.IsDone)
              value.Invoke(this);
          else
              _callback += value;
      }
      remove
      {
          if (IsValidWithWarning == false)
              throw new System.Exception($"{nameof(AssetHandle)} is invalid");

          _callback -= value;
      }
  }
  ```

  - 已经采用了「方案 B」：
    - 若 Provider 已经完成，则在 `add` 时立即调用 `value.Invoke(this)`，确保不会因为「先完成后订阅」而丢回调。
    - 否则正常加入 `_callback`，待异步完成时统一触发。
  - 这与 `AudioAgent` 中：

    ```csharp
    AssetHandle handle = _resourceModule.LoadAssetAsyncHandle<AudioClip>(path);
    handle.Completed += OnAssetLoadComplete;
    ```

    完全兼容，无论 `LoadAssetAsyncHandle` 此刻是否已经完成，`OnAssetLoadComplete` 都一定会被执行一次。

- 线程模型方面：
  - 在 Unity + YooAsset 的典型实现中，`Completed` 回调在 Unity 主线程执行，因此在 `OnAssetLoadComplete` 中操作 `AudioSource` / `AudioMixer` 是安全的。
  - 若迁移到非 Unity 环境或自定义资源系统，需要重新审视：
    - 回调在哪个线程执行；
    - 回调中是否访问 UI 或其他线程敏感对象；
    - 如有需要，在回调中切回主/UI 线程再执行逻辑。

---

### 5. 使用模式与建议

- **典型使用流程：**
  1. 在项目中配置 `AudioSetting`，为每个 `AudioType` 填写 `AudioGroupConfig`（包括通道数量、初始音量、3D 参数等）。
  2. 在 `AudioMixer.mixer` 中按约定建立分组与暴露参数：
     - `Master/Music/Music - 0..N`、`Master/Sound/Sound - 0..N` 等。
     - 暴露 `MusicVolume` / `SoundVolume` / `UISoundVolume` / `VoiceVolume` 等参数。
  3. 运行时通过 `ModuleSystem.GetModule<IAudioModule>()` 获取音频模块。
  4. 使用：
     - `Play(AudioType type, string path, ...)` 播放不同分类的音频；
     - 通过返回的 `AudioAgent` 调整实例音量、循环、位置等；
     - 通过模块级属性/方法控制分类开关与全局音量。

- **设计上的几个关键点：**
  - 强约定的 Mixer 结构和命名，使运行时代码相对简洁；
  - `AudioModule → AudioCategory → AudioAgent` 多层分责，提升扩展性与维护性；
  - 利用 `AudioAgent` 封装通道细节（资源加载、状态机、淡出），业务代码只需关注「我要播什么」与「如何控制这个实例」；
  - 使用 YooAsset 的 `Completed` 事件实现，在当前实现下保证了回调不会因「完成时间与订阅时间顺序」问题而丢失。

---

### 6. 关键示例代码片段（从原文中保留）

> 说明：本节保留了原讨论中的部分关键示例代码，便于结合上文设计说明理解实际用法。  
> 这些示例都是**基于当前项目真实接口**的调用示例。

- **示例 1：播放 BGM 与普通音效**

```csharp
using TEngine;
using UnityEngine;

public class AudioDemo : MonoBehaviour
{
    private IAudioModule _audio;

    private void Start()
    {
        // 获取音频模块
        _audio = ModuleSystem.GetModule<IAudioModule>();

        // 开启总开关
        _audio.Enable = true;
        _audio.Volume = 1.0f;

        // 播放 BGM（循环）
        _audio.MusicVolume = 0.8f;
        _audio.MusicEnable = true;
        _audio.Play(AudioType.Music, "Audio/BGM/MainTheme", bLoop: true, volume: 1.0f, bAsync: true);

        // 播放一个普通音效（一次性，不循环）
        _audio.SoundVolume = 1.0f;
        _audio.SoundEnable = true;
        _audio.Play(AudioType.Sound, "Audio/SFX/Explosion", bLoop: false, volume: 1.0f, bAsync: true);
    }
}
```

- **示例 2：UI 点击音效与滑条联动 Mixer 音量**

```csharp
using TEngine;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingPanel : MonoBehaviour
{
    public Slider musicSlider;
    public Slider soundSlider;
    public Toggle musicToggle;
    public Toggle soundToggle;

    private IAudioModule _audio;

    private void Awake()
    {
        _audio = ModuleSystem.GetModule<IAudioModule>();

        musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        soundSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
        musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
    }

    public void OnClickButton()
    {
        // 播放 UI 点击音效（使用 UISound 分类）
        _audio.UISoundEnable = true;
        _audio.UISoundVolume = 1.0f;
        _audio.Play(AudioType.UISound, "Audio/UI/Click", bLoop: false, volume: 1.0f, bAsync: true);
    }

    private void OnMusicVolumeChanged(float value)
    {
        // Slider 0–1 映射到 MusicVolume，内部会转换为 dB 写入 AudioMixer
        _audio.MusicVolume = value;
    }

    private void OnSoundVolumeChanged(float value)
    {
        _audio.SoundVolume = value;
    }

    private void OnMusicToggleChanged(bool on)
    {
        _audio.MusicEnable = on;
    }

    private void OnSoundToggleChanged(bool on)
    {
        _audio.SoundEnable = on;
    }
}
```

- **示例 3：预加载音效到对象池并复用**

```csharp
using System.Collections.Generic;
using TEngine;
using UnityEngine;

public class AudioPreload : MonoBehaviour
{
    private IAudioModule _audio;

    private void Start()
    {
        _audio = ModuleSystem.GetModule<IAudioModule>();

        var preloadList = new List<string>
        {
            "Audio/SFX/Explosion",
            "Audio/SFX/Hit",
            "Audio/SFX/Footstep"
        };

        // 异步预加载并放入 AudioClipPool
        _audio.PutInAudioPool(preloadList);
    }

    public void PlayHitSound()
    {
        // bInPool = true，会优先从 AudioClipPool 里取，避免重复加载
        _audio.Play(AudioType.Sound, "Audio/SFX/Hit", bLoop: false, volume: 1.0f, bAsync: false, bInPool: true);
    }

    private void OnDestroy()
    {
        // 场景切换或不再需要时可以清空对象池
        _audio.CleanSoundPool();
    }
}
```


