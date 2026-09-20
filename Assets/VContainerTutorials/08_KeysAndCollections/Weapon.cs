namespace VContainerTutorials.Lesson08
{
    public enum WeaponType
    {
        Primary,
        Secondary,
        Special,
    }

    public interface IWeapon
    {
        string Name { get; }
        float Attack { get; }
    }

    public sealed class Sword : IWeapon
    {
        public string Name => "长剑";
        public float Attack => 12f;
    }

    public sealed class Bow : IWeapon
    {
        public string Name => "弓";
        public float Attack => 8f;
    }

    public sealed class MagicStaff : IWeapon
    {
        public string Name => "法杖";
        public float Attack => 20f;
    }
}
