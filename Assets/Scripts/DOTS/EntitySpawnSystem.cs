using Unity.Entities;

public class EntitySpawnSystem : ComponentSystem
{
    private float spawnTimer;
    
    protected override void OnUpdate()
    {
        spawnTimer -= Time.DeltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = .5f;
            Entities.ForEach((ref PrefabEntityComponent prefabEntityComponent) =>
            {
                Entity spawnedEntity = EntityManager.Instantiate(prefabEntityComponent.prefab);
                
            });
        }
    }
}
