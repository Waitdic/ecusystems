using System.Collections.Generic;

namespace OpenOLT.DataValueInfo;

internal class ChartSet
{
    public string Name { get; set; }
    public List<ValueInfo> Items { get; private set; }

    public ChartSet()
    {
        Items = [];
    }
}