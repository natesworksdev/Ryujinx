using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Ryujinx.Tests
{
    public class StaticTheoryData : TheoryData
    {
        public StaticTheoryData(params object[] data)
        {
            List<int> indices = new();
            int length = -1;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] is IList { Count: > 0 } list)
                {
                    indices.Add(i);

                    if (length == -1)
                    {
                        length = list.Count;
                    }
                    else if (length != list.Count)
                    {
                        throw new NotImplementedException($"{nameof(StaticTheoryData)} currently only works with lists of the same size. (expected: {length}, actual: {list.Count})");
                    }
                }
            }

            if (indices.Count == 0)
            {
                AddRow(data);
                return;
            }

            for (int i = 0; i < length; i++)
            {
                object[] row = new object[data.Length];

                for (int j = 0; j < data.Length; j++)
                {
                    if (indices.Contains(j) && data[j] is IList list)
                    {
                        row[j] = list[i];
                    }
                    else
                    {
                        row[j] = data[j];
                    }
                }

                AddRow(row);
            }
        }
    }
}
