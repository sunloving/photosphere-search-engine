﻿using System;
using System.Linq;
using Jbta.SearchEngine.Trie.ValueObjects;

namespace Jbta.SearchEngine.Trie.Services
{
    internal class KeyRemover<T>
    {
        private readonly NodeRetriever<T> _nodeRetriever;
        private readonly Node<T> _rootNode;

        public KeyRemover(
            NodeRetriever<T> nodeRetriever,
            Node<T> rootNode)
        {
            _nodeRetriever = nodeRetriever;
            _rootNode = rootNode;
        }

        public void Remove(string key, Func<T, bool> valueSelector)
        {
            var (node, parent) = _nodeRetriever.RetrieveWithParent(_rootNode, key, 0);

            RemoveValue(valueSelector, node);

            if (node.Values.Any())
            {
                return;
            }

            RemoveNode(node, parent);
        }

        private static void RemoveValue(Func<T, bool> valueSelector, Node<T> node)
        {
            foreach (var value in node.Values.Where(valueSelector).ToList())
            {
                node.Values.Remove(value);
            }
        }

        private void RemoveNode(Node<T> node, Node<T> parent)
        {
            if (node.Children.Count < 1)
            {
                parent.Children.Remove(node.Key[0]);
                if (parent.Children.Count == 1 && !parent.Values.Any() && parent != _rootNode)
                {
                    MergeParentWithAloneChild(parent);
                }
            }
            else if (node.Children.Count == 1)
            {
                MergeParentWithAloneChild(node);
            }
        }

        private static void MergeParentWithAloneChild(Node<T> parent)
        {
            var child = parent.Children.Values.First();
            parent.Key = new StringSlice(child.Key.Origin, parent.Key.StartIndex, parent.Key.Length + child.Key.Length);
            parent.Values = child.Values;
            parent.Children.Remove(child.Key[0]);
        }
    }
}