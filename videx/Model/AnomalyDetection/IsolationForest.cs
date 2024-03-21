using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace videx.Model.AnomalyDetection
{
    public class IsolationForest
    {
        private int _numTrees;
        private int _maxTreeDepth;

        public IsolationForest(int numTrees, int maxTreeDepth)
        {
            _numTrees = numTrees;
            _maxTreeDepth = maxTreeDepth;
        }

        public List<double> Train(AnomalyData[] data)
        {
            int numInstances = data.Length;
            int numFeatures = data[0].Features.Length;

            List<double> avgPathLengths = new List<double>();

            for (int t = 0; t < _numTrees; t++)
            {
                // Shuffle the data for each tree
                Random rand = new Random();
                data = data.OrderBy(x => rand.Next()).ToArray();

                // Convert AnomalyData[] to double[][]
                double[][] featureValues = new double[numInstances][];
                for (int i = 0; i < numInstances; i++)
                {
                    featureValues[i] = new double[numFeatures];
                    for (int j = 0; j < numFeatures; j++)
                    {
                        featureValues[i][j] = data[i].Features[j];
                    }
                }

                // Build a tree
                DecisionTree tree = new DecisionTree(_maxTreeDepth);
                tree.BuildTree(featureValues);

                // Compute average path length for each instance
                double sumPathLengths = 0.0;
                for (int i = 0; i < numInstances; i++)
                {
                    double[] instance = featureValues[i];
                    sumPathLengths += tree.GetPathLength(instance);
                }
                double avgPathLength = sumPathLengths / numInstances;
                avgPathLengths.Add(avgPathLength);
            }

            return avgPathLengths;
        }
    }

    public class DecisionTree
    {
        private int _maxDepth;
        private Node _root;

        public DecisionTree(int maxDepth)
        {
            _maxDepth = maxDepth;
        }

        public void BuildTree(double[][] data)
        {
            int numInstances = data.Length;
            int numFeatures = data[0].Length;

            List<int> indices = Enumerable.Range(0, numInstances).ToList();
            _root = BuildTreeRecursive(data, indices, 0);
        }

        private Node BuildTreeRecursive(double[][] data, List<int> indices, int depth)
        {
            if (depth >= _maxDepth || indices.Count <= 1)
            {
                return new Node(indices.Count);
            }

            int numInstances = indices.Count;
            int numFeatures = data[0].Length;

            // Randomly select a feature
            Random rand = new Random();
            int featureIndex = rand.Next(0, numFeatures);

            // Randomly select a split value
            double minValue = data[indices.Min()][featureIndex];
            double maxValue = data[indices.Max()][featureIndex];
            double splitValue = minValue + rand.NextDouble() * (maxValue - minValue);

            List<int> leftIndices = new List<int>();
            List<int> rightIndices = new List<int>();

            foreach (int index in indices)
            {
                if (data[index][featureIndex] < splitValue)
                    leftIndices.Add(index);
                else
                    rightIndices.Add(index);
            }

            Node leftNode = BuildTreeRecursive(data, leftIndices, depth + 1);
            Node rightNode = BuildTreeRecursive(data, rightIndices, depth + 1);

            return new Node(featureIndex, splitValue, leftNode, rightNode);
        }

        public double GetPathLength(double[] instance)
        {
            return GetPathLengthRecursive(_root, instance, 0);
        }

        private double GetPathLengthRecursive(Node node, double[] instance, double currentDepth)
        {
            if (node.IsLeaf)
                return currentDepth + PathLength(node.Size);

            double featureValue = instance[node.FeatureIndex];
            if (featureValue < node.SplitValue)
                return GetPathLengthRecursive(node.LeftChild, instance, currentDepth + 1);
            else
                return GetPathLengthRecursive(node.RightChild, instance, currentDepth + 1);
        }

        private double PathLength(int size)
        {
            if (size <= 1)
                return 0;

            return 2 * (Math.Log(size - 1) + 0.5772156649) - 2 * (size - 1) / size;
        }
    }

    public class Node
    {
        public int FeatureIndex { get; }
        public double SplitValue { get; }
        public Node LeftChild { get; }
        public Node RightChild { get; }
        public int Size { get; }
        public bool IsLeaf => LeftChild == null && RightChild == null;

        public Node(int size)
        {
            Size = size;
        }

        public Node(int featureIndex, double splitValue, Node leftChild, Node rightChild)
        {
            FeatureIndex = featureIndex;
            SplitValue = splitValue;
            LeftChild = leftChild;
            RightChild = rightChild;
        }
    }
}
