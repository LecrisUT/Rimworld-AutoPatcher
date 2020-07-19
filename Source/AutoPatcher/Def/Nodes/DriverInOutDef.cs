using System;
using System.Linq;
using RimWorld;
using Verse;

namespace AutoPatcher
{
    // XML interface of generic type is prefered
    public class DriverIn_DefOut<T> : PassNode<Type, T> where T : Def
    {
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            var input = node.inputPorts[0].GetData<Type>();
            var output = DefDatabase<T>.AllDefs;
            output = output.Where(t => input.Contains(DefToDriver(t)));
            node.outputPorts[0].SetData(output.ToList());
            return true;
        }
        protected virtual Type DefToDriver(T def)
            => null;
    }
    public class DefIn_DriverOut<T> : PassNode<T, Type> where T : Def
    {
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            var input = node.inputPorts[0].GetData<T>();
            var output = input.Select(t => DefToDriver(t));
            node.outputPorts[0].SetData(output.ToList());
            return true;
        }
        protected virtual Type DefToDriver(T def)
            => null;
    }
    // Workaround until generic XML interface is coded
    public class JobDriverInDefOut : DriverIn_DefOut<JobDef>
    {
        protected override Type DefToDriver(JobDef def)
            => def.driverClass;
    }
    public class WorkGiverDriverInDefOut : DriverIn_DefOut<WorkGiverDef>
    {
        protected override Type DefToDriver(WorkGiverDef def)
            => def.giverClass;
    }
    public class StatDriverInDefOut : DriverIn_DefOut<StatDef>
    {
        protected override Type DefToDriver(StatDef def)
            => def.workerClass;
    }
    public class JobDefInDriverOut : DefIn_DriverOut<JobDef>
    {
        protected override Type DefToDriver(JobDef def)
            => def.driverClass;
    }
    public class WorkGiverDefInDriverOut : DefIn_DriverOut<WorkGiverDef>
    {
        protected override Type DefToDriver(WorkGiverDef def)
            => def.giverClass;
    }
    public class StatDefInDriverOut : DefIn_DriverOut<StatDef>
    {
        protected override Type DefToDriver(StatDef def)
            => def.workerClass;
    }
}
