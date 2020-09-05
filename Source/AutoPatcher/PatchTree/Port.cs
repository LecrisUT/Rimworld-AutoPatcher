using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using AutoPatcher.Utility;
using System.Collections;

namespace AutoPatcher
{
    // Placehorder for common functions
    public abstract class Port : IExposable, ILoadReferenceable
    {
        public Node node;
        public int portNumber;
        public int portGroup;
        public bool inPort;
        public virtual void ExposeData()
        {
            Scribe_References.Look(ref node, "node");
            Scribe_Values.Look(ref portNumber, "portNumber");
            Scribe_Values.Look(ref portGroup, "portGroup");
        }
        public string GetUniqueLoadID()
            => node.GetUniqueLoadID() + "_Port" + (inPort ? "In" : "Out") + portNumber;
    }
    // Main port interface and data storage
    /// <summary>
    /// Main port interface and data storage for each node.
    /// </summary>
    /// <typeparam name="dataT">data type of the port</typeparam>
    public class Port<dataT> : Port, IPort<dataT>
    {
        public void RegisterPort(Node node, int portNumber, int portGroup, bool inPort)
        {
            this.node = node;
            this.portNumber = portNumber;
            this.portGroup = portGroup;
            this.inPort = inPort;
        }
        public Type DataType { get => typeof(dataT); }
        public List<dataT> data = new List<dataT>();
        // Temporary setup: Force new data to be added instead of overriding.
        public List<dataT> Data { get => data; set => data.AddRange(value); }
        // Casting issues might occur here. Needs deep testing
        /// <summary>
        /// Get data castable to type T
        /// </summary>
        /// <typeparam name="T">Data type to be cast</typeparam>
        /// <returns>Data as type T</returns>
        public IEnumerable<T> GetData<T>() => data.CastTo<dataT,T>();
        public List<T> GetDataList<T>() => data.CastTo<dataT, T>()?.ToList() ?? new List<T>();
        /// <summary>
        /// Replace data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void SetData<T>(List<T> value)
        {
            if (!value.TryCastTo<T,dataT>(out var cvalue))
            {
                data = new List<dataT>();
                return;
            }
            data = cvalue;
            /*if (!typeof(T).CanCastTo(typeof(dataT)))
            {
                data = new List<dataT>();
                return;
            }
            data = value.OfType<dataT>().ToList();*/
        }
        /// <summary>
        /// Add data if castable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void AddData<T>(T value)
        {
            //if (value is dataT cvalue)
            //    data.Add(cvalue);
            //data.Add(value.ChangeType<dataT>());
            //data.Add((dataT)Convert.ChangeType(value, typeof(dataT)));
            if (value.TryCastTo<T, dataT>(out var cvalue))
                data.Add(cvalue);
            //data.Add(value.CastTo<T,dataT>());
        }
        /// <summary>
        /// Add data range if castable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void AddData<T>(IEnumerable<T> value)
        {
            if (value.TryCastTo<T, dataT>(out var cvalue))
                data.AddRange(cvalue);
            /*if (value is IEnumerable<dataT> cvalue)
                data.AddRange(cvalue);
            else
                data.AddRange(value.OfType<dataT>());*/
        }
        /// <summary>
        /// Clear data
        /// </summary>
        public void Clear() => data.Clear();
        /// <summary>
        /// Print data as string for debug purposes
        /// </summary>
        /// <returns></returns>
        public string PrintData()
        {
            if (data.NullOrEmpty())
                return "(empty)";
            StringBuilder builder = new StringBuilder();
            //if (IsTuple(DataType))
            data.Do(t => builder.Append($"{t}, "));
            builder.Length -= 2;
            return builder.ToString();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.LoadingVars && !Controller.fromSave)
                return;
            Scribe_Collections.Look(ref data, "data", LookMode.Undefined);
        }
        // Future work: Change how PrintData performs for List and tuple.
        /*public static bool IsTuple(Type tuple)
        {
            if (!tuple.IsGenericType)
                return false;
            var openType = tuple.GetGenericTypeDefinition();
            return openType == typeof(ValueTuple<>)
                || openType == typeof(ValueTuple<,>)
                || openType == typeof(ValueTuple<,,>)
                || openType == typeof(ValueTuple<,,,>)
                || openType == typeof(ValueTuple<,,,,>)
                || openType == typeof(ValueTuple<,,,,,>)
                || openType == typeof(ValueTuple<,,,,,,>)
                || (openType == typeof(ValueTuple<,,,,,,,>) && IsTuple(tuple.GetGenericArguments()[7]));
        }*/
    }
   
}
