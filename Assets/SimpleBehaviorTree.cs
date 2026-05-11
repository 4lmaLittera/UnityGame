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

    public enum ParallelPolicy { RequireOne, RequireAll }

    public class Parallel : Node
    {
        protected List<Node> nodes = new List<Node>();
        private readonly ParallelPolicy _successPolicy;
        private readonly ParallelPolicy _failurePolicy;

        public Parallel(List<Node> nodes,
                        ParallelPolicy successPolicy = ParallelPolicy.RequireAll,
                        ParallelPolicy failurePolicy = ParallelPolicy.RequireOne)
        {
            this.nodes = nodes;
            _successPolicy = successPolicy;
            _failurePolicy = failurePolicy;
        }

        public override NodeState Evaluate()
        {
            int successCount = 0;
            int failureCount = 0;

            foreach (var node in nodes)
            {
                switch (node.Evaluate())
                {
                    case NodeState.Success: successCount++; break;
                    case NodeState.Failure: failureCount++; break;
                }
            }

            if (_failurePolicy == ParallelPolicy.RequireOne && failureCount >= 1)
            { State = NodeState.Failure; return State; }
            if (_failurePolicy == ParallelPolicy.RequireAll && failureCount == nodes.Count)
            { State = NodeState.Failure; return State; }

            if (_successPolicy == ParallelPolicy.RequireOne && successCount >= 1)
            { State = NodeState.Success; return State; }
            if (_successPolicy == ParallelPolicy.RequireAll && successCount == nodes.Count)
            { State = NodeState.Success; return State; }

            State = NodeState.Running;
            return State;
        }
    }

    public abstract class Decorator : Node
    {
        protected readonly Node child;
        protected Decorator(Node child) { this.child = child; }
    }

    public class Inverter : Decorator
    {
        public Inverter(Node child) : base(child) { }
        public override NodeState Evaluate()
        {
            switch (child.Evaluate())
            {
                case NodeState.Failure: State = NodeState.Success; break;
                case NodeState.Success: State = NodeState.Failure; break;
                default: State = NodeState.Running; break;
            }
            return State;
        }
    }

    public class Cooldown : Decorator
    {
        private readonly float _cooldown;
        private float _nextAllowedTime;

        public Cooldown(float cooldownSeconds, Node child) : base(child)
        {
            _cooldown = cooldownSeconds;
            _nextAllowedTime = 0f;
        }

        public override NodeState Evaluate()
        {
            if (UnityEngine.Time.time < _nextAllowedTime)
            {
                State = NodeState.Failure;
                return State;
            }

            State = child.Evaluate();
            if (State == NodeState.Success)
                _nextAllowedTime = UnityEngine.Time.time + _cooldown;
            return State;
        }
    }
}
