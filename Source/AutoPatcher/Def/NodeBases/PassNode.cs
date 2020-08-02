namespace AutoPatcher
{
    /// <summary>
    /// Pass node type
    /// </summary>
    /// <typeparam name="inT"></typeparam>
    /// <typeparam name="outT"></typeparam>
    public class PassNode<inT,outT> : NodeDef
    {
        public PassNode()
        {
            baseOutPorts = 1;
            baseInPorts = 1;
            nOutPortGroups = 1;
            nInPortGroups = 1;
            nOutPorts = nOutPortGroups * baseOutPorts;
            nInPorts = nInPortGroups * baseInPorts;
        }
        protected override void CreateInputPortGroup(Node node, int group)
            => node.inputPorts.Add(new Port<inT>());
        protected override void CreateOutputPortGroup(Node node, int group)
            => node.outputPorts.Add(new Port<outT>());
    }
}
