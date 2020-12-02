using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
// using BoxCollider = Unity.Physics.BoxCollider;
using MeshCollider = Unity.Physics.MeshCollider;

public struct ChangeBoxColliderSize : IComponentData
{
    public float3 Value;
    // public bool IsFollowSize;
}

// In general, you should treat colliders as immutable data at run-time, as several bodies might share the same collider.
// If you plan to modify mesh or convex colliders at run-time, remember to tick the Force Unique box on the PhysicsShapeAuthoring component.
// This guarantees that the PhysicsCollider component will have a unique instance in all cases.

// Converted in PhysicsSamplesConversionSystem so Physics and Graphics conversion is over
public class ChangeBoxColliderSizeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    // public bool IsFollowSize;
    public float3 Size;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Debug.Log("ChangeBoxColliderSize");
        dstManager.AddComponentData(entity, new ChangeBoxColliderSize
        {
            // IsFollowSize = IsFollowSize,
            Value = Size,
        });
        // Physics and graphics representations of bodies can be largely independent.
        // Positions and Rotations of each representation are associated through the BuildPhysicsWorld & ExportPhysicsWorld systems.
        // As scale is generally baked for runtime performance, we specifically need to add a scale component here
        // and will update both the graphical and physical scales in our own demo update system.
        dstManager.AddComponentData(entity, new NonUniformScale
        {
            Value = Size,
        });
    }
}

// [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
// [UpdateBefore(typeof(BuildPhysicsWorld))]
public class ChangeBoxColliderSizeSystem : SystemBase
{

    public float3 curSize = new float3(1f, 1f, 1f);
    protected override void OnUpdate()
    {
        Entities
            .WithName("ChangeBoxColliderSize")
            .WithBurst()
            .ForEach(( ref ChangeBoxColliderSize sizeData, ref NonUniformScale scaleUniform) =>
            {
                // make sure we are dealing with spheres
                // if (collider.Value.Value.Type != ColliderType.Box) return;

                // tweak the physical representation of the sphere

                // NOTE: this approach affects all instances using the same BlobAsset
                // so you cannot simply use this approach for instantiated prefabs
                // if you want to modify prefab instances independently, you need to create
                // unique BlobAssets at run-time and dispose them when you are done

                float3 oldSize = scaleUniform.Value;
                float3 targetSize = sizeData.Value;
                float3 newSize = math.lerp(oldSize, targetSize, 0.05f);

                // unsafe
                // {
                //     // grab the sphere pointer
                //     MeshCollider * scPtr = (MeshCollider * ) collider.ColliderPtr;
                //     oldSize = scPtr -> Size;
                //     // newSize = newSize;

                //     // update the collider geometry
                //     var boxGeometry = scPtr -> Geometry;
                //     boxGeometry.Size = newSize;
                //     scPtr -> Geometry = boxGeometry;

                // }
                scaleUniform.Value = newSize;

                // now tweak the graphical representation of the sphere
                // float3 oldScale = scaleUniform.Value;
                // float3 newScale = oldScale;

                // if (oldSize.x == 0.0f)
                // {
                //     // avoid the divide by zero errors.
                //     newScale.x = newSize.x;
                // }
                // else
                // {
                //     newScale.x *= newSize.x / oldSize.x;
                // }
                // if (oldSize.y == 0.0f)
                // {
                //     // avoid the divide by zero errors.
                //     newScale.y = newSize.y;
                // }
                // else
                // {
                //     newScale.y *= newSize.y / oldSize.y;
                // }
                // if (oldSize.z == 0.0f)
                // {
                //     // avoid the divide by zero errors.
                //     newScale.z = newSize.z;
                // }
                // else
                // {
                //     newScale.z *= newSize.z / oldSize.z;
                // }
                // scaleUniform.Value = newScale;

                // Debug.Log($"ChangeBoxColliderSizeSystem");
                Debug.Log($"{scaleUniform.Value}");
            }).Schedule();
    }
}