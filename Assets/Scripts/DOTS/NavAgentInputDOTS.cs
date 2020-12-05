using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Ray = UnityEngine.Ray;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

namespace DOTS
{
    /*
     * Copyright (C) Anton Trukhan, 2020.
     */
    public class NavAgentInputDOTS : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        [SerializeField] private Camera Camera;
        public GameObject ObstacleMono;
        public GameObject ObstacleEntityGameobject;

        public Entity ObstacleEntity;
        public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
        {
            gameObjects.Add(ObstacleEntityGameobject);
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            ObstacleEntity = conversionSystem.GetPrimaryEntity(ObstacleEntityGameobject);
            Debug.Log("ObstacleEntity already covert");
        }
        public bool GetInputPos(out float3 resultPos)
        {
            resultPos = new float3(0, 0, 0);
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
                return false;
            }
            resultPos = result.Position;
            return true;
        }

        public void FindPath(float3 position)
        {
            var world = World.DefaultGameObjectInjectionWorld;

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
                        Destination = position,
                        Status = PathSearchStatus.Requested,
                        Agent = entity,
                        Extents = Vector3.one * 2,
                        AgentTypeId = 0
                });
                manager.SetComponentData(entity, new FollowPathData { RequestEntity = requestEntity, PathIndex = 0, PathStatus = PathStatus.Calculated });

            }
        }

        public void AddObstcale(float3 position)
        {
            // AddObstcaleMono(position, ObstacleMono);

            AddObstcaleEntity(position, ObstacleEntity);
        }

        public void AddObstcaleMono(float3 position, GameObject gameobject)
        {
            var ob = GameObject.Instantiate(gameobject);
            ob.transform.localPosition = position;
        }

        public void AddObstcaleEntity(float3 position, Entity entity)
        {
            World.DefaultGameObjectInjectionWorld.EntityManager.Instantiate(entity);
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData<Translation>(entity, new Translation { Value = position });
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var position = new float3();
                if (!GetInputPos(out position))
                {
                    return;
                }

                FindPath(position);

            }
            if (Input.GetMouseButtonDown(1))
            {
                var position = new float3();
                if (!GetInputPos(out position))
                {
                    return;
                }

                AddObstcale(position);

            }
        }

    }
}