# Mermaid 图表生成指南

## 使用方法

1. 访问: https://mermaid.live/
2. 复制下面的任意图表代码
3. 粘贴到左侧编辑器
4. 点击 "Actions" → "Export PNG" 或 "Export SVG"
5. 下载图片并插入到 Word 文档中

---

## 图表 1: 整体架构图

```mermaid
graph TB
    subgraph "表现层 (Presentation Layer)"
        UI[WPF UI 主界面]
        Views[视图层]
        ViewModels[ViewModels 层]
        SharedControls[共享UI组件库]
    end

    subgraph "应用层 (Application Layer)"
        WorkflowEngine[工作流引擎]
        ApplicationServices[应用服务层]
        UseCases[用例协调层]
    end

    subgraph "领域层 (Domain Layer)"
        DomainModels[领域模型]
        BusinessRules[业务规则]
        DomainServices[领域服务]
    end

    subgraph "插件层 (Plugin Layer)"
        PluginManager[插件管理器]
        PluginUIAdapter[UI适配器]
        AlgorithmPlugins[算法插件]
        DevicePlugins[设备插件]
        NodePlugins[节点插件]
    end

    subgraph "基础设施层 (Infrastructure Layer)"
        EventSystem[事件总线]
        ConfigSystem[配置管理]
        LogSystem[日志系统]
        CacheSystem[缓存系统]
    end

    subgraph "数据访问层 (Data Access Layer)"
        Database[数据库访问]
        FileStorage[文件存储]
        Serialization[序列化服务]
    end

    subgraph "交叉关注点 (Cross-Cutting Concerns)"
        Security[安全系统]
        Monitoring[监控系统]
        Validation[参数验证]
        ErrorHandling[错误处理]
    end

    UI --> ViewModels
    Views --> ViewModels
    ViewModels --> SharedControls
    ViewModels --> ApplicationServices
    ViewModels --> PluginUIAdapter
    ApplicationServices --> WorkflowEngine
    ApplicationServices --> DomainServices
    WorkflowEngine --> DomainModels
    PluginManager --> AlgorithmPlugins
    PluginManager --> DevicePlugins
    PluginManager --> NodePlugins
    PluginUIAdapter --> PluginManager
    EventSystem --> ApplicationServices
    EventSystem --> PluginManager
    ConfigSystem --> ApplicationServices
    LogSystem --> ApplicationServices
    LogSystem --> PluginManager
    CacheSystem --> ApplicationServices
    ApplicationServices --> Database
    ApplicationServices --> FileStorage
    Security --> PluginManager
    Security --> ApplicationServices
    Monitoring --> ApplicationServices
    Monitoring --> PluginManager
    Validation --> ApplicationServices
    ErrorHandling --> ApplicationServices
```

---

## 图表 2: 分层架构图

```mermaid
graph TB
    subgraph "表现层 - 用户界面"
        UI1[主窗口<br/>MainWindow]
        UI2[工作流画布<br/>WorkflowCanvas]
        UI3[属性面板<br/>PropertyPanel]
        UI4[插件浏览器<br/>PluginBrowser]
        UI5[日志查看器<br/>LogViewer]
        UI6[调试控制台<br/>DebugConsole]
    end

    subgraph "应用层 - 业务逻辑协调"
        App1[工作流执行服务<br/>WorkflowExecutionService]
        App2[插件加载服务<br/>PluginLoadService]
        App3[设备管理服务<br/>DeviceManagementService]
        App4[图像处理服务<br/>ImageProcessingService]
        App5[配置服务<br/>ConfigurationService]
    end

    subgraph "领域层 - 核心业务模型"
        Domain1[工作流模型<br/>Workflow]
        Domain2[节点模型<br/>WorkflowNode]
        Domain3[连接模型<br/>Connection]
        Domain4[算法结果<br/>AlgorithmResult]
        Domain5[设备模型<br/>Device]
    end

    subgraph "插件层 - 可扩展功能"
        Plugin1[算法插件<br/>IAlgorithmPlugin]
        Plugin2[节点插件<br/>INodePlugin]
        Plugin3[设备驱动插件<br/>IDeviceDriver]
        Plugin4[自定义UI插件<br/>IPluginUIProvider]
    end

    subgraph "基础设施层 - 技术服务"
        Infra1[事件总线<br/>EventBus]
        Infra2[日志记录器<br/>Logger]
        Infra3[配置管理器<br/>ConfigManager]
        Infra4[序列化服务<br/>Serializer]
    end

    subgraph "数据访问层 - 数据管理"
        Data1[工作流持久化<br/>WorkflowPersistence]
        Data2[插件元数据<br/>PluginMetadata]
        Data3[用户设置<br/>UserSettings]
        Data4[历史记录<br/>History]
    end

    subgraph "交叉关注点 - 横切关注点"
        Cross1[认证授权<br/>Authentication]
        Cross2[性能监控<br/>PerformanceMonitor]
        Cross3[异常处理<br/>ExceptionHandler]
        Cross4[参数验证<br/>ParameterValidator]
    end

    UI1 --> App1
    UI2 --> App1
    UI3 --> App1
    UI4 --> App2
    UI5 --> App2
    UI6 --> App1

    App1 --> Domain1
    App1 --> Domain2
    App1 --> Plugin1
    App1 --> Plugin2
    App2 --> Plugin1
    App2 --> Plugin3
    App2 --> Plugin4
    App3 --> Domain5
    App3 --> Plugin3
    App4 --> Plugin1
    App4 --> Domain4

    Plugin1 --> Domain4
    Plugin2 --> Domain2
    Plugin3 --> Domain5

    App1 --> Infra1
    App2 --> Infra1
    App3 --> Infra2
    App4 --> Infra2
    App1 --> Infra3
    App2 --> Infra3

    App1 --> Data1
    App2 --> Data2
    App1 --> Data3
    App1 --> Data4

    Cross1 --> App1
    Cross1 --> App2
    Cross2 --> App1
    Cross2 --> App3
    Cross3 --> App1
    Cross3 --> App2
    Cross4 --> App1
    Cross4 --> App4
```

---

## 图表 3: 组件交互图

```mermaid
sequenceDiagram
    participant U as 用户界面
    participant VM as ViewModel
    participant WE as WorkflowEngine
    participant PM as PluginManager
    participant PU as PluginUIAdapter
    participant P as 插件实例
    participant EB as EventBus
    participant L as Logger

    U->>VM: 用户操作 (创建工作流)
    VM->>WE: CreateWorkflow(id, name)
    WE->>L: LogInfo("Created workflow")
    L-->>WE: 记录日志
    WE-->>VM: 返回工作流实例
    VM-->>U: 更新UI显示

    U->>VM: 加载插件
    VM->>PM: LoadPlugins(directory)
    PM->>PM: 扫描DLL文件
    PM->>P: 创建插件实例
    P->>PM: Initialize()
    PM->>PM: 加载plugin.json
    PM-->>VM: 返回已加载插件列表
    VM->>PU: GetMainControl(plugin)
    PU->>PU: 判断UI模式
    alt Auto模式
        PU->>PU: 自动生成UI
    else Hybrid模式
        PU->>PU: 使用通用组件
        PU->>P: GetCustomPanel()
    else Custom模式
        PU->>P: GetCustomControl()
        P-->>PU: 自定义控件
    end
    PU-->>VM: 返回UI控件
    VM-->>U: 显示插件UI

    U->>VM: 执行工作流
    VM->>WE: ExecuteWorkflow(workflowId, inputImage)
    WE->>WE: 验证工作流
    WE->>EB: Publish("WorkflowStarted", event)
    loop 每个节点
        WE->>PM: GetPlugin(pluginId)
        PM-->>WE: 插件实例
        WE->>P: Execute(input, parameters)
        P->>P: 执行算法逻辑
        P-->>WE: 返回结果
        WE->>EB: Publish("NodeExecuted", event)
    end
    WE->>L: LogInfo("Workflow completed")
    WE-->>VM: 返回执行结果
    VM-->>U: 显示结果

    EB->>L: 记录所有事件
    EB->>VM: 更新UI状态
```

---

## 图表 4: 数据流图

```mermaid
graph LR
    subgraph "输入层"
        A[摄像头<br/>Camera]
        B[图像文件<br/>Image File]
        C[视频流<br/>Video Stream]
        D[实时信号<br/>Real-time Signal]
    end

    subgraph "数据采集层"
        E[设备驱动管理器<br/>DeviceDriverManager]
        F[数据采集服务<br/>DataAcquisitionService]
    end

    subgraph "预处理层"
        G[图像预处理<br/>ImagePreprocessing]
        H[信号滤波<br/>SignalFiltering]
        I[数据标准化<br/>DataNormalization]
    end

    subgraph "工作流执行层"
        J[WorkflowEngine<br/>工作流引擎]
        K[节点执行器<br/>NodeExecutor]
        L[结果聚合器<br/>ResultAggregator]
    end

    subgraph "算法处理层"
        M[图像识别<br/>ImageRecognition]
        N[缺陷检测<br/>DefectDetection]
        O[测量分析<br/>MeasurementAnalysis]
        P[数据统计<br/>DataStatistics]
    end

    subgraph "输出层"
        Q[结果显示<br/>ResultDisplay]
        R[报告生成<br/>ReportGeneration]
        S[数据导出<br/>DataExport]
        T[报警通知<br/>AlertNotification]
    end

    subgraph "存储层"
        U[数据库<br/>Database]
        V[文件存储<br/>FileStorage]
        W[日志存储<br/>LogStorage]
    end

    A --> E
    B --> F
    C --> F
    D --> E

    E --> F
    F --> G
    F --> H
    F --> I

    G --> J
    H --> J
    I --> J

    J --> K
    K --> M
    K --> N
    K --> O
    K --> P

    M --> L
    N --> L
    O --> L
    P --> L

    L --> Q
    L --> R
    L --> S
    L --> T

    L --> U
    L --> V
    J --> W
```

---

## 图表 5: 插件系统架构图

```mermaid
graph TB
    subgraph "插件接口层"
        I1[IPlugin<br/>基础接口]
        I2[IPluginUIProvider<br/>UI提供者]
        I3[IAlgorithmPlugin<br/>算法插件]
        I4[INodePlugin<br/>节点插件]
        I5[IDeviceDriver<br/>设备驱动]
    end

    subgraph "插件管理器"
        PM1[PluginManager<br/>插件管理器]
        PM2[PluginLoader<br/>插件加载器]
        PM3[PluginLifecycle<br/>生命周期管理]
        PM4[PluginDependency<br/>依赖管理]
    end

    subgraph "UI适配器"
        UA1[PluginUIAdapter<br/>UI适配器]
        UA2[AutoModeGenerator<br/>Auto模式生成器]
        UA3[HybridModeProvider<br/>Hybrid模式提供器]
        UA4[CustomModeProvider<br/>Custom模式提供器]
    end

    subgraph "共享UI组件库"
        SC1[GenericPropertyGrid<br/>属性网格]
        SC2[GenericParameterPanel<br/>参数面板]
        SC3[ImageVisualizationPanel<br/>图像可视化]
        SC4[ProgressPanel<br/>进度面板]
        SC5[StatusIndicator<br/>状态指示器]
    end

    subgraph "插件实例层"
        P1[ImageProcessingPlugin<br/>图像处理插件]
        P2[WorkflowPlugin<br/>工作流插件]
        P3[CustomFiltersPlugin<br/>自定义滤镜插件]
        P4[DeviceDriverPlugin<br/>设备驱动插件]
    end

    subgraph "插件沙箱"
        PS1[AppDomain隔离<br/>AppDomain Isolation]
        PS2[资源限制<br/>Resource Limits]
        PS3[安全检查<br/>Security Check]
        PS4[权限控制<br/>Permission Control]
    end

    subgraph "插件市场"
        PL1[在线插件库<br/>Online Repository]
        PL2[版本管理<br/>Version Management]
        PL3[评分评论<br/>Rating & Reviews]
        PL4[自动更新<br/>Auto Update]
    end

    P1 -.实现.-> I1
    P1 -.实现.-> I3
    P2 -.实现.-> I1
    P2 -.实现.-> I4
    P2 -.实现.-> I2
    P3 -.实现.-> I1
    P3 -.实现.-> I3
    P3 -.实现.-> I2
    P4 -.实现.-> I1
    P4 -.实现.-> I5

    PM1 --> PM2
    PM1 --> PM3
    PM1 --> PM4
    PM2 --> P1
    PM2 --> P2
    PM2 --> P3
    PM2 --> P4

    UA1 --> P1
    UA1 --> P2
    UA1 --> P3
    UA1 --> UA2
    UA1 --> UA3
    UA1 --> UA4

    UA2 --> SC1
    UA2 --> SC2
    UA3 --> SC1
    UA3 --> SC2
    UA3 --> SC3
    UA3 --> SC4
    UA3 --> SC5

    PM1 --> PS1
    PM1 --> PS2
    PM1 --> PS3
    PM1 --> PS4

    PM1 -.连接.-> PL1
    PL1 --> PL2
    PL1 --> PL3
    PL1 --> PL4
```

---

## 图表 6: 技术栈图

```mermaid
graph TB
    subgraph "前端技术栈"
        FE1[.NET 9.0<br/>运行时]
        FE2[WPF<br/>UI框架]
        FE3[XAML<br/>UI标记语言]
        FE4[MVVM模式<br/>架构模式]
        FE5[Prism/Caliburn.Micro<br/>MVVM框架]
    end

    subgraph "后端技术栈"
        BE1[C# 13<br/>编程语言]
        BE2[依赖注入<br/>DI Container]
        BE3[事件驱动<br/>Event-Driven]
        BE4[异步编程<br/>Async/Await]
        BE5[并行处理<br/>TPL]
    end

    subgraph "图像处理技术栈"
        IP1[OpenCV<br/>图像处理库]
        IP2[Emgu CV<br/>.NET OpenCV封装]
        IP3[深度学习框架<br/>TensorFlow/PyTorch]
        IP4[CUDA<br/>GPU加速]
    end

    subgraph "数据存储技术栈"
        DS1[SQLite<br/>本地数据库]
        DS2[System.Text.Json<br/>序列化]
        DS3[File System<br/>文件系统]
        DS4[Memory Cache<br/>内存缓存]
    end

    subgraph "插件技术栈"
        PL1[反射<br/>Reflection]
        PL2[动态加载<br/>Dynamic Loading]
        PL3[MEF/MAF<br/>插件框架]
        PL4[AppDomain<br/>应用程序域]
    end

    subgraph "开发工具"
        DT1[Visual Studio 2025<br/>IDE]
        DT2[MSBuild<br/>构建工具]
        DT3[NuGet<br/>包管理]
        DT4[Git<br/>版本控制]
        DT5[Mermaid<br/>文档图表]
    end

    FE1 --> BE1
    FE2 --> FE4
    FE3 --> FE2
    FE4 --> BE2
    FE4 --> BE3

    BE1 --> IP2
    IP2 --> IP1
    IP2 --> IP3
    IP2 --> IP4

    BE1 --> DS2
    BE1 --> DS3
    BE2 --> DS4

    BE1 --> PL1
    BE1 --> PL2
    PL2 --> PL3
    PL3 --> PL4

    DT1 --> BE1
    DT1 --> FE1
    DT1 --> DT2
    DT2 --> DT3
    DT1 --> DT4
    DT4 --> DT5
```

---

## 图表 7: 部署架构图

```mermaid
graph TB
    subgraph "开发环境"
        Dev1[开发人员工作站<br/>Visual Studio]
        Dev2[本地Git仓库<br/>版本控制]
        Dev3[本地测试<br/>单元测试]
    end

    subgraph "构建环境"
        Build1[CI/CD服务器<br/>Azure DevOps/Jenkins]
        Build2[自动化构建<br/>MSBuild]
        Build3[代码分析<br/>SonarQube]
        Build4[自动化测试<br/>自动化测试套件]
    end

    subgraph "部署环境"
        Deploy1[部署包<br/>MSIX/ClickOnce]
        Deploy2[安装程序<br/>Installer]
        Deploy3[配置文件<br/>Config Files]
    end

    subgraph "生产环境 - 单机部署"
        Prod1[工作站/PC<br/>Windows 10/11]
        Prod2[应用程序<br/>SunEyeVision.exe]
        Prod3[插件目录<br/>Plugins/]
        Prod4[数据目录<br/>Data/]
        Prod5[日志目录<br/>Logs/]
    end

    subgraph "生产环境 - 企业部署"
        Ent1[应用服务器<br/>Application Server]
        Ent2[数据库服务器<br/>Database Server]
        Ent3[文件服务器<br/>File Server]
        Ent4[监控服务器<br/>Monitoring Server]
    end

    subgraph "云部署"
        Cloud1[Azure/AWS<br/>云平台]
        Cloud2[容器化<br/>Docker]
        Cloud3[负载均衡<br/>Load Balancer]
        Cloud4[自动扩展<br/>Auto Scaling]
    end

    Dev1 --> Dev2
    Dev2 --> Build1
    Build1 --> Build2
    Build2 --> Build3
    Build2 --> Build4
    Build1 --> Deploy1
    Build1 --> Deploy2
    Deploy1 --> Deploy3

    Deploy1 --> Prod1
    Deploy2 --> Prod2
    Deploy3 --> Prod3
    Deploy3 --> Prod4
    Deploy3 --> Prod5

    Deploy1 --> Ent1
    Deploy1 --> Ent2
    Deploy3 --> Ent3
    Ent4 --> Ent1
    Ent4 --> Ent2

    Deploy1 --> Cloud1
    Cloud1 --> Cloud2
    Cloud2 --> Cloud3
    Cloud3 --> Cloud4
```

---

## 图表 8: 安全架构图

```mermaid
graph TB
    subgraph "认证层"
        Auth1[用户认证<br/>Username/Password]
        Auth2[多因素认证<br/>MFA]
        Auth3[Windows集成认证<br/>Windows Auth]
        Auth4[证书认证<br/>Certificate Auth]
    end

    subgraph "授权层"
        AuthZ1[基于角色的访问控制<br/>RBAC]
        AuthZ2[基于资源的访问控制<br/>Resource-based]
        AuthZ3[操作权限<br/>Operation Permissions]
        AuthZ4[插件权限<br/>Plugin Permissions]
    end

    subgraph "数据安全"
        Sec1[加密传输<br/>TLS/SSL]
        Sec2[数据加密<br/>AES-256]
        Sec3[敏感数据脱敏<br/>Data Masking]
        Sec4[安全存储<br/>Secure Storage]
    end

    subgraph "插件安全"
        PluginSec1[插件沙箱<br/>Plugin Sandbox]
        PluginSec2[资源限制<br/>Resource Limits]
        PluginSec3[权限隔离<br/>Permission Isolation]
        PluginSec4[代码签名<br/>Code Signing]
    end

    subgraph "审计日志"
        Audit1[操作日志<br/>Operation Log]
        Audit2[访问日志<br/>Access Log]
        Audit3[错误日志<br/>Error Log]
        Audit4[审计报告<br/>Audit Reports]
    end

    subgraph "防护机制"
        Protect1[输入验证<br/>Input Validation]
        Protect2[SQL注入防护<br/>SQL Injection Protection]
        Protect3[XSS防护<br/>XSS Protection]
        Protect4[异常处理<br/>Exception Handling]
    end

    Auth1 --> AuthZ1
    Auth2 --> AuthZ1
    Auth3 --> AuthZ1
    Auth4 --> AuthZ1

    AuthZ1 --> Sec1
    AuthZ2 --> Sec2
    AuthZ3 --> Sec3
    AuthZ4 --> Sec4

    AuthZ1 --> PluginSec1
    AuthZ4 --> PluginSec2
    PluginSec1 --> PluginSec3
    PluginSec1 --> PluginSec4

    AuthZ1 --> Audit1
    AuthZ2 --> Audit2
    AuthZ3 --> Audit3
    Audit1 --> Audit4
    Audit2 --> Audit4
    Audit3 --> Audit4

    AuthZ1 --> Protect1
    Sec1 --> Protect2
    Protect1 --> Protect3
    Protect1 --> Protect4
```

---

## 💡 快速提示

1. **复制整个代码块** - 从 ```mermaid 到 ```
2. **粘贴到编辑器** - https://mermaid.live/
3. **调整大小** - 使用滚轮缩放查看
4. **导出格式** - PNG 或 SVG 都可以
5. **插入Word** - Word 完全支持这两种格式

---

## 📝 建议的导出设置

- **分辨率**: 1200px 或更高
- **格式**: PNG (Word兼容性更好)
- **背景**: 透明或白色
- **主题**: 默认主题即可
