﻿using System.Collections.Generic;
using Verse;
using AutoPatcher.Utility;
using System.Reflection;
using HarmonyLib;
using System.Linq;
using UnityEngine.VR;
using System.CodeDom;
using System;

namespace AutoPatcher
{
    public abstract class NodeDef : Def
    {
        // Ports and port groups for interfacing
        protected int nOutPorts => baseOutPorts * nOutPortGroups;
        protected int nInPorts => baseInPorts * nInPortGroups;
        protected virtual int baseOutPorts => 0;
        protected virtual int baseInPorts => 0;
        protected virtual int nOutPortGroups => 0;
        protected virtual int nInPortGroups => 0;
        public NodeDef()
        {
            // Have to manually add to a list to access Initialize()
            NodeUtility.allNodeDefs.Add(this);
        }
        // Add Ports to the nodes with appropriate variable type
        protected virtual void CreateInputPortGroup(Node node, int group) { }
        protected virtual void CreateOutputPortGroup(Node node, int group) { }
        // Globally initialize the NodeDef
        public virtual void Initialize() { }
        // Initialize stage: Create the ports for the node
        public virtual bool Initialize(Node node)
        {
            if (node.nodeDef.defName != defName)
            {
                Log.Error($"[[LC]AutoPatcher] Error {defName}: Node passed is not the same defName:\n({node.index} : {node.name} : {node.nodeDef} : {node.nodeDef.defName}) != ({this} : {defName})");
                return false;
            }
            node.inputPorts = new List<IPort>(nInPorts);
            node.inputPortGroups = new List<List<IPort>>(nInPortGroups);
            for (int i = 0; i < nInPortGroups; i++)
            {
                CreateInputPortGroup(node, i);
                node.inputPortGroups.Add(node.inputPorts.GetRange(i * baseInPorts, baseInPorts));
            };
            node.outputPorts = new List<IPort>(nOutPorts);
            node.outputPortGroups = new List<List<IPort>>(nOutPortGroups);
            for (int i = 0; i < nOutPortGroups; i++)
            {
                CreateOutputPortGroup(node, i);
                node.outputPortGroups.Add(node.outputPorts.GetRange(i * baseOutPorts, baseOutPorts));
            }
            return true;
        }
        // Prepare stage: Node specific preparations before Perform stage
        public virtual bool Prepare(Node node)
        {
            var DebugLevel = node.DebugLevel;
            var DebugMessage = node.DebugMessage;
            var inputPorts = node.inputPorts;
            if (DebugLevel > 1)
                DebugMessage.AppendLine($"Prepare Stage: From {node}: Default method:");
            if (DebugLevel > 2)
            {
                DebugMessage.AppendLine("DataPassed:");
                for (int i = 0; i < inputPorts.Count; i++)
                {
                    DebugMessage.AppendLine($"\nInput Port Data ({i}): {inputPorts[i].DataType}");
                    DebugMessage.Append(inputPorts[i].PrintData());
                    DebugMessage.AppendLine();
                }
                DebugMessage.AppendLine();
            }
            return true;
        }
        // Perform stage: Main function of the node
        public virtual bool Perform(Node node) => true;
        // PostPerform stage: Cleanup stage before Pass stage
        public virtual bool PostPerform(Node node) => true;
        // Pass stage: Pass the data to next nodes in the branch list
        public virtual bool Pass(Node node, IEnumerable<PatchTreeBranch> branches)
        {
            var DebugLevel = node.DebugLevel;
            var DebugMessage = node.DebugMessage;
            var outputPorts = node.outputPorts;
            if (DebugLevel > 1)
                DebugMessage.AppendLine($"Pass Stage: From {node}: Default method:");
            if (DebugLevel > 2)
            {
                DebugMessage.AppendLine("DataPassing:");
                for (int i = 0; i < outputPorts.Count; i++)
                {
                    DebugMessage.AppendLine($"\nOutput Port Data ({i}): {outputPorts[i].DataType}");
                    DebugMessage.Append(outputPorts[i].PrintData());
                    DebugMessage.AppendLine();
                }
                DebugMessage.AppendLine();
            }
            if (branches.EnumerableNullOrEmpty())
                return true;
            if (DebugLevel > 1)
                DebugMessage.AppendLine($"Passing to nodes: (outgoingPort - > incomingPort) : [ReceivingNode]");
            foreach (PatchTreeBranch branch in branches)
            {
                Node nextNode = branch.inputNode;
                IPort inputPort = branch.inputPort;
                IPort outputPort = branch.outputPort;
                if (!outputPort.DataType.CanCastTo(inputPort.DataType))
                {
                    Log.Warning($"[[LC]AutoPatcher] Warning couldn't pass from [{node} : {node.outputPorts.IndexOf(outputPort)} : {outputPort.DataType}] to [{nextNode} : {nextNode.inputPorts.IndexOf(inputPort)} : {inputPort.DataType}");
                    continue;
                }
                if (DebugLevel > 1)
                    DebugMessage.AppendLine($"({node.outputPorts.IndexOf(branch.outputPort)} -> {nextNode.inputPorts.IndexOf(branch.inputPort)}) : {nextNode}");
                var method = passMethod.MakeGenericMethod(outputPort.DataType);
                method.Invoke(null, new object[] { outputPort, inputPort });
            }
            return true;
        }
        private static readonly MethodInfo passMethod = AccessTools.Method(typeof(NodeDef), nameof(NodeDef.PassPortData));
        private static void PassPortData<outType>(IPort outPort, IPort inPort)
        {
            var data = outPort.GetData<outType>();
            inPort.AddData(data);
        }
        // Finish stage: Final post process after passing data
        public virtual bool Finish(Node node) => true;
        // End stage: Function to run only if at the end of patch tree
        public virtual bool End(Node node) => true;
    }
   
}
