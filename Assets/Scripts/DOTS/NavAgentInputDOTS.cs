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

    public enum RightKeyCreateObj
    {
        ObstacleMono,
        PlayerEntity,
        ObstacleEntity
    }
    public class NavAgentInputDOTS : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        [SerializeField] private Camera Camera;
        public GameObject ObstacleMono;
        public GameObject ObstacleEntityGameobject;
        public GameObject Player;
        public Entity ObstacleEntity;
        public Entity PlayerEntity;
        public RightKeyCreateObj RightKeyCreate;

        public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
        {
            gameObjects.Add(ObstacleEntityGameobject);
            gameObjects.Add(Player);
        }

        private void Awake()
        {
            // Debug.Log("Awake");
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            ObstacleEntity = conversionSystem.GetPrimaryEntity(ObstacleEntityGameobject);
            PlayerEntity = conversionSystem.GetPrimaryEntity(Player);
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
            // Debug.Log("FindPath");
            var world = World.DefaultGameObjectInjectionWorld;

            EntityManager manager = world.EntityManager;
            var players = world.GetOrCreateSystem<PlayerSystem>().GetPlayers();

            foreach (var entity in players)
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
                
                // 默认不进行动态寻路，会影响寻路进行时的性能，当检测到需要动态寻路时开启
                manager.AddComponentData(requestEntity, new NavMeshPathfindingRequestData
                {
                    Start = translation.Value,
                        Destination = position,
                        Status = PathSearchStatus.Requested,
                        Agent = entity,
                        Extents = Vector3.one * 2,
                        AgentTypeId = 0,
                        IsDynamicFindPath = true
                });
                manager.SetComponentData(entity, new FollowPathData { RequestEntity = requestEntity, PathIndex = 0, PathStatus = PathStatus.Calculated });

            }
        }

        public void AddObject(float3 position)
        {
            if (RightKeyCreate == RightKeyCreateObj.ObstacleMono)
            {
                var onFloorPos = new float3(position.x, 1.5f, position.z);
                AddMono(onFloorPos, ObstacleMono);
            }
            if (RightKeyCreate == RightKeyCreateObj.PlayerEntity)
            {
                var onFloorPos = new float3(position.x, 1.5f, position.z);
                Entity e = AddEntity(onFloorPos, PlayerEntity);
                World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData<DestinationData>(e, new DestinationData { Destination = onFloorPos });
            }
            if (RightKeyCreate == RightKeyCreateObj.ObstacleEntity)
            {
                var onFloorPos = new float3(position.x, 1.5f, position.z);
                Entity e = AddEntity(onFloorPos, ObstacleEntity);
            }
        }

        public GameObject AddMono(float3 position, GameObject gameobject)
        {
            var ob = GameObject.Instantiate(gameobject);
            ob.transform.localPosition = position;
            return ob;
        }

        public Entity AddEntity(float3 position, Entity entity)
        {
            World.DefaultGameObjectInjectionWorld.EntityManager.Instantiate(entity);
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData<Translation>(entity, new Translation { Value = position });
            return entity;
        }

        void Update()
        {
            if (Input.GetMouseButton(0))
            {
                var position = new float3();
                if (!GetInputPos(out position))
                {
                    return;
                }

                FindPath(position);

            }
            if (Input.GetMouseButton(1))
            {
                var position = new float3();
                if (!GetInputPos(out position))
                {
                    return;
                }

                AddObject(position);

            }
        }

    }
}