using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.AI;

namespace DOTS
{
    /*
     * Copyright (C) Anton Trukhan, 2020.
     */
    // [DisableAutoCreation]
    // [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(FollowPathSystem))]
    public class NavMeshPathfindingSystem : SystemBase
    {
        // 这个数值关系到单次寻路距离
        private const int MAXIMUM_POOL_SIZE = 2048;
        private EntityQuery requests;
        private EntityCommandBufferSystem commandBufferSystem;
        private Dictionary<Entity, NavMeshQuery> navMeshQueries;
        // 动态寻路极大影响性能

        protected override void OnCreate()
        {
            navMeshQueries = new Dictionary<Entity, NavMeshQuery>();
            requests = GetEntityQuery(ComponentType.ReadOnly<NavMeshPathfindingRequestData>());
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            // Debug.Log("NavMeshPathfindingSystem OnUpdate began");
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, int entityInQueryIndex, in DeleteFindPathRequest deleteRequest, in NavMeshPathfindingRequestData request) =>
                {
                    navMeshQueries[entity].Dispose();
                    navMeshQueries.Remove(entity);
                    EntityManager.DestroyEntity(entity);
                }).Run();
            var lookup = GetBufferFromEntity<PathBufferElement>();
            var commandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var world = NavMeshWorld.GetDefaultWorld();

            var pathfindingDatas = requests.ToComponentDataArray<NavMeshPathfindingRequestData>(Allocator.TempJob);
            var entities = requests.ToEntityArray(Allocator.TempJob);

            var jobs = new NativeArray<JobHandle>(pathfindingDatas.Length, Allocator.TempJob);
            var findJobs = new List<NavMeshPathfindingJob>();
            var jobListBufferDirct = new Dictionary<Entity, NativeList<PathBufferElement>>();

            var jobRequestResult = new Dictionary<Entity, NativeArray<NavMeshPathfindingRequestData>>();

            // JobHandle jobHandle = Dependency;
            // Debug.Log( entities.Length);

            for (var i = 0; i < entities.Length; ++i)
            {
                var entity = entities[i];

                //Destroy finished requests
                if (navMeshQueries.TryGetValue(entity, out var hasData) && pathfindingDatas[i].IsDynamicFindPath)
                {
                    // Debug.Log("刷新");
                    // 这里是用于重新导航，用于动态寻路，极大影响性能
                    float3 curPos = EntityManager.GetComponentData<Translation>(pathfindingDatas[i].Agent).Value;
                    var entityBuffer = EntityManager.GetBuffer<PathBufferElement>(pathfindingDatas[i].Agent);
                    entityBuffer.Clear();
                    EntityManager.SetComponentData<NavMeshPathfindingRequestData>(entity, new NavMeshPathfindingRequestData
                    {
                        Start = curPos,
                            Destination = pathfindingDatas[i].Destination,
                            Status = PathSearchStatus.Requested,
                            Agent = pathfindingDatas[i].Agent,
                            Extents = pathfindingDatas[i].Extents,
                            AgentTypeId = pathfindingDatas[i].AgentTypeId,
                    });
                    EntityManager.SetComponentData(pathfindingDatas[i].Agent, new FollowPathData { RequestEntity = entity, PathIndex = 0, PathStatus = PathStatus.Calculated });

                    navMeshQueries[entity].Dispose();
                    navMeshQueries.Remove(entity);
                    // EntityManager.DestroyEntity(entity);
                    continue;
                }

                //Process requests in progress
                if (!navMeshQueries.TryGetValue(entity, out var query))
                {
                    query = navMeshQueries[entity] =
                        new NavMeshQuery(world, Allocator.Persistent, MAXIMUM_POOL_SIZE);
                }
                // var entityBuffer =EntityManager.GetBuffer<PathBufferElement>(entity);
                // var nativeListBuffer = new NativeList<PathBufferElement>(Allocator.TempJob);
                if (!jobListBufferDirct.TryGetValue(entity, out var agentBufferJob))
                {
                    agentBufferJob = jobListBufferDirct[entity] = new NativeList<PathBufferElement>(Allocator.TempJob);
                }
                else
                {
                    // jobListBufferDirct[entity].Clear();
                }
                NativeArray<NavMeshPathfindingRequestData> requestResult = new NativeArray<NavMeshPathfindingRequestData>(1, Allocator.TempJob);
                jobRequestResult[entity] = requestResult;
                var navMeshPathfindingJob = new NavMeshPathfindingJob
                {
                    NativeBuffer = jobListBufferDirct[entity],
                    JobIndex = i,
                    Request = pathfindingDatas[i],
                    Query = query,
                    MaximumPoolSize = MAXIMUM_POOL_SIZE,
                    EntityRequestId = entity,
                    RequestReslut = requestResult
                };
                findJobs.Add(navMeshPathfindingJob);
                jobs[i] = navMeshPathfindingJob.Schedule(Dependency);
            }
            // commandBufferSystem.AddJobHandleForProducer(Dependency);

            Dependency = JobHandle.CombineDependencies(jobs);
            NavMeshWorld.GetDefaultWorld().AddDependency(Dependency);
            // Dependency.Complete();

            // Debug.Log($"before CompleteDependency {findJobs.Count}");

            CompleteDependency();
            // var activeJobHandles = findJobs.Select( x => x.Schedule() ).ToArray();

            //  using (var handles = new NativeArray<JobHandle>( activeJobHandles, Allocator.Temp ))
            // {
            //     var combinedJobs = JobHandle.CombineDependencies( handles );
            //     NavMeshWorld.GetDefaultWorld().AddDependency( combinedJobs );
            //     combinedJobs.Complete();
            // }
            // Debug.Log($"on CompleteDependency {findJobs.Count}");
            for (var i = 0; i < findJobs.Count; i++)
            {
                var curJobRequstEntity = findJobs[i].EntityRequestId;

                var entityBuffer = EntityManager.GetBuffer<PathBufferElement>(findJobs[i].Request.Agent);
                // if (findJobs[i].RequestReslut[0].Status != PathSearchStatus.Finished)
                // {
                //     entityBuffer.Clear();
                // }
                entityBuffer.AddRange(findJobs[i].NativeBuffer);
                // Debug.Log($"Buffer length{findJobs[i].NativeBuffer.Length}");
                // Debug.Log(findJobs[i].Request.Status);
                // Debug.Log(findJobs[i].RequestReslut[0].Status);

                // Debug.Log($"test {findJobs[i].test}");

                EntityManager.SetComponentData<NavMeshPathfindingRequestData>(curJobRequstEntity, findJobs[i].RequestReslut[0]);
                // findJobs[i].NativeBuffer.Dispose();
                // jobListBufferDirct[curJobRequstEntity].Dispose();
            }
            foreach (KeyValuePair<Entity, NativeList<PathBufferElement>> item in jobListBufferDirct)
            {
                item.Value.Dispose();
            }
            foreach (KeyValuePair<Entity, NativeArray<NavMeshPathfindingRequestData>> item in jobRequestResult)
            {
                item.Value.Dispose();
            }
            // for (var i = 0; i < entities.Length; ++i)
            // {
            //     var entity = entities[i];

            //     //Destroy finished requests
            //     if (pathfindingDatas[i].Status == PathSearchStatus.Finished)
            //     {
            //         navMeshQueries[entity].Dispose();
            //         navMeshQueries.Remove(entity);
            //         // EntityManager.DestroyEntity(entity);
            //     }
            // }
            // Dependency = jobHandle;
            pathfindingDatas.Dispose();
            entities.Dispose();
            jobs.Dispose();
            // Debug.Log("NavMeshPathfindingSystem OnUpdate end");

            // return jobHandle;
        }

        protected override void OnDestroy()
        {
            foreach (KeyValuePair<Entity, NavMeshQuery> item in navMeshQueries)
            {
                item.Value.Dispose();
                // navMeshQueries.Remove(item.Key);
            }
            navMeshQueries.Clear();
        }

        // [BurstCompile]
        // struct FindPathDataToBufferJob : IJob
        // {
        //     [ReadOnly]
        //     [DeallocateOnJobCompletion] public NativeList<PathBufferElement> NativeBuffer;
        //     [ReadOnly]
        //     [DeallocateOnJobCompletion] public NativeList<Entity> BufferEntitys;

        //     public BufferFromEntity<PathBufferElement> BuffersLookup;

        //     public void Execute()
        //     {
        //         for ()
        //         {

        //         }

        //     }

        // }

        [BurstCompile]
        struct NavMeshPathfindingJob : IJob
        {
            // [NativeDisableParallelForRestriction]
            // [DeallocateOnJobCompletion]
            public NativeList<PathBufferElement> NativeBuffer;
            // public DynamicBuffer<PathBufferElement> NativeBuffer;
            // public BufferFromEntity<PathBufferElement> BuffersLookup;

            [ReadOnly]
            public int JobIndex;

            [ReadOnly]
            public int MaximumPoolSize;

            [ReadOnly]
            public Entity EntityRequestId;

            // public EntityCommandBuffer.ParallelWriter CommandBuffer;

            public NavMeshQuery Query;

            public NavMeshPathfindingRequestData Request;

            public NativeArray<NavMeshPathfindingRequestData> RequestReslut;
            // public NativeArray<NavMeshPathfindingRequestData> test;
            public void Execute()
            {
                // test=2;
                // NativeBuffer = CommandBuffer.AddBuffer<PathBufferElement>(JobIndex, EntityRequestId);
                if (Request.Status == PathSearchStatus.Requested)
                {
                    StartPathSearch(JobIndex, EntityRequestId, Query, ref Request);
                    // Debug.Log($"状态变为开始2 {Request.Status}");

                }
                else
                if (Request.Status == PathSearchStatus.Started)
                {
                    var pathfindingStatus = Query.UpdateFindPath(10, out _);
                    if (pathfindingStatus == PathQueryStatus.Success)
                    {
                        var status = Query.EndFindPath(out int pathSize);

                        // Debug.Log($"数量:{pathSize}");
                        // var pathBuffer = NativeBuffer;
                        // BuffersLookup[Request.Agent];
                        if (status == PathQueryStatus.Success)
                        {
                            // 性能的一点点优化，由于加上会有些穿模，故去除
                            //Path is straight and has no obstacles
                            // if (pathSize == 1)
                            // {
                            //     NativeBuffer.Add(new PathBufferElement { Value = Request.Destination });
                            //     Request.Status = PathSearchStatus.Finished;

                            //     RequestReslut[0] = Request;

                            //     // CommandBuffer.DestroyEntity(JobIndex, EntityRequestId);
                            //     return;
                            // }

                            //Path is complex and needs to be properly extracted

                            CompletePathSearch(JobIndex, EntityRequestId, Query, pathSize,
                                MaximumPoolSize, ref Request, ref NativeBuffer);
                        }
                    }
                }
                RequestReslut[0] = Request;
            }

            private static void StartPathSearch(int jobIndex, Entity entityRequest, NavMeshQuery query,
                ref NavMeshPathfindingRequestData request)
            {

                var from = query.MapLocation(request.Start, request.Extents, request.AgentTypeId);
                var to = query.MapLocation(request.Destination, request.Extents, request.AgentTypeId);
                query.BeginFindPath(from, to);
                // Debug.Log($"from:{from.position.x} {from.position.y} {from.position.z}");
                // Debug.Log($"to:{to.position.x} {to.position.y} {to.position.z}");

                request.Status = PathSearchStatus.Started;
                // commandBuffer.SetComponent(jobIndex, entityRequest, request);
            }

            private static void CompletePathSearch(int jobIndex, Entity entityRequest, NavMeshQuery query,
                int pathSize, int maximumPoolSize,
                ref NavMeshPathfindingRequestData request,
                ref NativeList<PathBufferElement> agentPathBuffer
                // DynamicBuffer<PathBufferElement> agentPathBuffer
            )
            {
                var resultPath = new NativeArray<PolygonId>(pathSize, Allocator.Temp);
                query.GetPathResult(resultPath);

                //Extract path from PolygonId list
                var straightPathCount = 0;
                var straightPath = ExtractPath(query,
                    request.Start, request.Destination,
                    resultPath, maximumPoolSize, ref straightPathCount);

                //Put the result path into buffer
                for (int i = 0; i < straightPathCount; i++)
                {
                    agentPathBuffer.Add(new PathBufferElement { Value = straightPath[i].position });
                    // Debug.Log($"{i}:{straightPath[i].position.x} {straightPath[i].position.y} {straightPath[i].position.z}");
                }

                straightPath.Dispose();
                resultPath.Dispose();

                request.Status = PathSearchStatus.Finished;
                // commandBuffer.SetComponent(jobIndex, entityRequest, request);
            }

            private static NativeArray<NavMeshLocation> ExtractPath(NavMeshQuery query,
                Vector3 startPosition, Vector3 endPosition,
                NativeArray<PolygonId> calculatedPath, int maxPathLength, ref int straightPathCount)
            {
                var pathLength = calculatedPath.Length;
                // var straightPath = new NativeArray<NavMeshLocation>(pathLength, Allocator.Temp);
                // var straightPathFlags = new NativeArray<StraightPathFlags>(pathLength, Allocator.Temp);
                // var vertexSide = new NativeArray<float>(pathLength, Allocator.Temp);
                var straightPath = new NativeList<NavMeshLocation>(pathLength, Allocator.Temp);
                var straightPathFlags = new NativeList<StraightPathFlags>(pathLength, Allocator.Temp);
                // var vertexSide = new NativeList<float>(pathLength, Allocator.Temp);

                var pathStatus = PathUtils.FindStraightPath(query, startPosition, endPosition, calculatedPath,
                    pathLength, ref straightPath,
                    ref straightPathCount, maxPathLength);

                straightPathFlags.Dispose();
                // vertexSide.Dispose();

                return straightPath;
            }
        }
    }
}