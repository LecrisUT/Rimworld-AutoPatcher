using System.Collections.Generic;

namespace AutoPatcher
{
    public abstract class EndNode<T> : NodeDef
    {
        public EndNode()
        {
            baseInPorts = 1;
            baseOutPorts = 0;
            nOutPortGroups = 0;
            nInPortGroups = 1;
            nOutPorts = nOutPortGroups * baseOutPorts;
            nInPorts = nInPortGroups * baseInPorts;
        }
        protected override void CreateInputPortGroup(Node node, int group)
            => node.inputPorts.Add(new Port<T>());
        protected IPort BaseInput(List<IPort> ports) => ports[0];
    }
    public abstract class EndNode<T,InputAT> : EndNode<T>
    {
        public EndNode()
        {
            baseInPorts = 2;
            nInPorts = nInPortGroups * baseInPorts;
        }
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputAT>());
        }
        protected IPort InputA(List<IPort> ports) => ports[1];
    }
    public abstract class EndNode<T, InputAT, InputBT> : EndNode<T, InputAT>
    {
        public EndNode()
        {
            baseInPorts = 3;
            nInPorts = nInPortGroups * baseInPorts;
        }
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputBT>());
        }
        protected IPort InputB(List<IPort> ports) => ports[2];
    }
    public abstract class EndNode<T, InputAT, InputBT, InputCT> : EndNode<T, InputAT, InputBT>
    {
        public EndNode()
        {
            baseInPorts = 4;
            nInPorts = nInPortGroups * baseInPorts;
        }
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputCT>());
        }
        protected IPort InputC(List<IPort> ports) => ports[3];
    }
    public abstract class EndNode<T, InputAT, InputBT, InputCT, InputDT> : EndNode<T, InputAT, InputBT, InputCT>
    {
        public EndNode()
        {
            baseInPorts = 5;
            nInPorts = nInPortGroups * baseInPorts;
        }
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputDT>());
        }
        protected IPort InputD(List<IPort> ports) => ports[4];
    }
}
