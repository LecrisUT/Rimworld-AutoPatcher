using System;
using System.Collections.Generic;

namespace AutoPatcher
{
    /// <summary>
    /// General port interface
    /// </summary>
    public interface IPort
    {
        Type DataType { get; }
        IEnumerable<T> GetData<T>();
        List<T> GetDataList<T>();
        void SetData<T>(List<T> value);
        void AddData<T>(T value);
        void AddData<T>(IEnumerable<T> value);
        void Clear();
        string PrintData();
    }
    /// <summary>
    /// Specific data port interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPort<T> : IPort
    {
        List<T> Data { get; set; }
    }
}
