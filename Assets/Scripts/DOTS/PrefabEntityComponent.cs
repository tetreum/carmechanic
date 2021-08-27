using Unity.Entities;

[GenerateAuthoringComponent]
public struct PrefabEntityComponent : IComponentData
{
    public Entity prefab;
}
