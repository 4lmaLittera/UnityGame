using System.Collections.Generic;

namespace AI.BehaviorTree
{
    public enum NodeState { Running, Success, Failure }

    public abstract class Node
    {
        public NodeState State { get; protected set; }
        public abstract NodeState Evaluate();
    }

    public class Selector : Node
    {
        protected List<Node> nodes = new List<Node>();
        public Selector(List<Node> nodes) { this.nodes = nodes; }

        public override NodeState Evaluate()
        {
            foreach (var node in nodes)
            {
                switch (node.Evaluate())
                {
                    case NodeState.Running: State = NodeState.Running; return State;
                    case NodeState.Success: State = NodeState.Success; return State;
                    case NodeState.Failure: continue;
                }
            }
            State = NodeState.Failure;
            return State;
        }
    }

    public class Sequence : Node
    {
        protected List<Node> nodes = new List<Node>();
        public Sequence(List<Node> nodes) { this.nodes = nodes; }

        public override NodeState Evaluate()
        {
            bool anyChildIsRunning = false;
            foreach (var node in nodes)
            {
                switch (node.Evaluate())
                {
                    case NodeState.Failure: State = NodeState.Failure; return State;
                    case NodeState.Success: continue;
                    case NodeState.Running: anyChildIsRunning = true; continue;
                }
            }
            State = anyChildIsRunning ? NodeState.Running : NodeState.Success;
            return State;
        }
    }

    public class ActionNode : Node
    {
        private System.Func<NodeState> _action;
        public ActionNode(System.Func<NodeState> action) { _action = action; }
        public override NodeState Evaluate() => _action();
    }

    public class ConditionNode : Node
    {
        private System.Func<bool> _condition;
        public ConditionNode(System.Func<bool> condition) { _condition = condition; }
        public override NodeState Evaluate() => _condition() ? NodeState.Success : NodeState.Failure;
    }
}
