using System.Collections;
using System.Data.Common;

namespace MyPgsql.Binary;

/// <summary>
/// PostgreSQLパラメータコレクション (DbParameterCollection実装) - バイナリプロトコル版
/// </summary>
public sealed class PgBinaryParameterCollection : DbParameterCollection
{
    private readonly List<PgBinaryParameter> _parameters = [];
    private readonly object _syncRoot = new();

    public override int Count => _parameters.Count;
    public override object SyncRoot => _syncRoot;

    public override int Add(object value)
    {
        _parameters.Add((PgBinaryParameter)value);
        return _parameters.Count - 1;
    }

    public PgBinaryParameter Add(PgBinaryParameter parameter)
    {
        _parameters.Add(parameter);
        return parameter;
    }

    public override void AddRange(Array values)
    {
        foreach (PgBinaryParameter param in values)
        {
            _parameters.Add(param);
        }
    }

    public override void Clear()
    {
        _parameters.Clear();
    }

    public override bool Contains(object value)
    {
        return _parameters.Contains((PgBinaryParameter)value);
    }

    public override bool Contains(string value)
    {
        return _parameters.Exists(p => p.ParameterName == value);
    }

    public override void CopyTo(Array array, int index)
    {
        ((ICollection)_parameters).CopyTo(array, index);
    }

    public override IEnumerator GetEnumerator()
    {
        return _parameters.GetEnumerator();
    }

    public override int IndexOf(object value)
    {
        return _parameters.IndexOf((PgBinaryParameter)value);
    }

    public override int IndexOf(string parameterName)
    {
        return _parameters.FindIndex(p => p.ParameterName == parameterName);
    }

    public override void Insert(int index, object value)
    {
        _parameters.Insert(index, (PgBinaryParameter)value);
    }

    public override void Remove(object value)
    {
        _parameters.Remove((PgBinaryParameter)value);
    }

    public override void RemoveAt(int index)
    {
        _parameters.RemoveAt(index);
    }

    public override void RemoveAt(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
            _parameters.RemoveAt(index);
    }

    protected override DbParameter GetParameter(int index)
    {
        return _parameters[index];
    }

    protected override DbParameter GetParameter(string parameterName)
    {
        return _parameters.Find(p => p.ParameterName == parameterName)
            ?? throw new ArgumentException($"パラメータ '{parameterName}' が見つかりません");
    }

    protected override void SetParameter(int index, DbParameter value)
    {
        _parameters[index] = (PgBinaryParameter)value;
    }

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
            _parameters[index] = (PgBinaryParameter)value;
        else
            _parameters.Add((PgBinaryParameter)value);
    }

    /// <summary>
    /// パラメータを内部リストとして取得（プロトコルハンドラ用）
    /// </summary>
    internal IReadOnlyList<PgBinaryParameter> GetParametersInternal() => _parameters;
}
