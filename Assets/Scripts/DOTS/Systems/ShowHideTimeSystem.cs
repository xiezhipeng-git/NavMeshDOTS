using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class ShowHideTimeSystem : SystemBase
{

    private EntityCommandBufferSystem commandBufferSystem;
    protected override void OnCreate()
    {
        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var commandBuffer = commandBufferSystem.CreateCommandBuffer();

        var timeDelta = Time.DeltaTime;

        Entities.WithStructuralChanges()
            .ForEach((Entity entity, int entityInQueryIndex, ref ShowHideTime data, in Translation translation) =>
            {

                if (data.time > data.timeMax)
                {
                    data.isShow = false;
                    data.time = 0;
                    EntityManager.SetEnabled(entity, false);
                    var monoSyncEventEntity = EntityManager.CreateEntity();
                    EntityManager.AddComponentData<MonoSyncEnabledEvent>(monoSyncEventEntity, new MonoSyncEnabledEvent { Entity = entity, Enabled = false });
                    var updateFindPathEventEntity = EntityManager.CreateEntity();
                    EntityManager.AddComponentData<UpdateFindPathEvent>(updateFindPathEventEntity, new UpdateFindPathEvent { Entity = entity, Radius = 20f, Pos = translation.Value });
                    commandBuffer.DestroyEntity(updateFindPathEventEntity);
                }
                data.time += timeDelta;

                // Implement the work to perform for each entity here.
                // You should only access data that is local or that is a
                // field on this job. Note that the 'rotation' parameter is
                // marked as 'in', which means it cannot be modified,
                // but allows this job to run in parallel with other jobs
                // that want to read Rotation component data.
                // For example,
                //     translation.Value += math.mul(rotation.Value, new float3(0, 0, 1)) * deltaTime;
            }).Run();

        Entities.WithStructuralChanges()
            .ForEach((Entity entity, int entityInQueryIndex, ref ShowHideTime data, ref Disabled disabled, in Translation translation) =>
            {

                if (data.time > data.timeMax)
                {
                    data.isShow = true;
                    data.time = 0;
                    EntityManager.SetEnabled(entity, true);
                    var monoSyncEventEntity = EntityManager.CreateEntity();
                    EntityManager.AddComponentData<MonoSyncEnabledEvent>(monoSyncEventEntity, new MonoSyncEnabledEvent { Entity = entity, Enabled = true });
                    var updateFindPathEventEntity = EntityManager.CreateEntity();
                    EntityManager.AddComponentData<UpdateFindPathEvent>(updateFindPathEventEntity, new UpdateFindPathEvent { Entity = entity, Radius = 20f, Pos = translation.Value });
                    commandBuffer.DestroyEntity(updateFindPathEventEntity);

                }
                data.time += timeDelta;

                // Implement the work to perform for each entity here.
                // You should only access data that is local or that is a
                // field on this job. Note that the 'rotation' parameter is
                // marked as 'in', which means it cannot be modified,
                // but allows this job to run in parallel with other jobs
                // that want to read Rotation component data.
                // For example,
                //     translation.Value += math.mul(rotation.Value, new float3(0, 0, 1)) * deltaTime;
            }).Run();

    }
}