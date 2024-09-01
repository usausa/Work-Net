namespace WorkGraphConsole1;

using System.Diagnostics;

internal class Program
{
    public static void Main()
    {
        var tree = new Tree();
        tree.Add(5);
        tree.Add(3);
        tree.Add(7);
        tree.Add(2);
        tree.Add(4);
        tree.Add(6);
        tree.Add(8);

        tree.AssignPositionsDfs();
        //tree.AssignPositionsBfs();
        tree.PrintTree();
        tree.PrintMatrix();
    }
}

public class Tree
{
    private TreeNode? root;

    public Tree()
    {
        root = null;
    }

    public void Add(int value)
    {
        if (root == null)
        {
            root = new TreeNode(value);
        }
        else
        {
            AddRecursive(root, value);
        }
    }

    private void AddRecursive(TreeNode node, int value)
    {
        if (value < node.Value)
        {
            if (node.Left == null)
            {
                node.Left = new TreeNode(value);
            }
            else
            {
                AddRecursive(node.Left, value);
            }
        }
        else
        {
            if (node.Right == null)
            {
                node.Right = new TreeNode(value);
            }
            else
            {
                AddRecursive(node.Right, value);
            }
        }
    }

    public void AssignPositionsDfs()
    {
        AssignPositionsRecursiveDfs(root, 0, 0);
    }

    private void AssignPositionsRecursiveDfs(TreeNode? node, int depth, int x)
    {
        if (node == null)
        {
            return;
        }

        node.X = x;
        node.Y = depth;

        //AssignPositionsRecursiveDfs(node.Left, depth + 1, x - 1);
        AssignPositionsRecursiveDfs(node.Left, depth + 1, x);
        AssignPositionsRecursiveDfs(node.Right, depth + 1, x + 1);
    }

    public void AssignPositionsBfs()
    {
        if (root == null)
        {
            return;
        }

        var queue = new Queue<(TreeNode, int, int)>();
        queue.Enqueue((root, 0, 0));

        while (queue.Count > 0)
        {
            var (node, depth, x) = queue.Dequeue();
            node.X = x;
            node.Y = depth;

            if (node.Left != null)
            {
                queue.Enqueue((node.Left, depth + 1, x - 1));
            }

            if (node.Right != null)
            {
                queue.Enqueue((node.Right, depth + 1, x + 1));
            }
        }
    }
    public void PrintTree()
    {
        var queue = new Queue<TreeNode>();
        queue.Enqueue(root!);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            Console.WriteLine($"Value: {node.Value}, X: {node.X}, Y: {node.Y}");

            if (node.Left != null)
            {
                queue.Enqueue(node.Left);
            }

            if (node.Right != null)
            {
                queue.Enqueue(node.Right);
            }
        }
    }

    private void GetMinXMaxXAndMaxYRecursive(TreeNode? node, ref int minX, ref int maxX, ref int maxY)
    {
        if (node == null)
        {
            return;
        }

        if (node.X < minX)
        {
            minX = node.X;
        }

        if (node.X > maxX)
        {
            maxX = node.X;
        }

        if (node.Y > maxY)
        {
            maxY = node.Y;
        }

        GetMinXMaxXAndMaxYRecursive(node.Left, ref minX, ref maxX, ref maxY);
        GetMinXMaxXAndMaxYRecursive(node.Right, ref minX, ref maxX, ref maxY);
    }

    private void FillMatrix(TreeNode? node, TreeNode?[,] matrix, int offset)
    {
        if (node == null)
        {
            return;
        }

        matrix[node.X + offset, node.Y] = node;
        FillMatrix(node.Left, matrix, offset);
        FillMatrix(node.Right, matrix, offset);
    }


    public void PrintMatrix()
    {
        var minX = 0;
        var maxX = 0;
        var maxY = 0;
        GetMinXMaxXAndMaxYRecursive(root, ref minX, ref maxX, ref maxY);

        var width = maxX - minX;
        var matrix = new TreeNode?[width + 1, maxY + 1];
        FillMatrix(root, matrix, -minX);

        for (var y = 0; y <= maxY; y++)
        {
            for (var x = 0; x <= width; x++)
            {
                var value = matrix[x, y];
                Debug.Write(value is null ? "-" : value.Value);
                Debug.Write(" ");
            }

            Debug.WriteLine(string.Empty);
        }
    }
}

public class TreeNode
{
    public int Value { get; }

    public TreeNode? Left { get; set; }

    public TreeNode? Right { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public TreeNode(int value)
    {
        Value = value;
        Left = null;
        Right = null;
    }
}
