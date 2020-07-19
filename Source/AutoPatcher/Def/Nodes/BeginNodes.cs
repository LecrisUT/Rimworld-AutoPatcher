﻿using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using AutoPatcher.Utility;

namespace AutoPatcher
{
    public class BeginDriver : BeginNode<Type>
    {
        private bool allDrivers = false;
        private Type baseDriver;
        private List<Type> driverException = new List<Type>();
        private List<Type> driverList = new List<Type>();
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            if (allDrivers)
                node.outputPorts[0].SetData(GenTypes.AllSubclasses(baseDriver).Where(t=>!driverException.Contains(t)).ToList());
            else
                node.outputPorts[0].SetData(driverList);
            return true;
        }
    }
    public class BeginDef : BeginNode<Def>
    {
        private bool allDefs = false;
        private Type defType;
        private List<Def> defException = new List<Def>();
        private List<Def> defList = new List<Def>();
        private List<string> defExceptionName = new List<string>();
        private List<string> defListName = new List<string>();
        protected override void CreateOutputPortGroup(Node node, int group)
            => node.outputPorts.Add((IPort)Activator.CreateInstance(typeof(Port<>).MakeGenericType(defType)));
        public override void Initialize()
        {
            base.Initialize();
            var allDefs = NodeUtility.getDefListInfo.MakeGenericMethod(defType).Invoke(null, null) as List<Def>;
            defListName.Do(t => defList.Add(allDefs.First(tt => tt.defName == t)));
            defExceptionName.Do(t => defException.Add(allDefs.First(tt => tt.defName == t)));
        }
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            if (allDefs)
                node.outputPorts[0].SetData((NodeUtility.getDefListInfo.MakeGenericMethod(defType).Invoke(null, null) as List<Def>).Where(t => !defException.Contains(t)).ToList());
            else
                node.outputPorts[0].SetData(defList);
            return true;
        }
    }
    public class BeginDef<T> : BeginNode<T> where T : Def
    {
        private bool allDefs = false;
        private List<T> defException = new List<T>();
        private List<T> defList = new List<T>();
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            if (allDefs)
                node.outputPorts[0].SetData(DefDatabase<T>.AllDefsListForReading.Where(t => !defException.Contains(t)).ToList());
            else
                node.outputPorts[0].SetData(defList);
            return true;
        }
    }
}