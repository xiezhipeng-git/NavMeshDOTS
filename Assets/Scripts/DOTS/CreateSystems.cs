using System.Collections.Generic;
using DOTS.Systems;
using Unity.Entities;
using UnityEngine;

namespace DOTS
{
    /*
     * Copyright (C) Anton Trukhan, 2020.
     */
    public class CreateSystems : MonoBehaviour
    {
        private List<ComponentSystemBase> systems;

        private void Start()
        {
            systems = new List<ComponentSystemBase>();
            var world = World.DefaultGameObjectInjectionWorld;
            var simulationGroup = world.GetOrCreateSystem<SimulationSystemGroup>();
            simulationGroup.AddSystemToUpdateList(world.GetOrCreateSystem<MoveToDestinationSystem>());
            simulationGroup.AddSystemToUpdateList(world.GetOrCreateSystem<FollowPathSystem>());
            simulationGroup.AddSystemToUpdateList(world.GetOrCreateSystem<NavMeshPathfindingSystem>());

            // simulationGroup.AddSystemToUpdateList( CreateSystem<MoveToDestinationSystem>(world ) );
            // simulationGroup.AddSystemToUpdateList( CreateSystem<FollowPathSystem>(world ) );
            // simulationGroup.AddSystemToUpdateList( CreateSystem<NavMeshPathfindingSystem>(world ) );
        }

        // private T CreateSystem<T>(World world, params object[] args) where T:ComponentSystemBase
        // {
        //     var system = world.AddSystem<T>(new ComponentSystemBase(args));
        //     // CreateSystem<T>(args);
        //     systems.Add(system);
        //     return system;
        // }
    }
}