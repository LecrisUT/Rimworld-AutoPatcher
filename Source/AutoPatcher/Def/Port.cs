﻿using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Verse;
using HarmonyLib;

namespace AutoPatcher
{
    // Placehorder for common functions
    public abstract class Port
    {

    }
    // Main port interface and data storage
    /// <summary>
    /// Main port interface and data storage for each node.
    /// </summary>
    /// <typeparam name="dataT">data type of the port</typeparam>
    public class Port<dataT> : Port, IPort<dataT>
    {
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
        public IEnumerable<T> GetData<T>()
        {
            if (!typeof(T).IsAssignableFrom(typeof(dataT)))
                yield break;
            foreach (dataT item in data)
                if (item is T cItem)
                    yield return cItem;
        }
        /// <summary>
        /// General get data method castable to type T
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public IEnumerable<object> GetData(Type T)
        {
            if (!T.IsAssignableFrom(typeof(dataT)))
                yield break;
            foreach (dataT item in data)
                if (T.IsAssignableFrom(item.GetType()))
                    yield return item.ChangeType<object>();
        }
        /// <summary>
        /// General get data as object
        /// </summary>
        /// <returns></returns>
        public IEnumerable<object> GetData() => data.Cast<object>();
        /// <summary>
        /// Replace data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void SetData<T>(List<T> value)
        {
            if (!typeof(T).IsAssignableFrom(typeof(dataT)))
            {
                data = new List<dataT>();
                return;
            }
            data = value.Cast<dataT>().ToList();
        }
        /// <summary>
        /// Add data if castable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void AddData<T>(T value)
        {
            if (value is dataT cvalue)
                data.Add(cvalue);
        }
        /// <summary>
        /// Add data range if castable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void AddData<T>(IEnumerable<T> value)
        {
            if (value is IEnumerable<dataT> cvalue)
                data.AddRange(cvalue);
            else
                data.AddRange(value.Cast<dataT>());
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