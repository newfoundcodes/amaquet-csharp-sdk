using Amaquet;

namespace Amaquet.Examples;

public static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "list")
        {
            Console.WriteLine(string.Join(Environment.NewLine, Available.Keys));
            return;
        }

        if (!Available.TryGetValue(args[0], out var runner))
        {
            throw new ArgumentException("Unknown example. Use list.");
        }

        await using var client = await AmaquetClient.ConnectAsync(
            Environment.GetEnvironmentVariable("AMAQUET_ENDPOINT")
                ?? "amaquet://127.0.0.1:13378",
            new ClientOptions
            {
                ApiKey = Environment.GetEnvironmentVariable("AMAQUET_API_KEY"),
                ExpectedIdentityFingerprint =
                    Environment.GetEnvironmentVariable(
                        "AMAQUET_IDENTITY_FINGERPRINT"
                    ),
            }
        );

        await runner(client, args.Contains("--operations"));
    }

    private static readonly Dictionary<
        string,
        Func<AmaquetClient, bool, Task>
    > Available = new()
    {
        ["type:adjacency_set"] = TypeAdjacencySet.RunAsync,
        ["type:aggregated_series"] = TypeAggregatedSeries.RunAsync,
        ["type:array"] = TypeArray.RunAsync,
        ["type:atomic_counter"] = TypeAtomicCounter.RunAsync,
        ["type:barrier"] = TypeBarrier.RunAsync,
        ["type:big_decimal"] = TypeBigDecimal.RunAsync,
        ["type:big_integer"] = TypeBigInteger.RunAsync,
        ["type:binary_string"] = TypeBinaryString.RunAsync,
        ["type:bit_field"] = TypeBitField.RunAsync,
        ["type:bit_set"] = TypeBitSet.RunAsync,
        ["type:bitmap"] = TypeBitmap.RunAsync,
        ["type:blocking_queue"] = TypeBlockingQueue.RunAsync,
        ["type:bloom_filter"] = TypeBloomFilter.RunAsync,
        ["type:boolean"] = TypeBoolean.RunAsync,
        ["type:bounding_box"] = TypeBoundingBox.RunAsync,
        ["type:btree_index"] = TypeBtreeIndex.RunAsync,
        ["type:cbor"] = TypeCbor.RunAsync,
        ["type:cidr_network"] = TypeCidrNetwork.RunAsync,
        ["type:consumer_group"] = TypeConsumerGroup.RunAsync,
        ["type:count_min_sketch"] = TypeCountMinSketch.RunAsync,
        ["type:counter_series"] = TypeCounterSeries.RunAsync,
        ["type:counting_bloom_filter"] = TypeCountingBloomFilter.RunAsync,
        ["type:cuckoo_filter"] = TypeCuckooFilter.RunAsync,
        ["type:decimal"] = TypeDecimal.RunAsync,
        ["type:delayed_queue"] = TypeDelayedQueue.RunAsync,
        ["type:dense_vector"] = TypeDenseVector.RunAsync,
        ["type:deque"] = TypeDeque.RunAsync,
        ["type:duration"] = TypeDuration.RunAsync,
        ["type:event_log"] = TypeEventLog.RunAsync,
        ["type:fifo_queue"] = TypeFifoQueue.RunAsync,
        ["type:flat_vector_index"] = TypeFlatVectorIndex.RunAsync,
        ["type:float"] = TypeFloat.RunAsync,
        ["type:full_text_document"] = TypeFullTextDocument.RunAsync,
        ["type:gauge_series"] = TypeGaugeSeries.RunAsync,
        ["type:geo_point"] = TypeGeoPoint.RunAsync,
        ["type:geo_spatial_index"] = TypeGeoSpatialIndex.RunAsync,
        ["type:geospatial_secondary_index"] =
            TypeGeospatialSecondaryIndex.RunAsync,
        ["type:hash_index"] = TypeHashIndex.RunAsync,
        ["type:hashmap"] = TypeHashmap.RunAsync,
        ["type:histogram"] = TypeHistogram.RunAsync,
        ["type:hnsw_index"] = TypeHnswIndex.RunAsync,
        ["type:hyperloglog"] = TypeHyperloglog.RunAsync,
        ["type:integer"] = TypeInteger.RunAsync,
        ["type:inverted_index"] = TypeInvertedIndex.RunAsync,
        ["type:json"] = TypeJson.RunAsync,
        ["type:lease"] = TypeLease.RunAsync,
        ["type:lifo_stack"] = TypeLifoStack.RunAsync,
        ["type:list"] = TypeList.RunAsync,
        ["type:lock"] = TypeLock.RunAsync,
        ["type:matrix"] = TypeMatrix.RunAsync,
        ["type:messagepack"] = TypeMessagepack.RunAsync,
        ["type:multimap"] = TypeMultimap.RunAsync,
        ["type:multiset"] = TypeMultiset.RunAsync,
        ["type:null"] = TypeNull.RunAsync,
        ["type:numeric_field"] = TypeNumericField.RunAsync,
        ["type:ordered_map"] = TypeOrderedMap.RunAsync,
        ["type:ordered_set"] = TypeOrderedSet.RunAsync,
        ["type:persistent_topic"] = TypePersistentTopic.RunAsync,
        ["type:polygon"] = TypePolygon.RunAsync,
        ["type:priority_queue"] = TypePriorityQueue.RunAsync,
        ["type:pubsub"] = TypePubsub.RunAsync,
        ["type:quantized_vector"] = TypeQuantizedVector.RunAsync,
        ["type:radix_tree"] = TypeRadixTree.RunAsync,
        ["type:rate_limit_bucket"] = TypeRateLimitBucket.RunAsync,
        ["type:record"] = TypeRecord.RunAsync,
        ["type:reliable_queue"] = TypeReliableQueue.RunAsync,
        ["type:ring_buffer"] = TypeRingBuffer.RunAsync,
        ["type:roaring_bitmap"] = TypeRoaringBitmap.RunAsync,
        ["type:secondary_index"] = TypeSecondaryIndex.RunAsync,
        ["type:semaphore"] = TypeSemaphore.RunAsync,
        ["type:set"] = TypeSet.RunAsync,
        ["type:sorted_set"] = TypeSortedSet.RunAsync,
        ["type:sparse_vector"] = TypeSparseVector.RunAsync,
        ["type:stream"] = TypeStream.RunAsync,
        ["type:symbol"] = TypeSymbol.RunAsync,
        ["type:t_digest"] = TypeTDigest.RunAsync,
        ["type:tag_field"] = TypeTagField.RunAsync,
        ["type:tensor"] = TypeTensor.RunAsync,
        ["type:text_field"] = TypeTextField.RunAsync,
        ["type:timeseries"] = TypeTimeseries.RunAsync,
        ["type:timeseries_labels"] = TypeTimeseriesLabels.RunAsync,
        ["type:timestamp"] = TypeTimestamp.RunAsync,
        ["type:top_k"] = TypeTopK.RunAsync,
        ["type:trie"] = TypeTrie.RunAsync,
        ["type:tuple"] = TypeTuple.RunAsync,
        ["type:unsigned_integer"] = TypeUnsignedInteger.RunAsync,
        ["type:url"] = TypeUrl.RunAsync,
        ["type:utf8_string"] = TypeUtf8String.RunAsync,
        ["type:uuid"] = TypeUuid.RunAsync,
        ["type:vector_metadata"] = TypeVectorMetadata.RunAsync,
        ["type:vector_set"] = TypeVectorSet.RunAsync,

        ["workflow:01"] = WorkflowWorkflow01Connect.RunAsync,
        ["workflow:02"] = WorkflowWorkflow02KeyLifecycle.RunAsync,
        ["workflow:03"] = WorkflowWorkflow03ExactIntegers.RunAsync,
        ["workflow:04"] = WorkflowWorkflow04Cas.RunAsync,
        ["workflow:05"] = WorkflowWorkflow05Batch.RunAsync,
        ["workflow:06"] = WorkflowWorkflow06Scan.RunAsync,
        ["workflow:07"] = WorkflowWorkflow07Types.RunAsync,
        ["workflow:08"] = WorkflowWorkflow08Sets.RunAsync,
        ["workflow:09"] = WorkflowWorkflow09Hashmap.RunAsync,
        ["workflow:10"] = WorkflowWorkflow10VectorSearch.RunAsync,
        ["workflow:11"] = WorkflowWorkflow11Pubsub.RunAsync,
        ["workflow:12"] = WorkflowWorkflow12BlobUpload.RunAsync,
        ["workflow:13"] = WorkflowWorkflow13BlobDownload.RunAsync,
        ["workflow:14"] = WorkflowWorkflow14AdminHealth.RunAsync,
        ["workflow:15"] = WorkflowWorkflow15AdminMetrics.RunAsync,
        ["workflow:16"] = WorkflowWorkflow16AdminAuth.RunAsync,
        ["workflow:17"] = WorkflowWorkflow17AdminMembers.RunAsync,
        ["workflow:18"] = WorkflowWorkflow18AdminRbac.RunAsync,
        ["workflow:19"] = WorkflowWorkflow19AdminKeys.RunAsync,
        ["workflow:20"] = WorkflowWorkflow20AdminPersistence.RunAsync,
        ["workflow:21"] = WorkflowWorkflow21AdminData.RunAsync,
        ["workflow:22"] = WorkflowWorkflow22AdminAudit.RunAsync,
        ["workflow:23"] = WorkflowWorkflow23Timeouts.RunAsync,
        ["workflow:24"] = WorkflowWorkflow24ConcurrentRequests.RunAsync,
        ["workflow:25"] = WorkflowWorkflow25ConnectionPool.RunAsync,
        ["workflow:26"] = WorkflowWorkflow26Ttl.RunAsync,
        ["workflow:27"] = WorkflowWorkflow27Expiry.RunAsync,
        ["workflow:28"] = WorkflowWorkflow28NestedTypedValues.RunAsync,
        ["workflow:29"] = WorkflowWorkflow29OrderedMap.RunAsync,
        ["workflow:30"] = WorkflowWorkflow30Timeseries.RunAsync,
        ["workflow:31"] = WorkflowWorkflow31BloomFilter.RunAsync,
        ["workflow:32"] = WorkflowWorkflow32Stream.RunAsync,
        ["workflow:33"] = WorkflowWorkflow33Queue.RunAsync,
        ["workflow:34"] = WorkflowWorkflow34Geospatial.RunAsync,
        ["workflow:35"] = WorkflowWorkflow35RateLimit.RunAsync,
        ["workflow:36"] = WorkflowWorkflow36Hnsw.RunAsync,
        ["workflow:37"] = WorkflowWorkflow37Hyperloglog.RunAsync,
        ["workflow:38"] = WorkflowWorkflow38Trie.RunAsync,
        ["workflow:39"] = WorkflowWorkflow39Matrix.RunAsync,
        ["workflow:40"] = WorkflowWorkflow40EventLog.RunAsync,
        ["workflow:41"] = WorkflowWorkflow41Semaphore.RunAsync,
        ["workflow:42"] = WorkflowWorkflow42Cleanup.RunAsync,
    };
}
