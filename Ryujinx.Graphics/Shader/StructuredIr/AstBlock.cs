using System;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System.Collections;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class AstBlock : AstNode, IEnumerable<IAstNode>
    {
        public AstBlockType Type { get; }

        public IAstNode Condition { get; private set; }

        private LinkedList<IAstNode> _nodes;

        public IAstNode First => _nodes.First?.Value;
        public IAstNode Last  => _nodes.Last?.Value;

        public AstBlock(AstBlockType type, IAstNode condition = null)
        {
            Type      = type;
            Condition = condition;

            _nodes = new LinkedList<IAstNode>();
        }

        public void Add(IAstNode node)
        {
            Add(node, _nodes.AddLast(node));
        }

        public void AddFirst(IAstNode node)
        {
            Add(node, _nodes.AddFirst(node));
        }

        public void AddBefore(IAstNode oldNode, IAstNode node)
        {
            Add(node, _nodes.AddBefore(oldNode.LLNode, node));
        }

        public void AddAfter(IAstNode oldNode, IAstNode node)
        {
            Add(node, _nodes.AddAfter(oldNode.LLNode, node));
        }

        private void Add(IAstNode node, LinkedListNode<IAstNode> newNode)
        {
            if (node.Parent != null)
            {
                throw new ArgumentException("Node already belongs to a block.");
            }

            node.Parent = this;
            node.LLNode = newNode;
        }

        public void Remove(IAstNode node)
        {
            _nodes.Remove(node.LLNode);

            node.Parent = null;
            node.LLNode = null;
        }

        public void AndCondition(IAstNode cond)
        {
            Condition = new AstOperation(Instruction.LogicalAnd, Condition, cond);
        }

        public void OrCondition(IAstNode cond)
        {
            Condition = new AstOperation(Instruction.LogicalOr, Condition, cond);
        }

        public IEnumerator<IAstNode> GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}