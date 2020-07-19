using System.Text;
using System.Collections.Generic;

namespace AutoPatcher
{
    /// <summary>
    /// Basic node object
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Global index for quick index creation
        /// </summary>
        public static int count = 0;
        // Node driver and port interfaces
        public NodeDef nodeDef;
        public List<IPort> inputPorts;
        public List<IPort> outputPorts;
        public List<List<IPort>> inputPortGroups;
        public List<List<IPort>> outputPortGroups;
        // Sepcific index and name of the node
        public int index;
        public string name;
        // Index for how many branches are merging into this node
        public int current = 0;
        public int merging = 0;
        // Debug functions
        public int DebugLevel = 0;
        public StringBuilder DebugMessage;
        public Node()
        {
            index = count;
            count++;
            DebugMessage = new StringBuilder($"[[LC]AutoPatcher] Debug Node : {printID} : DebugLevel = {DebugLevel}\n");
        }
        // Shortcut methods to the driver's methods
        public bool Initialize() => nodeDef.Initialize(this);
        public bool Prepare() => nodeDef.Prepare(this);
        public bool Perform() => nodeDef.Perform(this);
        public bool PostPerform() => nodeDef.PostPerform(this);
        public bool Pass(IEnumerable<PatchTreeBranch> branches) => nodeDef.Pass(this, branches);
        public bool Finish() => nodeDef.Finish(this);
        public bool End() => nodeDef.End(this);
        public string printID => $"[{index} : {name} : {nodeDef}]";
    }
}
