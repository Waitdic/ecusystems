using System;

namespace CalibrTable;

public static class TableHelper
{
    extension<TSource, TValue>(TableValues<TSource, TValue> table) where TSource : struct, IComparable<TSource> where TValue : struct, IComparable<TValue>
    {
        public void GetMinMax(out TValue min, out TValue max)
        {
            min = max = default;

            var count = table.Count;
        
            if (count == 0) return;
        
            min = max = table.GetValue(0);
        
            for (var i = 1; i < count; i++)
            {
                var value = table.GetValue(i);
                
                if (value.CompareTo(max) > 0)
                    max = value;
                
                if (value.CompareTo(min) < 0)
                    min = value;
            }
        }

        public TSource[,] Get2DArray()
        {
            var res = new TSource[table.RowCount, table.ColCount];

            for (var i = 0; i < table.RowCount; i++)
            for (var j = 0; j < table.ColCount; j++)
                res[i, j] = table[j, i];

            return res;
        }

        public void Set2DArray(TSource[,] source)
        {
            for (var i = 0; i < table.RowCount; i++)
            for (var j = 0; j < table.ColCount; j++)
                table[j, i] = source[i, j];

            table.FillValues();
        }
    }
}
