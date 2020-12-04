using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Ray = UnityEngine.Ray;

namespace DOTS
{
    /*
     * Copyright (C) Anton Trukhan, 2020.
     */
    public class NavAgentInputDOTS : MonoBehaviour
    {
        [SerializeField] private Camera Camera;

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.ScreenPointToRay(Input.mousePosition);

                var world = World.DefaultGameObjectInjectionWorld;
                PhysicsWorld physicsWorld = world.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;

                var raycastInput = new RaycastInput
                {
                    Start = ray.origin,
                    End = ray.origin + ray.direction * int.MaxValue,
                    Filter = CollisionFilter.Default
                };
                if (!physicsWorld.CastRay(raycastInput, out var result))
                {
                    return;
                }

                EntityManager manager = world.EntityManager;

                foreach (var entity in DOTSLocator.AgentEntitys)
                {
                    //Search path
                    var buffer = manager.GetBuffer<PathBufferElement>(entity);
                    var pathData = manager.GetComponentData<FollowPathData>(entity);
                    // if (pathData.PathStatus == PathStatus.EndOfPathReached)
                    // {
                    buffer.Clear();
                    if (pathData.RequestEntity != Entity.Null)
                    {
                        manager.DestroyEntity(pathData.RequestEntity);
                    }
                    // }

                    var translation = manager.GetComponentData<Translation>(entity);
                    var requestEntity = manager.CreateEntity();
                    manager.AddComponentData(requestEntity, new NavMeshPathfindingRequestData
                    {
                        Start = translation.Value,
                            Destination = result.Position,
                            Status = PathSearchStatus.Requested,
                            Agent = entity,
                            Extents = Vector3.one * 2,
                            AgentTypeId = 0
                    });
                    manager.SetComponentData(entity, new FollowPathData { RequestEntity = requestEntity, PathIndex = 0, PathStatus = PathStatus.Calculated });

                }

            }
        }
    }
}