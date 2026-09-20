# 项目长期记忆

## 项目基本信息

- 路径：`f:\UnityProject\Test2022\VContainer\New Unity Project`
- Unity 版本：**2022.3.16f1**（Windows，PowerShell）
- 项目类型：VContainer 学习/实验项目，原本是空工程

## 依赖约定

- VContainer 通过 UPM git URL 引入（写入 `Packages/manifest.json`），版本 **1.19.0**：
  `"jp.hadashikick.vcontainer": "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.19.0"`
- 项目**没有**安装 UniTask / Unity.Entities；因此 `IAsyncStartable.StartAsync` 返回
  `System.Threading.Tasks.Task`，`VCONTAINER_UNITASK_INTEGRATION` 未定义。
- 教程示例**刻意不使用 `UnityEngine.UI`**，避免额外引入 `com.unity.ugui` 依赖。

## 教程位置与结构

- 教程根目录：`Assets/VContainerTutorials/`
- 约定：**一课 = 一个文件夹，内含 `README.md` + 可运行的 C# 代码**
- 命名空间约定：`VContainerTutorials.Lesson01` ~ `VContainerTutorials.Lesson11`
- 日志约定：统一带 `[LessonXX]` 前缀，方便在 Console 里过滤
- `TutorialLauncher.cs`：挂在场景任意 GameObject 上，用 Inspector 下拉框切换课程
  （实现方式是 `new GameObject` + `AddComponent<XxxLifetimeScope>()`）
- **第 11 课是唯一带 asmdef 的课程**（`11_CommercialCase/`），五层结构：
  `Domain`（`noEngineReferences: true`）/ `Infrastructure` / `Application` /
  `Presentation` / `Composition`。其余课程都在 `Assembly-CSharp` 里。
  5 个 asmdef 都是 `autoReferenced: true`，所以 `TutorialLauncher` 能引用到
  `Lesson11.Composition.Lesson11LifetimeScope`。

## 代码风格约定（本仓库教程）

- 依赖优先构造函数注入 + `readonly` 字段
- 代码注释使用中文；**在字符串字面量内部不要使用中文引号**
  （写入流程会把全角引号规范化成 ASCII 引号，导致字符串被截断）
- 涉及 VContainer API 的结论都以 1.19.0 源码为准，不直接照搬官方文档
  （官方文档部分页面基于较早版本）

## Unity 编译错误排查方法（本项目实测有效）

1. **看哪些程序集编译成功了**：`Library/ScriptAssemblies/*.dll`
   缺哪个 dll，问题就在哪个 asmdef/程序集里；被依赖方失败会级联跳过下游程序集
   （例如 Composition 失败 → Assembly-CSharp 也不会编译）。
2. **看全部编译错误**：`%LOCALAPPDATA%\Unity\Editor\Editor.log`
   （可能几百 MB，用 `Select-String -Pattern "error CS" -Encoding UTF8` 过滤，
   也可以过滤 `"warning CS"`）。
3. **确认包是否装好**：`Library/PackageCache/jp.hadashikick.vcontainer@<commit>`
   （1.19.0 对应 commit `5401e5a7`）。
4. **没有 Unity 时的静态检查**（本机有 .NET SDK）：
   - 语法：临时 csproj 编译全部 `.cs`，看有没有 `CS1xxx` 错误；
   - **using 完整性**：`CS0246` 在缺程序集时会大量出现，**无法反映真正的漏 using**，
     必须单独做"用到的类型名 ↔ 文件 using"的交叉检查脚本。

## 已知易错点

- `LifetimeScope` / `IInstaller` / `IStartable` / `ITickable` 等都在
  **`VContainer.Unity`**；`Lifetime` / `IContainerBuilder` / `IObjectResolver` /
  `Inject` / `Key` 在 **`VContainer`**。两行 using 经常只写一行。
