using System.Text;
using System.Linq;
using System.Collections.Generic;
using Verse;
using HarmonyLib;

namespace AutoPatcher
{
    /// <summary>
    /// Patch tree
    /// </summary>
    public class PatchTreeDef : Def, IExposable, ILoadReferenceable
    {
        /// <summary>
        /// All nodes of the PatchTree
        /// </summary>
        public List<Node> Nodes = new List<Node>();
        /// <summary>
        /// All root nodes with no prior input nodes
        /// </summary>
        public List<Node> RootNodes = new List<Node>();
        /// <summary>
        /// All end nodes with no connected output nodes
        /// </summary>
        public List<Node> EndNodes = new List<Node>();
        /// <summary>
        /// All unconnected nodes
        /// </summary>
        public List<Node> BubbleNodes = new List<Node>();
        /// <summary>
        /// Branches connecting the nodes
        /// </summary>
        public List<PatchTreeBranch> Branches;
        // PatchTree debug message
        public int DebugLevel = 0;
        public StringBuilder DebugMessage;
        public PatchTreeDef()
        {
            // Reset the global node count
            Node.count = 0;
        }
        /// <summary>
        /// Initialize the nodes and branches of the PatchTree
        /// </summary>
        /// <returns></returns>
        public bool Initialize(bool fromSave = false)
        {
            if (fromSave)
                Nodes.Do(t => t.fromSave = true);
            else
            {
                // Initialize all nodes to create the ports
                Nodes.Do(t => t.Initialize(this));
                // Initialize and create all missing branches
                foreach (PatchTreeBranch branch in Branches.ToList())
                    if (!branch.Initialize(this, Nodes))
                    {
                        Log.Error($"[[LC]AutoPatcher] Failed to initialize branch {Branches.IndexOf(branch)} in PatchTree {defName}");
                        return false;
                    }
            }
            var inputNodes = Branches.Select(t => t.inputNode).ToList();
            inputNodes.RemoveDuplicates();
            var outputNodes = Branches.Select(t => t.outputNode).ToList();
            outputNodes.RemoveDuplicates();
            // Calculate how many nodes are merging in each node
            foreach (var inNode in inputNodes)
            {
                foreach (var outNode in outputNodes)
                    if (Branches.Any(t => t.inputNode == inNode && t.outputNode == outNode))
                        inNode.merging++;
            }
            // Identify all node types
            RootNodes = Nodes.Where(t => !inputNodes.Contains(t)).ToList();
            EndNodes = Nodes.Where(t => !outputNodes.Contains(t)).ToList();
            BubbleNodes = RootNodes.Intersect(EndNodes).ToList();
            RootNodes = RootNodes.Except(BubbleNodes).ToList();
            EndNodes = EndNodes.Except(BubbleNodes).ToList();
            if (DebugLevel > 0)
            {
                DebugMessage = new StringBuilder($"[[LC]AutoPatcher]: PatchTree: {defName} : FromSave: {fromSave} : Nodes:\n");
                foreach (var node in Nodes)
                {
                    DebugMessage.AppendLine($"{node}\nInputPorts:");
                    for (int i = 0; i < node.inputPorts.Count; i++)
                        DebugMessage.AppendLine($"[{i}] {node.inputPorts[i].DataType}");
                    DebugMessage.AppendLine("OutputPorts:");
                    for (int i = 0; i < node.outputPorts.Count; i++)
                        DebugMessage.AppendLine($"[{i}] {node.outputPorts[i].DataType}");
                }
                DebugMessage.AppendLine($"\nBranches:");
                Branches.Do(t => DebugMessage.AppendLine($"({t.outputNode} : {t.outputNode.outputPorts.IndexOf(t.outputPort)} : {t.outputPort.DataType}) -> ({t.inputNode} : {t.inputNode.inputPorts.IndexOf(t.inputPort)} : {t.inputPort})"));
                Log.Message(DebugMessage.ToString());
            }
            return true;
        }
        /// <summary>
        /// Execute all the nodes
        /// </summary>
        public void Perform()
        {
            RootNodes.Do(t => Perform(t));
            if (DebugLevel > 0)
                Log.Message(DebugMessage.ToString());
        }
        /// <summary>
        /// Execute a specific node
        /// </summary>
        /// <param name="node"></param>
        protected void Perform(Node node)
        {
            // If not all merging nodes have reached this point wait
            if (++node.current < node.merging)
                return;
            var branches = Branches.Where(t => t.outputNode == node).ToList();
            var inputNodes = branches.Select(t => t.inputNode).ToList();
            inputNodes.RemoveDuplicates();
            node.Prepare();
            node.Perform();
            node.PostPerform();
            node.Pass(branches);
            node.Finish();
            if (EndNodes.Contains(node))
            {
                node.End();
                if (node.DebugLevel > 0)
                    Log.Message(node.DebugMessage.ToString());
                return;
            }
            if (node.DebugLevel > 0)
                Log.Message(node.DebugMessage.ToString());
            foreach (var nextNode in inputNodes)
                Perform(nextNode);
        }
        public void ExposeData()
        {
            /*Scribe_Collections.Look(ref Branches, "Branches", LookMode.Deep);
            Scribe_Collections.Look(ref Nodes, "Nodes", LookMode.Deep);*/
        }
        public string GetUniqueLoadID()
            => defName;
    }
}
