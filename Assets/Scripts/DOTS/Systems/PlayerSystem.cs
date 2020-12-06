using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace DOTS
{
    public class PlayerSystem : SystemBase
    {
        public EntityQuery PlayersQuery;
        // public NativeArray<Entity> Players;
        protected override void OnCreate()
        {
            PlayersQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>());
        }

        public NativeArray<Entity> GetPlayers()
        {
            return PlayersQuery.ToEntityArray(Allocator.Temp);
        }
        protected override void OnUpdate()
        {

            // Debug.Log(PlayersQuery.CalculateEntityCount());
    
        }

        protected override void OnDestroy()
        {
            // Players.Dispose();
        }
    }
}