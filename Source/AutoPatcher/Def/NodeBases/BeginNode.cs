namespace AutoPatcher
{
    public abstract class BeginNode<T> : NodeDef where T : class
    {
        public BeginNode()
        {
            baseInPorts = 0;
            baseOutPorts = 1;
            nOutPortGroups = 1;
            nInPortGroups = 0;
            nOutPorts = nOutPortGroups * baseOutPorts;
            nInPorts = nInPortGroups * baseInPorts;
        }
        protected override void CreateOutputPortGroup(Node node, int group)
            => node.outputPorts.Add(new Port<T>());
    }
}
