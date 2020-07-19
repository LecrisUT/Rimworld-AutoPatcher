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
            nOutPortGroups = 1;
            nInPortGroups = 1;
            nOutPorts = nOutPortGroups * 1;
            nInPorts = nInPortGroups * 1;
        }
        protected override void CreateInputPortGroup(Node node, int group)
            => node.inputPorts.Add(new Port<inT>());
        protected override void CreateOutputPortGroup(Node node, int group)
            => node.outputPorts.Add(new Port<outT>());
    }
}
