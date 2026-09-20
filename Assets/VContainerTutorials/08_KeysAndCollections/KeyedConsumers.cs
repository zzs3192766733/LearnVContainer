using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson08
{
    /// <summary>
    /// 方式 A：在构造参数上使用 [Key] 指定要注入哪个实现。
    /// </summary>
    public sealed class KeyedWeaponUser : IStartable
    {
        readonly IWeapon primary;
        readonly IWeapon secondary;
        readonly IWeapon special;
        readonly IObjectResolver container;

        public KeyedWeaponUser(
            [Key(WeaponType.Primary)] IWeapon primary,
            [Key(WeaponType.Secondary)] IWeapon secondary,
            [Key(WeaponType.Special)] IWeapon special,
            IObjectResolver container)
        {
            this.primary = primary;
            this.secondary = secondary;
            this.special = special;
            this.container = container;
        }

        void IStartable.Start()
        {
            Debug.Log("[Lesson08] ---- 1. 构造参数上的 [Key] ----");
            Debug.Log($"[Lesson08] 主武器 = {primary.Name} (atk {primary.Attack})");
            Debug.Log($"[Lesson08] 副武器 = {secondary.Name} (atk {secondary.Attack})");
            Debug.Log($"[Lesson08] 特殊武器 = {special.Name} (atk {special.Attack})");

            Debug.Log("[Lesson08] ---- 2. 容器 API 带 Key 解析 ----");

            var again = container.Resolve<IWeapon>(WeaponType.Primary);
            Debug.Log($"[Lesson08] container.Resolve<IWeapon>(WeaponType.Primary) -> {again.Name}，" +
                      $"与注入的是同一实例？ {ReferenceEquals(again, primary)}");

            // TryResolve 的签名是 TryResolve<T>(out T resolved, object key = null)
            if (container.TryResolve<IWeapon>(out _, "missing"))
            {
                Debug.Log("[Lesson08] 不该走到这里");
            }
            else
            {
                Debug.Log("[Lesson08] container.TryResolve<IWeapon>(out _, \"missing\") -> False（不抛异常）");
            }

            // 下面这行会抛异常，因为用了 Keyed 注册之后，不带 Key 的键根本不存在。
            // container.Resolve<IWeapon>();   // ← 取消注释试试
        }
    }

    /// <summary>
    /// 方式 B：不关心具体是哪一个，而是拿到"全部实现"，由自己决定怎么用。
    /// 这通常比 Key 更安全。
    /// </summary>
    public sealed class WeaponCatalog : IStartable
    {
        readonly IReadOnlyList<IWeapon> allWeapons;
        readonly IEnumerable<IWeapon> alsoAllWeapons;

        public WeaponCatalog(IReadOnlyList<IWeapon> allWeapons, IEnumerable<IWeapon> alsoAllWeapons)
        {
            this.allWeapons = allWeapons;
            this.alsoAllWeapons = alsoAllWeapons;
        }

        void IStartable.Start()
        {
            Debug.Log("[Lesson08] ---- 3. 集合注入 ----");
            Debug.Log($"[Lesson08] IReadOnlyList<IWeapon> 共 {allWeapons.Count} 把武器");
            foreach (var weapon in allWeapons)
            {
                Debug.Log($"[Lesson08]   - {weapon.Name} (atk {weapon.Attack})");
            }

            Debug.Log($"[Lesson08] IEnumerable<IWeapon> 也是同一批（{alsoAllWeapons.Count()} 把），顺序 = 注册顺序");
        }
    }
}
