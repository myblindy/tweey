namespace Twee.Core.Ecs;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class EcsComponentAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class MessageAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class EcsArchetypesAttribute : Attribute
{
}

public interface IEcsPartition
{
    int Width { get; }
    int Height { get; }
}

