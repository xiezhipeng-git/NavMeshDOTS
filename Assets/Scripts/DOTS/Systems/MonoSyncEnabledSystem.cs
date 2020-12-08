using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class MonoSyncEnabledSystem : SystemBase
{
    public Hashtable Monos;
    private EntityCommandBufferSystem commandBufferSystem;

    protected override void OnCreate()
    {
        Monos = new Hashtable();
        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {

        // var commandBuffer = commandBufferSystem.CreateCommandBuffer();

        Entities.WithoutBurst()
            .WithStructuralChanges()

            .ForEach((Entity entity, in MonoSyncEnabledEvent data) =>
            {
                var gameObject = Monos[data.Entity];
                // Debug.Log($"{entity}");
                if (gameObject != null)
                {
                    ((GameObject) gameObject).SetActive(data.Enabled);
                    // Debug.Log(((GameObject) gameObject).name);
                }
                // commandBuffer.DestroyEntity(entity);
                EntityManager.DestroyEntity(entity);

            }).Run();
    }
}