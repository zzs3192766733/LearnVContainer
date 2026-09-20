# 第 05 课：注册家族大全

> 官方对照：`registering/register-type`、`registering/register-scriptable-object`

## 本课目标

把 `builder.Register*` 的全部写法过一遍，重点是三件事：

1. **一个实现可以同时以多种"身份"被解析**（接口 / 多个接口 / 自身）
2. **`RegisterInstance` 与 `Register` 的本质区别**（谁负责创建、谁负责释放）
3. **`WithParameter` 的适用场景与陷阱**、**开放泛型**怎么写

---

## 1. 全览表

| 写法 | 解析出的类型 | Lifetime |
| --- | --- | --- |
| `builder.Register<ServiceA>(Lifetime.Singleton)` | `ServiceA` | 指定 |
| `builder.Register<IServiceA, ServiceA>(Lifetime.Singleton)` | `IServiceA` | 指定 |
| `builder.Register<ServiceA>(lt).As<IServiceA, IInputPort>()` | `IServiceA`、`IInputPort` | 指定 |
| `builder.Register<ServiceA>(lt).AsImplementedInterfaces()` | 所有实现的接口 | 指定 |
| `builder.Register<ServiceA>(lt).AsImplementedInterfaces().AsSelf()` | 所有接口 + `ServiceA` | 指定 |
| `builder.RegisterInstance(obj)` | `obj` 的运行时类型 | **恒为 Singleton** |
| `builder.RegisterInstance<IServiceA>(obj)` | `IServiceA` | **恒为 Singleton** |
| `builder.RegisterInstance(obj).As<I1, I2>()` | `I1`、`I2` | **恒为 Singleton** |
| `builder.Register<SomeService>(lt).WithParameter<string>("...")` | `SomeService` | 指定 |
| `builder.Register(typeof(GenericRepo<>), Lifetime.Singleton)` | 任意闭合泛型 | 指定 |

---

## 2. 一个实现，多种身份

```csharp
builder.Register<ServiceA>(Lifetime.Singleton)
       .AsImplementedInterfaces()   // IServiceA, IInputPort, IOutputPort
       .AsSelf();                   // 再加上 ServiceA 自身
```

于是下面**四种写法都能解析到同一个实例**：

```csharp
container.Resolve<IServiceA>();     // #
container.Resolve<IInputPort>();    // #  同一个对象
container.Resolve<IOutputPort>();   // #
container.Resolve<ServiceA>();      // #
```

示例里用 `ReferenceEquals` 亲自验证了这一点。

**原理**（源码 `Registry.Build`）：注册表 `Registry` 本质是一张
`(Type, key) → Registration` 的表。一次注册如果声明了多个 `InterfaceTypes`，
就会在表里为**每一个类型**插入同一份 `Registration`。
所以"一个实例多种身份"是天然支持的，不产生额外对象。

> `AsSelf()` 的作用是**同时保留具体类型这个入口**。
> 不加它，`Resolve<ServiceA>()` 会失败（因为注册只声明了接口），
> 但仍然可以 `Resolve<IServiceA>()`。

### 什么时候不该用 `AsImplementedInterfaces()`

当你的类实现了 `IDisposable` 时会有点烦：`AsImplementedInterfaces()` 会把
`IDisposable` 也注册进去，之后 `Resolve<IDisposable>()` 就会意外地成功（或者和别的注册冲突）。
需要精确控制时就老老实实 `As<IServiceA>()`。

---

## 3. `RegisterInstance`：把"已经存在的对象"交给容器

```csharp
var obj = new AppInfo("1.19.0");
builder.RegisterInstance(obj);
```

两条**非常重要的规则**（源码 `InstanceRegistrationBuilder` / `ExistingInstanceProvider`）：

| 规则 | 说明 |
| --- | --- |
| **Lifecycle 恒为 Singleton** | 所以这个方法没有 `Lifetime` 参数。 |
| **容器不接管它的生命周期** | 不会调用它的 `Dispose()`，**也不会执行它的方法注入 / 字段注入**。 |

也就是说：`RegisterInstance` 注册的对象**只是一个"值"**，容器不会去动它。

如果希望容器管它，官方给了两个替代方案：

```csharp
// 方案 A：用委托注册 —— 容器会调用你的 lambda，产出的对象归容器管
builder.Register<IServiceA>(container => new ServiceA(), Lifetime.Singleton);

// 方案 B：如果它是 MonoBehaviour，用 RegisterComponent
builder.RegisterComponent(myView);
```

### ScriptableObject 配置

官方推荐的做法就是把 SO 里的子设置对象**当成普通实例**注册：

```csharp
[CreateAssetMenu(menuName = "MyGame/Settings")]
public class GameSettings : ScriptableObject
{
    public CameraSettings cameraSettings;
    public ActorSettings  actorSettings;
}

// LifetimeScope 里
[SerializeField] GameSettings settings;

protected override void Configure(IContainerBuilder builder)
{
    builder.RegisterInstance(settings.cameraSettings);
    builder.RegisterInstance(settings.actorSettings);
}
```

这样做的好处：**依赖方只看到 `CameraSettings`，完全不知道 ScriptableObject / Asset 的存在**，
测试时可以传入一个临时 `new CameraSettings()`。

---

## 4. `WithParameter`：注册期指定常量参数

有些类需要的是"值"而不是"依赖"，比如：

```csharp
public class SomeService
{
    public SomeService(string url, int timeout) { ... }
}
```

`url` 和 `timeout` 不是服务，容器不可能自动装配。这时：

```csharp
builder.Register<SomeService>(Lifetime.Singleton)
       .WithParameter<string>("https://example.com")   // 按类型匹配
       .WithParameter<int>(30);

// 或者按参数名匹配
builder.Register<SomeService>(Lifetime.Singleton)
       .WithParameter("url", "https://example.com");
```

### 匹配规则（源码 `InjectParameter.cs`）

| 写法 | 匹配依据 |
| --- | --- |
| `WithParameter<T>(value)` / `WithParameter(typeof(T), value)` | **只看类型** `parameterType == Type` |
| `WithParameter("name", value)` | **只看名字** `parameterName == name` |

解析顺序（`ResolveOrParameter`）：**先看参数表，没命中才去容器 Resolve**。

### ⚠️ 陷阱

`WithParameter` 的作用域**仅限这一个注册**：

```csharp
builder.Register<SomeService>(lt).WithParameter<string>("http://example.com");

class OtherClass
{
    // ❌ 如果别处也想要一个 string，会抛 "No such registration of type: System.String"
    public OtherClass(string anything) { }
}
```

官方明确建议：**能用配置对象（`CameraSettings` 这种）就别用裸 `string`/`int` 参数**。
因为裸类型没有语义，注册表里全是 `string`、`int` 时，谁也说不清哪个是哪个。

---

## 5. 开放泛型

```csharp
builder.Register(typeof(GenericRepository<>), Lifetime.Singleton);
```

然后任何闭合类型都能解析：

```csharp
public MyClass(GenericRepository<int> intRepo, GenericRepository<string> strRepo) { }
```

实现细节（源码 `OpenGenericInstanceProvider`）：容器在**运行时**用
`implementationType.MakeGenericType(typeParameters)` 构造出闭合类型，
并按"类型参数组合"缓存在 `ConcurrentDictionary` 里。
所以同一组类型参数只会有一个 `Registration`，`Singleton` 语义得以保持。

**平台注意**：官方说明在 **Unity 2022.1+ 的 IL2CPP** 上可以正常工作；
更早版本可能不支持运行时动态 typedef。

---

## 6. 本课示例运行结果

```
[Lesson05] ---- 一个实现，多种身份 ----
[Lesson05] Resolve<IServiceA>   -> ServiceA
[Lesson05] Resolve<IInputPort>  -> ServiceA, Read() = 来自 ServiceA 的输入
[Lesson05] Resolve<IOutputPort> -> ServiceA
[Lesson05] Resolve<ServiceA>    -> ServiceA
[Lesson05] 它们是不是同一个实例？ True / True / True
[Lesson05] ---- 实例 / 参数 / 开放泛型 ----
[Lesson05] WithParameter 注入的 HttpConfig -> https://example.com (timeout=30)
[Lesson05] 开放泛型 -> GenericRepository<Int32> / GenericRepository<String>
[Lesson05] RegisterInstance 注入的 CameraSettings.MoveSpeed = 12
[Lesson05] RegisterInstance 注入的 ActorSettings.FlyingTime = 0.8
[Lesson05] RegisterInstance 注入的 AppInfo -> AppInfo(v1.19.0)
```

---

## 7. 常见坑

| 坑 | 说明 |
| --- | --- |
| 同一个**具体类型 + `Lifetime.Singleton`** 注册两次 | 构建时抛 `VContainerException: Conflict implementation type`（源码 `CollectionInstanceProvider.Add` 只在 Singleton 时拦截） |
| 同一个具体类型用**其它 Lifecycle** 注册两次 | 不报错：直接 `Resolve<T>()` 拿到**最后一个**，同时所有注册都能通过 `IReadOnlyList<T>` 集合形式拿到（第 08 课） |
| `.AsSelf()` 忘了写 | `Resolve<具体类型>()` 失败 |
| `AsImplementedInterfaces()` 把 `IDisposable` 也注册了 | 造成意外的可解析类型 |
| 用 `RegisterInstance` 注册需要注入的对象 | 它的 `[Inject]` **不会**被执行 |
| 用 `RegisterInstance` 注册 `IDisposable` | 容器**不会**替你 `Dispose` |
| 大量 `WithParameter<string>` | 语义丢失；改用配置对象 |

---

## 8. 练习题

1. 把 `Register<ServiceA>(...).AsImplementedInterfaces().AsSelf()` 改成
   `Register<IServiceA, ServiceA>(Lifetime.Singleton)`，预测哪些 `Resolve` 会失败。
2. 给 `ServiceA` 加一个 `IDisposable` 实现，观察 `AsImplementedInterfaces()` 带来的额外注册。
3. 把 `HttpConfig` 的 `WithParameter` 换成一个 `HttpConfigSettings` 配置类，
   体会"带语义的参数对象"和"裸 string"的区别。
4. 给 `GenericRepository<T>` 加一个 `T value` 字段，把它改成 `Lifetime.Transient`，
   验证 `GenericRepository<int>` 和 `GenericRepository<string>` 互不干扰。
