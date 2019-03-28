using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class StructuredProgramContext
    {
        private BasicBlock[] _blocks;

        private List<(AstBlock Block, int EndIndex)> _blockStack;

        private Dictionary<Operand, AstOperand> _locals;

        private AstBlock _currBlock;

        private int _currEndIndex;

        public StructuredProgramInfo Info { get; }

        public StructuredProgramContext(BasicBlock[] blocks)
        {
            _blocks = blocks;

            _blockStack = new List<(AstBlock, int)>();

            _locals = new Dictionary<Operand, AstOperand>();

            _currBlock = new AstBlock(AstBlockType.Main);

            _currEndIndex = blocks.Length;

            Info = new StructuredProgramInfo(_currBlock);
        }

        public void EnterBlock(BasicBlock block)
        {
            while (_currEndIndex == block.Index)
            {
                PopBlock();
            }

            LookForDoWhileStatements(block);
        }

        public void LeaveBlock(BasicBlock block)
        {
            LookForIfElseStatements(block);
        }

        private void LookForDoWhileStatements(BasicBlock block)
        {
            //Check if we have any predecessor whose index is greater than the
            //current block, this indicates a loop.
            foreach (BasicBlock predecessor in block.Predecessors.OrderByDescending(x => x.Index))
            {
                if (predecessor.Index < block.Index)
                {
                    break;
                }

                if (predecessor.Index < _currEndIndex)
                {
                    Operation branchOp = (Operation)predecessor.GetLastOp();

                    NewBlock(AstBlockType.DoWhile, branchOp, predecessor.Index + 1);
                }
                else /* if (predecessor.Index >= _currEndIndex) */
                {
                    //TODO: Handle unstructured case.
                }
            }
        }

        private void LookForIfElseStatements(BasicBlock block)
        {
            if (block.Branch == null || block.Branch.Index <= block.Index)
            {
                return;
            }

            Operation branchOp = (Operation)block.GetLastOp();

            if (block.Branch.Index <= _currEndIndex)
            {
                //If (conditional branch forward).
                NewBlock(AstBlockType.If, branchOp, block.Branch.Index);
            }
            else if (IsElseSkipBlock(block))
            {
                //Else (unconditional branch forward).
                int topBlockIndex = TopBlockIndexOnStack();

                //We need to pop enough elements so that the one at
                //"topBlockIndex" is the last one poped from the stack.
                while (_blockStack.Count > topBlockIndex)
                {
                    PopBlock();
                }

                NewBlock(AstBlockType.Else, branchOp, block.Branch.Index);
            }
            else if (IsElseSkipBlock(_blocks[_currEndIndex - 1]) && block.Branch == _blocks[_currEndIndex - 1].Branch)
            {
                //If (conditional branch forward).
                NewBlock(AstBlockType.If, branchOp, _currEndIndex);
            }
            else if (block.Branch.Index > _currEndIndex)
            {
                //TODO: Handle unstructured case.
            }
        }

        private bool IsElseSkipBlock(BasicBlock block)
        {
            //Checks performed (in order):
            //- The block should end with a branch.
            //- The branch should be unconditional.
            //- This should be the last block on the current (if) statement.
            //- The statement before the else must be an if statement.
            //- The branch target must be before or at (but not after) the end of the enclosing block.
            if (block.Branch == null || block.Next != null || block.Index + 1 != _currEndIndex)
            {
                return false;
            }

            (AstBlock parentBlock, int parentEndIndex) = _blockStack[TopBlockIndexOnStack()];

            if ((parentBlock.Nodes.Last.Value as AstBlock).Type != AstBlockType.If)
            {
                return false;
            }

            return block.Branch.Index <= parentEndIndex;
        }

        private void NewBlock(AstBlockType type, Operation branchOp, int endIndex)
        {
            Instruction inst = branchOp.Inst;

            if (type == AstBlockType.If)
            {
                //For ifs, the if block is only executed executed if the
                //branch is not taken, that is, if the condition is false.
                //So, we invert the condition to take that into account.
                if (inst == Instruction.BranchIfFalse)
                {
                    inst = Instruction.BranchIfTrue;
                }
                else if (inst == Instruction.BranchIfTrue)
                {
                    inst = Instruction.BranchIfFalse;
                }
            }

            IAstNode cond;

            switch (inst)
            {
                case Instruction.Branch:
                    cond = new AstOperand(OperandType.Constant, IrConsts.True);
                    break;

                case Instruction.BranchIfFalse:
                    cond = new AstOperation(Instruction.BitwiseNot, GetOperandUse(branchOp.GetSource(0)));
                    break;

                case Instruction.BranchIfTrue:
                    cond = GetOperandUse(branchOp.GetSource(0));
                    break;

                default: throw new InvalidOperationException($"Invalid branch instruction \"{branchOp.Inst}\".");
            }

            AstBlock childBlock = new AstBlock(type, cond);

            AddNode(childBlock);

            PushBlock();

            _currBlock = childBlock;

            _currEndIndex = endIndex;
        }

        public void AddNode(IAstNode node)
        {
            _currBlock.Nodes.AddLast(node);
        }

        private void PushBlock()
        {
            _blockStack.Add((_currBlock, _currEndIndex));
        }

        private void PopBlock()
        {
            int lastIndex = _blockStack.Count - 1;

            (_currBlock, _currEndIndex) = _blockStack[lastIndex];

            _blockStack.RemoveAt(lastIndex);
        }

        private int TopBlockIndexOnStack()
        {
            for (int index = _blockStack.Count - 1; index >= 0; index--)
            {
                if (_blockStack[index].EndIndex > _currEndIndex)
                {
                    return index;
                }
            }

            return 0;
        }

        public void PrependLocalDeclarations()
        {
            AstBlock mainBlock = Info.MainBlock;

            LinkedListNode<IAstNode> declNode = null;

            foreach (AstOperand operand in _locals.Values)
            {
                AstDeclaration astDecl = new AstDeclaration(operand);

                if (declNode == null)
                {
                    declNode = mainBlock.Nodes.AddFirst(astDecl);
                }
                else
                {
                    declNode = mainBlock.Nodes.AddAfter(declNode, astDecl);
                }
            }
        }

        public AstOperand GetOperandDef(Operand operand)
        {
            if (TryGetUserAttributeIndex(operand, out int attrIndex))
            {
                Info.OAttributes.Add(attrIndex);
            }

            return GetOperand(operand);
        }

        public AstOperand GetOperandUse(Operand operand)
        {
            if (TryGetUserAttributeIndex(operand, out int attrIndex))
            {
                Info.IAttributes.Add(attrIndex);
            }
            else if (operand.Type == OperandType.ConstantBuffer)
            {
                Info.ConstantBuffers.Add(operand.GetCbufSlot());
            }

            return GetOperand(operand);
        }

        private AstOperand GetOperand(Operand operand)
        {
            if (operand == null)
            {
                return null;
            }

            if (operand.Type != OperandType.LocalVariable)
            {
                return new AstOperand(operand);
            }

            if (!_locals.TryGetValue(operand, out AstOperand astOperand))
            {
                astOperand = new AstOperand(operand);

                _locals.Add(operand, astOperand);
            }

            return astOperand;
        }

        private static bool TryGetUserAttributeIndex(Operand operand, out int attrIndex)
        {
            if (operand.Type == OperandType.Attribute)
            {
                if (operand.Value >= AttributeConsts.UserAttributeBase &&
                    operand.Value <  AttributeConsts.UserAttributeEnd)
                {
                    attrIndex = (operand.Value - AttributeConsts.UserAttributeBase) >> 4;

                    return true;
                }
                else if (operand.Value >= AttributeConsts.FragmentOutputColorBase &&
                         operand.Value <  AttributeConsts.FragmentOutputColorEnd)
                {
                    attrIndex = (operand.Value - AttributeConsts.FragmentOutputColorBase) >> 4;

                    return true;
                }
            }

            attrIndex = 0;

            return false;
        }
    }
}