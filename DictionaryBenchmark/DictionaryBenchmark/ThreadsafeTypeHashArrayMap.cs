namespace Smart.Collections.Concurrent
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public sealed class ThreadsafeTypeHashArrayMap<TValue> : IEnumerable<KeyValuePair<Type, TValue>>
    {
        private static readonly Node EmptyNode = new Node(typeof(EmptyKey), default);

        private readonly object sync = new object();

        private readonly IHashArrayMapStrategy strategy;

        private Node[] nodes;

        public void Dump()
        {
            Debug.WriteLine(nodes.Length);
            for (var i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (node != EmptyNode)
                {
                    Debug.Write($"{i:D5} :");
                    do
                    {
                        Debug.Write(" ");
                        Debug.Write(node.Key.Name);
                        node = node.Next;
                    } while (node != null);

                    Debug.WriteLine("");
                }
            }
        }

        //--------------------------------------------------------------------------------
        // Constructor
        //--------------------------------------------------------------------------------

        public ThreadsafeTypeHashArrayMap(int initialSize = 64, double factor = 1.5)
            : this(new GrowthHashArrayMapStrategy(initialSize, factor))
        {
        }

        public ThreadsafeTypeHashArrayMap(IHashArrayMapStrategy strategy)
        {
            this.strategy = strategy;
            nodes = CreateInitialTable();
        }

        //--------------------------------------------------------------------------------
        // Private
        //--------------------------------------------------------------------------------

        private static uint CalculateSize(int count)
        {
            uint size = 0;

            for (var i = 1L; i < count; i *= 2)
            {
                size = (size << 1) + 1;
            }

            return size + 1;
        }

        private static int CalculateCount(Node[] targetNodes)
        {
            var count = 0;
            for (var i = 0; i < targetNodes.Length; i++)
            {
                var node = targetNodes[i];
                if (node != EmptyNode)
                {
                    do
                    {
                        count++;
                        node = node.Next;
                    } while (node != null);
                }
            }

            return count;
        }

        private static int CalculateDepth(Node[] targetNodes)
        {
            var depth = 0;
            for (var i = 0; i < targetNodes.Length; i++)
            {
                var node = targetNodes[i];
                if (node != EmptyNode)
                {
                    var length = 0;

                    do
                    {
                        length++;
                        node = node.Next;
                    } while (node != null);

                    depth = Math.Max(length, depth);
                }
            }

            return depth;
        }

        private Node[] CreateInitialTable()
        {
            var size = CalculateSize(strategy.CalculateInitialSize());
            var newNodes = new Node[size];

            for (var i = 0; i < newNodes.Length; i++)
            {
                newNodes[i] = EmptyNode;
            }

            return newNodes;
        }

        // TODO 以下、更新の見なおし

        private static Node AddNode(Node node, Node addNode)
        {
            if (node is null)
            {
                return addNode;
            }

            var last = node;
            while (last.Next != null)
            {
                last = last.Next;
            }
            last.Next = addNode;

            return node;
        }

        private static void RelocateNodes(Node[] nodes, Node[] oldNodes, int mask)
        {
            for (var i = 0; i < oldNodes.Length; i++)
            {
                var node = oldNodes[i];
                if (node != EmptyNode)
                {
                    do
                    {
                        var next = node.Next;
                        node.Next = null;

                        var relocateIndex = node.Key.GetHashCode() & mask;
                        nodes[relocateIndex] = AddNode(nodes[relocateIndex], node);

                        node = next;
                    } while (node != null);
                }
            }
        }

        private static void FillEmptyIfNull(Node[] targetNodes)
        {
            for (var i = 0; i < targetNodes.Length; i++)
            {
                if (targetNodes[i] is null)
                {
                    targetNodes[i] = EmptyNode;
                }
            }
        }

        private Node[] CreateAddTable(Node node)
        {
            // TODO 配置最適化 & メモリバリアはこの中
            var depth = CalculateDepth(nodes);
            var count = CalculateCount(nodes);
            var requestSize = strategy.CalculateRequestSize(new AddResizeContext(nodes.Length, depth, count, 1));

            var size = CalculateSize(requestSize);
            var mask = (int)(size - 1);
            var newNodes = new Node[size];

            RelocateNodes(newNodes, nodes, mask);

            var index = node.Key.GetHashCode() & mask;
            newNodes[index] = AddNode(newNodes[index], node);

            FillEmptyIfNull(newNodes);

            return newNodes;
        }

        //private Table CreateAddRangeTable(ICollection<Node> addNodes)
        //{
        //    var requestSize = strategy.CalculateRequestSize(new AddResizeContext(oldTable.Nodes.Length, oldTable.Depth, oldTable.Count, addNodes.Count));

        //    var size = CalculateSize(requestSize);
        //    var mask = (int)(size - 1);
        //    var newNodes = new Node[size];

        //    RelocateNodes(newNodes, oldTable.Nodes, mask);

        //    foreach (var node in addNodes)
        //    {
        //        var index = node.Key.GetHashCode() & mask;
        //        newNodes[index] = AddNode(newNodes[index], node);
        //    }

        //    FillEmptyIfNull(newNodes);

        //    return new Table(newNodes, oldTable.Count + addNodes.Count, CalculateDepth(newNodes));
        //}

        //--------------------------------------------------------------------------------
        // Public
        //--------------------------------------------------------------------------------

        public int Count => CalculateCount(nodes);

        public int Depth => CalculateDepth(nodes);

        public void Clear()
        {
            lock (sync)
            {
                // TODO バリア移動？
                var newNodes = CreateInitialTable();
                Interlocked.MemoryBarrier();
                nodes = newNodes;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Performance")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(Type key, out TValue value)
        {
            var node = nodes[key.GetHashCode() & (nodes.Length - 1)];
            do
            {
                if (node.Key == key)
                {
                    value = node.Value;
                    return true;
                }
                node = node.Next;
            } while (node != null);

            value = default;
            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Performance")]
        public TValue AddIfNotExist(Type key, TValue value)
        {
            lock (sync)
            {
                // Double checked locking
                if (TryGetValue(key, out var currentValue))
                {
                    return currentValue;
                }

                // TODO 中へ移動
                // Rebuild
                var newTable = CreateAddTable(new Node(key, value));
                Interlocked.MemoryBarrier();
                nodes = newTable;

                return value;
            }
        }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Performance")]
        //public TValue AddIfNotExist(Type key, Func<Type, TValue> valueFactory)
        //{
        //    lock (sync)
        //    {
        //        // Double checked locking
        //        if (TryGetValueInternal(table, key, out var currentValue))
        //        {
        //            return currentValue;
        //        }

        //        var value = valueFactory(key);

        //        // Check if added by recursive
        //        if (TryGetValueInternal(table, key, out currentValue))
        //        {
        //            return currentValue;
        //        }

        //        // Rebuild
        //        var newTable = CreateAddTable(table, new Node(key, value));
        //        Interlocked.MemoryBarrier();
        //        table = newTable;

        //        return value;
        //    }
        //}

        //public int AddRangeIfNotExist(IEnumerable<KeyValuePair<Type, TValue>> pairs)
        //{
        //    lock (sync)
        //    {
        //        var nodes = pairs
        //            .GroupBy(x => x.Key, (key, g) => g.First())
        //            .Where(x => !TryGetValueInternal(table, x.Key, out _))
        //            .Select(x => new Node(x.Key, x.Value))
        //            .ToList();

        //        // Rebuild
        //        var newTable = CreateAddRangeTable(table, nodes);
        //        Interlocked.MemoryBarrier();
        //        table = newTable;

        //        return nodes.Count;
        //    }
        //}

        //public int AddRangeIfNotExist(IEnumerable<Type> keys, Func<Type, TValue> valueFactory)
        //{
        //    lock (sync)
        //    {
        //        var nodes = keys
        //            .Distinct()
        //            .Where(x => !TryGetValueInternal(table, x, out _))
        //            .Select(x => new KeyValuePair<Type, TValue>(x, valueFactory(x)))
        //            .Where(x => !TryGetValueInternal(table, x.Key, out _))
        //            .Select(x => new Node(x.Key, x.Value))
        //            .ToList();

        //        // Rebuild
        //        var newTable = CreateAddRangeTable(table, nodes);
        //        Interlocked.MemoryBarrier();
        //        table = newTable;

        //        return nodes.Count;
        //    }
        //}

        //--------------------------------------------------------------------------------
        // IEnumerable
        //--------------------------------------------------------------------------------

        public IEnumerator<KeyValuePair<Type, TValue>> GetEnumerator()
        {
            // TODO local
            for (var i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (node != EmptyNode)
                {
                    do
                    {
                        yield return new KeyValuePair<Type, TValue>(node.Key, node.Value);
                        node = node.Next;
                    } while (node != null);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        //--------------------------------------------------------------------------------
        // Helper
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(Type key)
        {
            return TryGetValue(key, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue GetValueOrDefault(Type key, TValue defaultValue = default)
        {
            return TryGetValue(key, out var value) ? value : defaultValue;
        }

        //--------------------------------------------------------------------------------
        // Inner
        //--------------------------------------------------------------------------------

        private class EmptyKey
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Performance")]
        private sealed class Node
        {
            public readonly Type Key;

            public readonly TValue Value;

            public Node Next;

            public Node(Type key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        private sealed class AddResizeContext : IHashArrayMapResizeContext
        {
            public int Width { get; }

            public int Depth { get; }

            public int Count { get; }

            public int Growth { get; }

            public AddResizeContext(int width, int depth, int count, int growth)
            {
                Width = width;
                Count = count;
                Depth = depth;
                Growth = growth;
            }
        }
    }
}
