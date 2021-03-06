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

        private int count;

        private int depth;

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

        public ThreadsafeTypeHashArrayMap(int initialSize = 64, double factor = 3.0)
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

        private static int CalculateSize(int requestSize)
        {
            uint size = 0;

            for (var i = 1L; i < requestSize; i *= 2)
            {
                size = (size << 1) + 1;
            }

            return (int)(size + 1);
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

        private static int CalculateDepth(Node node)
        {
            var length = 0;

            do
            {
                length++;
                node = node.Next;
            } while (node != null);

            return length;
        }

        private static int CalculateDepth(Node[] targetNodes)
        {
            var depth = 0;

            for (var i = 0; i < targetNodes.Length; i++)
            {
                var node = targetNodes[i];
                if (node != EmptyNode)
                {
                    depth = Math.Max(CalculateDepth(node), depth);
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

        private static Node FindLastNode(Node node)
        {
            while (node.Next != null)
            {
                node = node.Next;
            }

            return node;
        }

        private static void UpdateLink(ref Node node, Node addNode)
        {
            if (node == EmptyNode)
            {
                node = addNode;
            }
            else
            {
                var last = FindLastNode(node);
                last.Next = addNode;
            }
        }

        private static void RelocateNodes(Node[] nodes, Node[] oldNodes)
        {
            for (var i = 0; i < oldNodes.Length; i++)
            {
                var node = oldNodes[i];
                if (node == EmptyNode)
                {
                    continue;
                }

                do
                {
                    var next = node.Next;
                    node.Next = null;

                    UpdateLink(ref nodes[node.Key.GetHashCode() & (nodes.Length - 1)], node);

                    node = next;
                } while (node != null);
            }
        }

        private void AddNode(Node node)
        {
            var requestSize = strategy.CalculateRequestSize(new AddResizeContext(nodes.Length, depth, count, 1));
            var size = CalculateSize(requestSize);
            if (size > nodes.Length)
            {
                var newNodes = new Node[size];
                for (var i = 0; i < newNodes.Length; i++)
                {
                    newNodes[i] = EmptyNode;
                }

                RelocateNodes(newNodes, nodes);

                UpdateLink(ref newNodes[node.Key.GetHashCode() & (newNodes.Length - 1)], node);

                Interlocked.MemoryBarrier();

                nodes = newNodes;
                depth = CalculateDepth(newNodes);
                count = CalculateCount(newNodes);
            }
            else
            {
                Interlocked.MemoryBarrier();

                UpdateLink(ref nodes[node.Key.GetHashCode() & (nodes.Length - 1)], node);

                depth = Math.Max(CalculateDepth(nodes[node.Key.GetHashCode() & (nodes.Length - 1)]), depth);
                count++;
            }
        }

        //--------------------------------------------------------------------------------
        // Public
        //--------------------------------------------------------------------------------

        public int Count
        {
            get
            {
                lock (sync)
                {
                    return count;
                }
            }
        }

        public int Depth
        {
            get
            {
                lock (sync)
                {
                    return depth;
                }
            }
        }

        public void Clear()
        {
            lock (sync)
            {
                var newNodes = CreateInitialTable();

                Interlocked.MemoryBarrier();

                nodes = newNodes;
                count = 0;
                depth = 0;
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

                AddNode(new Node(key, value));

                return value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Performance")]
        public TValue AddIfNotExist(Type key, Func<Type, TValue> valueFactory)
        {
            lock (sync)
            {
                // Double checked locking
                if (TryGetValue(key, out var currentValue))
                {
                    return currentValue;
                }

                var value = valueFactory(key);

                // Check if added by recursive
                if (TryGetValue(key, out currentValue))
                {
                    return currentValue;
                }

                AddNode(new Node(key, value));

                return value;
            }
        }

        //--------------------------------------------------------------------------------
        // IEnumerable
        //--------------------------------------------------------------------------------

        public IEnumerator<KeyValuePair<Type, TValue>> GetEnumerator()
        {
            var copy = nodes;

            for (var i = 0; i < copy.Length; i++)
            {
                var node = copy[i];
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
