using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DOTS
{
    /*
     * Copyright (C) Anton Trukhan, 2020.
     */
    // [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(NavMeshPathfindingSystem))]

    public class FollowPathSystem : SystemBase
    {
        private EntityCommandBufferSystem commandBufferSystem;
        public const float STOPPING_RANGE = 0.5f;
        protected override void OnCreate()
        {

            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            // Debug.Log("FollowPathSystem OnUpdate");

            // var buffersLookup = GetBufferFromEntity<PathBufferElement>();
            // var pathFindSystemDependency = World.GetOrCreateSystem<NavMeshPathfindingSystem>()

            //  Dependency = JobHandle.CombineDependencies(Dependency, .GetOutputDependency());
            // Debug.Log("FollowPathSystem OnUpdate began");

            var commandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();

            var StoppingDistance = STOPPING_RANGE;
            Entities.

            ForEach((Entity entity, int entityInQueryIndex, ref FollowPathData pathData,
                ref DestinationData destinationData, ref Translation translation, ref DynamicBuffer<PathBufferElement> buffer) =>
            {
                if (pathData.PathStatus == PathStatus.EndOfPathReached)
                {
                    return;
                }

                if (pathData.PathStatus == PathStatus.Calculated)
                {
                    pathData.PathStatus = PathStatus.Following;
                    // buffer的第一个点是原地，所以应该从1开始
                    pathData.PathIndex = 1;
                }

                // if (!buffersLookup.HasComponent(entity))
                // {
                //     return;
                // }

                var path = buffer;
                // buffersLookup[entity];
                if (path.Length == 0)
                {
                    return;
                }
                if (pathData.PathIndex > path.Length - 1)
                {
                    return;
                }
                var pos = path[pathData.PathIndex].Value;
                var distance = math.distance(pos.xz, translation.Value.xz);
                var pointReached = distance < StoppingDistance;
                if (pointReached && pathData.PathIndex == path.Length - 1)
                {
                    pathData.PathStatus = PathStatus.EndOfPathReached;
                    buffer.Clear();
                    commandBuffer.AddComponent<DeleteFindPathRequest>(entityInQueryIndex, pathData.RequestEntity, new DeleteFindPathRequest { });
                    // commandBuffer.DestroyEntity(entityInQueryIndex, pathData.RequestEntity);
                    return;
                }

                if (pointReached)
                {
                    pathData.PathIndex++;
                    pos = path[pathData.PathIndex].Value;
                }

                destinationData.Destination = pos;
            }).Schedule();
            CompleteDependency();

            // Debug.Log("FollowPathSystem OnUpdate end");

        }

        //     protected override JobHandle OnUpdate(JobHandle inputDeps)
        //     {
        //         var lookup = GetBufferFromEntity<PathBufferElement>();
        //         var job = new SetPathJob { buffersLookup = lookup, StoppingDistance = STOPPING_RANGE };
        //         var jobHandle = job.Schedule(this, inputDeps);
        //         return jobHandle;
        //     }
        // }

        // [BurstCompile]
        // struct SetPathJob : IJobForEachWithEntity<FollowPathData, DestinationData, Translation>
        // {
        //     [ReadOnly]
        //     public BufferFromEntity<PathBufferElement> buffersLookup;

        //     public float StoppingDistance;

        //     public void Execute(Entity entity, int index, ref FollowPathData pathData,
        //         ref DestinationData destinationData, [ReadOnly] ref Translation translation)
        //     {
        // if (pathData.PathStatus == PathStatus.EndOfPathReached)
        // {
        //     return;
        // }

        // if (pathData.PathStatus == PathStatus.Calculated)
        // {
        //     pathData.PathStatus = PathStatus.Following;
        //     pathData.PathIndex = 0;
        // }

        // if ( !buffersLookup.HasComponent(entity))
        // {
        //     return;
        // }

        // var path = buffersLookup[entity];
        // if (path.Length == 0)
        // {
        //     return;
        // }

        // var pos = path[pathData.PathIndex].Value;
        // var distance = math.distance(pos.xz, translation.Value.xz);
        // var pointReached = distance < StoppingDistance;
        // if (pointReached && pathData.PathIndex == path.Length - 1)
        // {
        //     pathData.PathStatus = PathStatus.EndOfPathReached;
        //     return;
        // }

        // if (pointReached)
        // {
        //     pathData.PathIndex++;
        //     pos = path[pathData.PathIndex].Value;
        // }

        // destinationData.Destination = pos;
        //     }
    }
}