# EX-Maid For UI (XUI)
本插件集成了LoxodonFramework(MVVM)和FairyGUI，用于快速开发UI界面。适用于单人/小团队/小规模游戏开发。

## 相关文档
- [LoxodonFramework](https://github.com/vovgou/loxodon-framework/blob/master/docs/LoxodonFramework.md)
- [FairyGUI](https://www.fairygui.com/docs/guide/index.html)
> 使用本插件需要先学会使用以上两个插件。
> 
> FairyGUI的使用请参考官方文档，本插件不提供FairyGUI的使用教程。FairyGUI功能很强大基本满足了传统UI开发的需求。
> 学习起来也很快，主要是掌握UI编辑器的使用和各UI组件的接口。
> 
> Loxodon Framework的使用请参考官方文档。Loxodon Framework是一个MVVM框架，主要用于视图和数据的绑定。
> Loxodon Framework有专门针对FairyGUI的扩展，可以很方便的将FairyGUI的UI组件和Loxodon Framework的ViewModel绑定起来。
> 本插件已经自带集成了Loxodon Framework的FairyGUI扩展，不需要再额外安装。

## 快速开始
### 安装
1. UMP中git方式添加依赖插件LoxodonFramework:https://github.com/vovgou/loxodon-framework.git?path=Loxodon.Framework/Assets/LoxodonFramework
2. UMP中git方式添加EXMaidForUI:https://github.com/No78Vino/EXMaidForFGUIandLoxondon.git

### 使用
1. 先使用FairyGUI编辑器创建UI界面。请遵守以下**规范:**
   1. 合理分包。
   2. 包的导出目录，必须遵照**一包一文件夹**的规格。可以使用FairyGUI的导出变量来设置导出路径表达式，例如：C:\your_game_project\Assets\FairyGUI\\{publish_file_name}
   3. 包之间依赖请尽量避开组件引用的情况（资源引用不做约束要求）。这是为了方便之后的FairyGUI的UI定义脚本生成不出现循环引用的情况。
2. 导出FairyGUI资源包到Unity游戏工程内。
3. 第一次使用EX-Maid For UI时，需要设置好 *FairyGUI资源包路径* 和 *UI定义脚本生成路径*
4. 在Unity游戏工程内，使用FairyGUI的UI定义脚本生成工具，生成UI定义脚本。
5. 根据项目UI，编写对应的 V（View） 和 VM（ViewModel）脚本。
6. 在合适的时机初始化XUI
   >XUI.Launch(string prefix,FairyGUIPackageExtension.OnLoadResource onLoadResourceHandler)
7. 接下来就根据项目的各自需要，加载/打开/关闭/管理UI

## 参考案例

## API介绍
### XUI
- static void Launch(string prefix,FairyGUIPackageExtension.OnLoadResource onLoadResourceHandler)
    - prefix: FairyGUI资源包路径前缀
    - onLoadResourceHandler: FairyGUI资源包加载回调
    - XUI的总初始化启动函数
- static void Close()
    - XUI的总关闭函数

- static IEXMaidUI M
    - XUI的UI管理实例， M为Maid的首字母。一切的UI操作都通过M来进行的。
### IEXMaidUI
- void UITick()
    - Tick函数，所有window/UI的Update最终都由该函数调用。

- void OnDispose();
    - EXMaidUI的销毁回调，会关闭所有窗口UI以及上下文服务。不建议自己调用。

- void LaunchBindingService(string prefix,FairyGUIPackageExtension.OnLoadResource onLoadResourceHandler)
    - prefix: FairyGUI资源包路径前缀
    - onLoadResourceHandler: FairyGUI资源包加载回调
    - 启动UI绑定服务。初始化绑定用的上下文。

- T LoadWindow<T>() where T : AbstractFGUIWindow
    - 加载窗口UI，如果已经加载过，则直接返回已经加载的窗口UI。

- void UnloadWindow<T>() where T : AbstractFGUIWindow
    - 卸载窗口UI，如果窗口UI已经打开，则会先关闭窗口UI。

- T OpenWindow<T>() where T : AbstractFGUIWindow
    - 打开窗口UI，如果窗口UI没有加载过，则会先加载窗口UI。

- T VM<T>() where T : ViewModelCommon
    - 获取VM实例，如果VM没有加载过，则会失败返回null。切记一定要保证VM对应的V已经加载过。
  
- AbstractFGUIWindow Windows(Type type)
    - 获取窗口UI实例，如果窗口UI没有加载过，则会先加载窗口UI。

- AbstractFGUIWindow WindowsWithoutLoad(Type type)
    - 获取窗口UI实例，如果窗口UI没有加载过，则会失败返回null。

- void AddWorldSpaceUI(GObject obj)
    - 添加世界空间UI，用于在3D场景中显示UI。例如：血条，伤害飘字等。

- void RefreshSceneUICanvas(float cameraSize);
    - 刷新场景UI画布，用于在3D场景中显示UI。例如：血条，伤害飘字等。
