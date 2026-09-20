using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson08
{
    public sealed class Lesson08LifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 同一个接口 IWeapon 有三个实现，用 Key 区分。
            // 注意注册顺序 = 集合注入的顺序。
            builder.Register<IWeapon, Sword>(Lifetime.Singleton).Keyed(WeaponType.Primary);
            builder.Register<IWeapon, Bow>(Lifetime.Singleton).Keyed(WeaponType.Secondary);
            builder.Register<IWeapon, MagicStaff>(Lifetime.Singleton).Keyed(WeaponType.Special);

            // 因为 IWeapon 被注册了 3 次，容器自动生成了"集合注册"，
            // 于是 IReadOnlyList<IWeapon> / IEnumerable<IWeapon> 也可以直接注入。

            builder.RegisterEntryPoint<KeyedWeaponUser>();
            builder.RegisterEntryPoint<WeaponCatalog>();
        }
    }
}
