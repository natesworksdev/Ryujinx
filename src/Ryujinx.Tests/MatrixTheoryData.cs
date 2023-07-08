using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Xunit;

namespace Ryujinx.Tests
{
    public class MatrixTheoryData<T1, T2, T3> : TheoryData<T1, T2, T3>
    {
        public MatrixTheoryData(IEnumerable<T1> data1, IEnumerable<T2> data2, IEnumerable<T3> data3)
        {
            Contract.Assert(data1 != null && data1.Any());
            Contract.Assert(data2 != null && data2.Any());
            Contract.Assert(data3 != null && data3.Any());

            foreach (T1 t1 in data1)
            {
                foreach (T2 t2 in data2)
                {
                    foreach (T3 t3 in data3)
                    {
                        Add(t1, t2, t3);
                    }
                }
            }
        }
    }

    public class MatrixTheoryData<T1, T2, T3, T4> : TheoryData<T1, T2, T3, T4>
    {
        public MatrixTheoryData(IEnumerable<T1> data1, IEnumerable<T2> data2, IEnumerable<T3> data3, IEnumerable<T4> data4)
        {
            Contract.Assert(data1 != null && data1.Any());
            Contract.Assert(data2 != null && data2.Any());
            Contract.Assert(data3 != null && data3.Any());
            Contract.Assert(data4 != null && data4.Any());

            foreach (T1 t1 in data1)
            {
                foreach (T2 t2 in data2)
                {
                    foreach (T3 t3 in data3)
                    {
                        foreach (T4 t4 in data4)
                        {
                            Add(t1, t2, t3, t4);
                        }
                    }
                }
            }
        }
    }

    public class MatrixTheoryData<T1, T2, T3, T4, T5> : TheoryData<T1, T2, T3, T4, T5>
    {
        public MatrixTheoryData(IEnumerable<T1> data1, IEnumerable<T2> data2, IEnumerable<T3> data3, IEnumerable<T4> data4, IEnumerable<T5> data5)
        {
            Contract.Assert(data1 != null && data1.Any());
            Contract.Assert(data2 != null && data2.Any());
            Contract.Assert(data3 != null && data3.Any());
            Contract.Assert(data4 != null && data4.Any());
            Contract.Assert(data5 != null && data5.Any());

            foreach (T1 t1 in data1)
            {
                foreach (T2 t2 in data2)
                {
                    foreach (T3 t3 in data3)
                    {
                        foreach (T4 t4 in data4)
                        {
                            foreach (T5 t5 in data5)
                            {
                                Add(t1, t2, t3, t4, t5);
                            }
                        }
                    }
                }
            }
        }
    }

    public class MatrixTheoryData<T1, T2, T3, T4, T5, T6> : TheoryData<T1, T2, T3, T4, T5, T6>
    {
        public MatrixTheoryData(IEnumerable<T1> data1, IEnumerable<T2> data2, IEnumerable<T3> data3, IEnumerable<T4> data4, IEnumerable<T5> data5, IEnumerable<T6> data6)
        {
            Contract.Assert(data1 != null && data1.Any());
            Contract.Assert(data2 != null && data2.Any());
            Contract.Assert(data3 != null && data3.Any());
            Contract.Assert(data4 != null && data4.Any());
            Contract.Assert(data5 != null && data5.Any());
            Contract.Assert(data6 != null && data6.Any());

            foreach (T1 t1 in data1)
            {
                foreach (T2 t2 in data2)
                {
                    foreach (T3 t3 in data3)
                    {
                        foreach (T4 t4 in data4)
                        {
                            foreach (T5 t5 in data5)
                            {
                                foreach (T6 t6 in data6)
                                {
                                    Add(t1, t2, t3, t4, t5, t6);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class MatrixTheoryData<T1, T2, T3, T4, T5, T6, T7> : TheoryData<T1, T2, T3, T4, T5, T6, T7>
    {
        public MatrixTheoryData(IEnumerable<T1> data1, IEnumerable<T2> data2, IEnumerable<T3> data3, IEnumerable<T4> data4, IEnumerable<T5> data5, IEnumerable<T6> data6, IEnumerable<T7> data7)
        {
            Contract.Assert(data1 != null && data1.Any());
            Contract.Assert(data2 != null && data2.Any());
            Contract.Assert(data3 != null && data3.Any());
            Contract.Assert(data4 != null && data4.Any());
            Contract.Assert(data5 != null && data5.Any());
            Contract.Assert(data6 != null && data6.Any());
            Contract.Assert(data7 != null && data7.Any());

            foreach (T1 t1 in data1)
            {
                foreach (T2 t2 in data2)
                {
                    foreach (T3 t3 in data3)
                    {
                        foreach (T4 t4 in data4)
                        {
                            foreach (T5 t5 in data5)
                            {
                                foreach (T6 t6 in data6)
                                {
                                    foreach (T7 t7 in data7)
                                    {
                                        Add(t1, t2, t3, t4, t5, t6, t7);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class MatrixTheoryData<T1, T2, T3, T4, T5, T6, T7, T8> : TheoryData<T1, T2, T3, T4, T5, T6, T7, T8>
    {
        public MatrixTheoryData(IEnumerable<T1> data1, IEnumerable<T2> data2, IEnumerable<T3> data3, IEnumerable<T4> data4, IEnumerable<T5> data5, IEnumerable<T6> data6, IEnumerable<T7> data7, IEnumerable<T8> data8)
        {
            Contract.Assert(data1 != null && data1.Any());
            Contract.Assert(data2 != null && data2.Any());
            Contract.Assert(data3 != null && data3.Any());
            Contract.Assert(data4 != null && data4.Any());
            Contract.Assert(data5 != null && data5.Any());
            Contract.Assert(data6 != null && data6.Any());
            Contract.Assert(data7 != null && data7.Any());
            Contract.Assert(data8 != null && data8.Any());

            foreach (T1 t1 in data1)
            {
                foreach (T2 t2 in data2)
                {
                    foreach (T3 t3 in data3)
                    {
                        foreach (T4 t4 in data4)
                        {
                            foreach (T5 t5 in data5)
                            {
                                foreach (T6 t6 in data6)
                                {
                                    foreach (T7 t7 in data7)
                                    {
                                        foreach (T8 t8 in data8)
                                        {
                                            Add(t1, t2, t3, t4, t5, t6, t7, t8);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class MatrixTheoryData<T1, T2, T3, T4, T5, T6, T7, T8, T9> : TheoryData<T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        public MatrixTheoryData(IEnumerable<T1> data1, IEnumerable<T2> data2, IEnumerable<T3> data3, IEnumerable<T4> data4, IEnumerable<T5> data5, IEnumerable<T6> data6, IEnumerable<T7> data7, IEnumerable<T8> data8, IEnumerable<T9> data9)
        {
            Contract.Assert(data1 != null && data1.Any());
            Contract.Assert(data2 != null && data2.Any());
            Contract.Assert(data3 != null && data3.Any());
            Contract.Assert(data4 != null && data4.Any());
            Contract.Assert(data5 != null && data5.Any());
            Contract.Assert(data6 != null && data6.Any());
            Contract.Assert(data7 != null && data7.Any());
            Contract.Assert(data8 != null && data8.Any());
            Contract.Assert(data9 != null && data9.Any());

            foreach (T1 t1 in data1)
            {
                foreach (T2 t2 in data2)
                {
                    foreach (T3 t3 in data3)
                    {
                        foreach (T4 t4 in data4)
                        {
                            foreach (T5 t5 in data5)
                            {
                                foreach (T6 t6 in data6)
                                {
                                    foreach (T7 t7 in data7)
                                    {
                                        foreach (T8 t8 in data8)
                                        {
                                            foreach (T9 t9 in data9)
                                            {
                                                Add(t1, t2, t3, t4, t5, t6, t7, t8, t9);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class MatrixTheoryData<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : TheoryData<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    {
        public MatrixTheoryData(IEnumerable<T1> data1, IEnumerable<T2> data2, IEnumerable<T3> data3, IEnumerable<T4> data4, IEnumerable<T5> data5, IEnumerable<T6> data6, IEnumerable<T7> data7, IEnumerable<T8> data8, IEnumerable<T9> data9, IEnumerable<T10> data10)
        {
            Contract.Assert(data1 != null && data1.Any());
            Contract.Assert(data2 != null && data2.Any());
            Contract.Assert(data3 != null && data3.Any());
            Contract.Assert(data4 != null && data4.Any());
            Contract.Assert(data5 != null && data5.Any());
            Contract.Assert(data6 != null && data6.Any());
            Contract.Assert(data7 != null && data7.Any());
            Contract.Assert(data8 != null && data8.Any());
            Contract.Assert(data9 != null && data9.Any());
            Contract.Assert(data10 != null && data10.Any());

            foreach (T1 t1 in data1)
            {
                foreach (T2 t2 in data2)
                {
                    foreach (T3 t3 in data3)
                    {
                        foreach (T4 t4 in data4)
                        {
                            foreach (T5 t5 in data5)
                            {
                                foreach (T6 t6 in data6)
                                {
                                    foreach (T7 t7 in data7)
                                    {
                                        foreach (T8 t8 in data8)
                                        {
                                            foreach (T9 t9 in data9)
                                            {
                                                foreach (T10 t10 in data10)
                                                {
                                                    Add(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class MatrixTheoryData<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : TheoryData
    {
        public MatrixTheoryData(IEnumerable<T1> data1, IEnumerable<T2> data2, IEnumerable<T3> data3, IEnumerable<T4> data4, IEnumerable<T5> data5, IEnumerable<T6> data6, IEnumerable<T7> data7, IEnumerable<T8> data8, IEnumerable<T9> data9, IEnumerable<T10> data10, IEnumerable<T11> data11)
        {
            Contract.Assert(data1 != null && data1.Any());
            Contract.Assert(data2 != null && data2.Any());
            Contract.Assert(data3 != null && data3.Any());
            Contract.Assert(data4 != null && data4.Any());
            Contract.Assert(data5 != null && data5.Any());
            Contract.Assert(data6 != null && data6.Any());
            Contract.Assert(data7 != null && data7.Any());
            Contract.Assert(data8 != null && data8.Any());
            Contract.Assert(data9 != null && data9.Any());
            Contract.Assert(data10 != null && data10.Any());
            Contract.Assert(data11 != null && data11.Any());

            foreach (T1 t1 in data1)
            {
                foreach (T2 t2 in data2)
                {
                    foreach (T3 t3 in data3)
                    {
                        foreach (T4 t4 in data4)
                        {
                            foreach (T5 t5 in data5)
                            {
                                foreach (T6 t6 in data6)
                                {
                                    foreach (T7 t7 in data7)
                                    {
                                        foreach (T8 t8 in data8)
                                        {
                                            foreach (T9 t9 in data9)
                                            {
                                                foreach (T10 t10 in data10)
                                                {
                                                    foreach (T11 t11 in data11)
                                                    {
                                                        AddRow(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
