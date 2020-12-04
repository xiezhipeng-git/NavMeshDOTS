using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

public class DynamicBufferJobSystem : SystemBase
{
    private EntityQuery query;

    protected override void OnCreate()
    {
        //Create a query to find all entities with a dynamic buffer
        // containing MyBufferElement
        EntityQueryDesc queryDescription = new EntityQueryDesc();
        queryDescription.All = new [] { ComponentType.ReadOnly<MyBufferElement>() };
        query = GetEntityQuery(queryDescription);
    }

    public struct BuffersInChunks : IJobChunk
    {
        //The data type and safety object
        public BufferTypeHandle<MyBufferElement> BufferTypeHandle;

        //An array to hold the output, intermediate sums
        public NativeArray<int> sums;
        public int test;
        public void Execute(ArchetypeChunk chunk,
            int chunkIndex,
            int firstEntityIndex)
        {
            test = 2;
            //A buffer accessor is a list of all the buffers in the chunk
            BufferAccessor<MyBufferElement> buffers = chunk.GetBufferAccessor(BufferTypeHandle);

            for (int c = 0; c < chunk.Count; c++)
            {
                //An individual dynamic buffer for a specific entity
                DynamicBuffer<MyBufferElement> buffer = buffers[c];
                for (int i = 0; i < buffer.Length; i++)
                {
                    sums[chunkIndex] += buffer[i].Value;
                }
            }
        }
    }

    //Sums the intermediate results into the final total
    public struct SumResult : IJob
    {
        [DeallocateOnJobCompletion] public NativeArray<int> sums;
        public NativeArray<int> result;
        public int test;
        public NativeArray<int> test2;

        public void Execute()
        {
            test = 2;
            test2[0] = 8;
            for (int i = 0; i < sums.Length; i++)
            {
                result[0] += sums[i];
            }
        }
    }

    protected override void OnUpdate()
    {
        //Create a native array to hold the intermediate sums
        int chunksInQuery = query.CalculateChunkCount();
        NativeArray<int> intermediateSums = new NativeArray<int>(chunksInQuery, Allocator.TempJob);
        var test1 = 0;
        var test2 = 0;
        //Schedule the first job to add all the buffer elements
        BuffersInChunks bufferJob = new BuffersInChunks();
        bufferJob.test = test1;
        bufferJob.BufferTypeHandle = GetBufferTypeHandle<MyBufferElement>();
        bufferJob.sums = intermediateSums;
        this.Dependency = bufferJob.ScheduleParallel(query, this.Dependency);

        //Schedule the second job, which depends on the first
        SumResult finalSumJob = new SumResult();
        finalSumJob.test = test1;
        finalSumJob.sums = intermediateSums;
        NativeArray<int> finalSum = new NativeArray<int>(1, Allocator.TempJob);
        NativeArray<int> test2N = new NativeArray<int>(1, Allocator.TempJob);
        finalSumJob.test2 = test2N;
        finalSumJob.result = finalSum;
        this.Dependency = finalSumJob.Schedule(this.Dependency);

        this.CompleteDependency();
        Debug.Log($"test1 {bufferJob.test}");
        Debug.Log($"test2 {finalSumJob.test2[0]}");
        Debug.Log($"test1 {test1}");
        Debug.Log($"test2 {test2}");
        Debug.Log($"test3 {test2N[0]}");

        Debug.Log("Sum of all buffers: " + finalSum[0]);

        test2N.Dispose();
        finalSum.Dispose();
    }
}