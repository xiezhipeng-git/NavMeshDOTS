using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace DOTS
{
    [UpdateBefore(typeof(NavMeshPathfindingSystem))]
    public class UpdateFindPathSystem : SystemBase
    {
        public EntityQuery UpdateFindPathEventQuery;
        // public NativeArray<Entity> Players;
        protected override void OnCreate()
        {
            UpdateFindPathEventQuery = GetEntityQuery(ComponentType.ReadOnly<UpdateFindPathEvent>());
        }
        protected override void OnUpdate()
        {
            if (UpdateFindPathEventQuery.CalculateEntityCount() == 0)
            {
                return;
            }
            var events = UpdateFindPathEventQuery.ToComponentDataArray<UpdateFindPathEvent>(Allocator.TempJob);

            Entities
                .WithoutBurst()
                .ForEach((Entity Entity, ref NavMeshPathfindingRequestData requestData, ref Translation translation) =>
                {
                    translation = EntityManager.GetComponentData<Translation>(requestData.Agent);

                }).Run();
            Entities
                .ForEach((Entity Entity, ref NavMeshPathfindingRequestData requestData, ref Translation translation) =>
                {
                    for (var i = 0; i < events.Length; i++)
                    {
                        var distance = math.distance(events[i].Pos.xz, translation.Value.xz);
                        var inRound = distance < events[i].Radius;
                        if (inRound)
                        {
                            // requestData.Status = PathSearchStatus.Requested;
                            requestData.IsDynamicFindPath = true;
                            // Debug.Log("刷新");
                        }
                    }
                }).Schedule();
            CompleteDependency();

            events.Dispose();
            // Entities.ForEach((Entity Entity, ref UpdateFindPathEvent findEvent) =>
            // {
            //     // Implement the work to perform for each entity here.
            //     // You should only access data that is local or that is a
            //     // field on this job. Note that the 'rotation' parameter is
            //     // marked as 'in', which means it cannot be modified,
            //     // but allows this job to run in parallel with other jobs
            //     // that want to read Rotation component data.
            //     // For example,
            //     //     translation.Value += math.mul(rotation.Value, new float3(0, 0, 1)) * deltaTime;
            // }).Schedule();
        }
    }
}