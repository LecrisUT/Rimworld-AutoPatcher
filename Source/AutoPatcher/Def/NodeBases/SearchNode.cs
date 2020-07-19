using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

namespace AutoPatcher
{
    public abstract class SearchNode<InOutT, TargetT> : PassNode<InOutT, InOutT>
    {
        public SearchNode()
        {
            baseInPorts = 2;
            baseOutPorts = 2;
            nOutPortGroups = 3;
            nInPortGroups = 1;
            nOutPorts = nOutPortGroups * baseOutPorts;
            nInPorts = nInPortGroups * baseInPorts;
        }
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<TargetT>());
        }
        protected override void CreateOutputPortGroup(Node node, int group)
        {
            base.CreateOutputPortGroup(node, group);
            node.outputPorts.Add(new Port<TargetT>());
        }
        protected bool mergeAmbiguous = false;
        protected bool FindAll = true;
        public List<IPort> FoundPorts(Node node)
            => node.outputPorts.GetRange(0, baseOutPorts);
        public List<IPort> FailedPorts(Node node)
            => node.outputPorts.GetRange(baseOutPorts, baseOutPorts);
        public List<IPort> AmbiguousPorts(Node node)
            => node.outputPorts.GetRange(2 * baseOutPorts, baseOutPorts);
        public IPort Targets(List<IPort> ports)  => ports[1];
        public override bool PostPerform(Node node)
        {
            if (!base.PostPerform(node))
                return false;
            var data = node.inputPorts[0].GetData<InOutT>().ToList();
            FoundPorts(node)[0].GetData<InOutT>().Do(t => data.Remove(t));
            AmbiguousPorts(node)[0].GetData<InOutT>().Do(t => data.Remove(t));
            FailedPorts(node)[0].SetData(data);
            return true;
        }
    }
    public abstract class SearchNode<InOutT, TargetT, ResultAT> : SearchNode<InOutT, TargetT>
    {
        public SearchNode()
        {
            baseOutPorts = 3;
            nOutPorts = nOutPortGroups * baseOutPorts;
        }
        protected override void CreateOutputPortGroup(Node node, int group)
        {
            base.CreateOutputPortGroup(node, group);
            node.outputPorts.Add(new Port<ResultAT>());
        }
        public IPort ResultA(List<IPort> ports) => ports[2];
    }
    public abstract class SearchNode<InOutT, TargetT, ResultAT, ResultBT> : SearchNode<InOutT, TargetT, ResultAT>
    {
        public SearchNode()
        {
            baseOutPorts = 4;
            nOutPorts = nOutPortGroups * baseOutPorts;
        }
        protected override void CreateOutputPortGroup(Node node, int group)
        {
            base.CreateOutputPortGroup(node, group);
            node.outputPorts.Add(new Port<ResultBT>());
        }
        public IPort ResultB(List<IPort> ports) => ports[3];
    }
    public abstract class SearchNode<InOutT, TargetT, ResultAT, ResultBT, ResultCT> : SearchNode<InOutT, TargetT, ResultAT, ResultBT>
    {
        public SearchNode()
        {
            baseOutPorts = 5;
            nOutPorts = nOutPortGroups * baseOutPorts;
        }
        protected override void CreateOutputPortGroup(Node node, int group)
        {
            base.CreateOutputPortGroup(node, group);
            node.outputPorts.Add(new Port<ResultCT>());
        }
        public IPort ResultC(List<IPort> ports) => ports[4];
    }
    public abstract class SearchNode<InOutT, TargetT, ResultAT, ResultBT, ResultCT, ResultDT> : SearchNode<InOutT, TargetT, ResultAT, ResultBT, ResultCT>
    {
        public SearchNode()
        {
            baseOutPorts = 6;
            nOutPorts = nOutPortGroups * baseOutPorts;
        }
        protected override void CreateOutputPortGroup(Node node, int group)
        {
            base.CreateOutputPortGroup(node, group);
            node.outputPorts.Add(new Port<ResultDT>());
        }
        public IPort ResultD(List<IPort> ports) => ports[5];
    }
}
