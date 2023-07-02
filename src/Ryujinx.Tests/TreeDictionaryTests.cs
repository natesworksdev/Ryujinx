using NUnit.Framework;
using Ryujinx.Common.Collections;
using System;
using System.Collections.Generic;

namespace Ryujinx.Tests.Collections
{
    class TreeDictionaryTests
    {
        [Test]
        public void EnsureAddIntegrity()
        {
            TreeDictionary<int, int> dictionary = new();

            Assert.AreEqual(0, dictionary.Count);

            dictionary.Add(2, 7);
            dictionary.Add(1, 4);
            dictionary.Add(10, 2);
            dictionary.Add(4, 1);
            dictionary.Add(3, 2);
            dictionary.Add(11, 2);
            dictionary.Add(5, 2);

            Assert.AreEqual(7, dictionary.Count);

            List<KeyValuePair<int, int>> list = dictionary.AsLevelOrderList();

            /*
             *  Tree Should Look as Follows After Rotations
             *
             *        2
             *    1        4
             *           3    10
             *              5    11
             *
             */

            Assert.AreEqual(list.Count, dictionary.Count);
            Assert.AreEqual(2, list[0].Key);
            Assert.AreEqual(1, list[1].Key);
            Assert.AreEqual(4, list[2].Key);
            Assert.AreEqual(3, list[3].Key);
            Assert.AreEqual(10, list[4].Key);
            Assert.AreEqual(5, list[5].Key);
            Assert.AreEqual(11, list[6].Key);
        }

        [Test]
        public void EnsureRemoveIntegrity()
        {
            TreeDictionary<int, int> dictionary = new();

            Assert.AreEqual(0, dictionary.Count);

            dictionary.Add(2, 7);
            dictionary.Add(1, 4);
            dictionary.Add(10, 2);
            dictionary.Add(4, 1);
            dictionary.Add(3, 2);
            dictionary.Add(11, 2);
            dictionary.Add(5, 2);
            dictionary.Add(7, 2);
            dictionary.Add(9, 2);
            dictionary.Add(8, 2);
            dictionary.Add(13, 2);
            dictionary.Add(24, 2);
            dictionary.Add(6, 2);
            Assert.AreEqual(13, dictionary.Count);

            List<KeyValuePair<int, int>> list = dictionary.AsLevelOrderList();

            /*
             *  Tree Should Look as Follows After Rotations
             *
             *              4
             *      2               10
             *  1      3       7         13
             *              5      9  11    24
             *                6  8
             */

            foreach (KeyValuePair<int, int> node in list)
            {
                Console.WriteLine($"{node.Key} -> {node.Value}");
            }
            Assert.AreEqual(list.Count, dictionary.Count);
            Assert.AreEqual(4, list[0].Key);
            Assert.AreEqual(2, list[1].Key);
            Assert.AreEqual(10, list[2].Key);
            Assert.AreEqual(1, list[3].Key);
            Assert.AreEqual(3, list[4].Key);
            Assert.AreEqual(7, list[5].Key);
            Assert.AreEqual(13, list[6].Key);
            Assert.AreEqual(5, list[7].Key);
            Assert.AreEqual(9, list[8].Key);
            Assert.AreEqual(11, list[9].Key);
            Assert.AreEqual(24, list[10].Key);
            Assert.AreEqual(6, list[11].Key);
            Assert.AreEqual(8, list[12].Key);

            list.Clear();

            dictionary.Remove(7);

            /*
             *  Tree Should Look as Follows After Removal
             *
             *              4
             *      2               10
             *  1      3       6         13
             *              5      9  11    24
             *                  8
             */

            list = dictionary.AsLevelOrderList();
            foreach (KeyValuePair<int, int> node in list)
            {
                Console.WriteLine($"{node.Key} -> {node.Value}");
            }
            Assert.AreEqual(4, list[0].Key);
            Assert.AreEqual(2, list[1].Key);
            Assert.AreEqual(10, list[2].Key);
            Assert.AreEqual(1, list[3].Key);
            Assert.AreEqual(3, list[4].Key);
            Assert.AreEqual(6, list[5].Key);
            Assert.AreEqual(13, list[6].Key);
            Assert.AreEqual(5, list[7].Key);
            Assert.AreEqual(9, list[8].Key);
            Assert.AreEqual(11, list[9].Key);
            Assert.AreEqual(24, list[10].Key);
            Assert.AreEqual(8, list[11].Key);

            list.Clear();

            dictionary.Remove(10);

            list = dictionary.AsLevelOrderList();
            /*
             *  Tree Should Look as Follows After Removal
             *
             *              4
             *      2               9
             *  1      3       6         13
             *              5      8  11    24
             *
             */
            foreach (KeyValuePair<int, int> node in list)
            {
                Console.WriteLine($"{node.Key} -> {node.Value}");
            }
            Assert.AreEqual(4, list[0].Key);
            Assert.AreEqual(2, list[1].Key);
            Assert.AreEqual(9, list[2].Key);
            Assert.AreEqual(1, list[3].Key);
            Assert.AreEqual(3, list[4].Key);
            Assert.AreEqual(6, list[5].Key);
            Assert.AreEqual(13, list[6].Key);
            Assert.AreEqual(5, list[7].Key);
            Assert.AreEqual(8, list[8].Key);
            Assert.AreEqual(11, list[9].Key);
            Assert.AreEqual(24, list[10].Key);
        }

        [Test]
        public void EnsureOverwriteIntegrity()
        {
            TreeDictionary<int, int> dictionary = new();

            Assert.AreEqual(0, dictionary.Count);

            dictionary.Add(2, 7);
            dictionary.Add(1, 4);
            dictionary.Add(10, 2);
            dictionary.Add(4, 1);
            dictionary.Add(3, 2);
            dictionary.Add(11, 2);
            dictionary.Add(5, 2);
            dictionary.Add(7, 2);
            dictionary.Add(9, 2);
            dictionary.Add(8, 2);
            dictionary.Add(13, 2);
            dictionary.Add(24, 2);
            dictionary.Add(6, 2);
            Assert.AreEqual(13, dictionary.Count);

            List<KeyValuePair<int, int>> list = dictionary.AsLevelOrderList();

            foreach (KeyValuePair<int, int> node in list)
            {
                Console.WriteLine($"{node.Key} -> {node.Value}");
            }

            /*
             *  Tree Should Look as Follows After Rotations
             *
             *              4
             *      2               10
             *  1      3       7         13
             *              5      9  11    24
             *                6  8
             */

            Assert.AreEqual(list.Count, dictionary.Count);
            Assert.AreEqual(4, list[0].Key);
            Assert.AreEqual(2, list[1].Key);
            Assert.AreEqual(10, list[2].Key);
            Assert.AreEqual(1, list[3].Key);
            Assert.AreEqual(3, list[4].Key);
            Assert.AreEqual(7, list[5].Key);
            Assert.AreEqual(13, list[6].Key);
            Assert.AreEqual(5, list[7].Key);
            Assert.AreEqual(9, list[8].Key);
            Assert.AreEqual(11, list[9].Key);
            Assert.AreEqual(24, list[10].Key);
            Assert.AreEqual(6, list[11].Key);
            Assert.AreEqual(8, list[12].Key);

            Assert.AreEqual(2, list[4].Value);

            dictionary.Add(3, 4);

            list = dictionary.AsLevelOrderList();

            Assert.AreEqual(4, list[4].Value);


            // Assure that none of the nodes locations have been modified.
            Assert.AreEqual(4, list[0].Key);
            Assert.AreEqual(2, list[1].Key);
            Assert.AreEqual(10, list[2].Key);
            Assert.AreEqual(1, list[3].Key);
            Assert.AreEqual(3, list[4].Key);
            Assert.AreEqual(7, list[5].Key);
            Assert.AreEqual(13, list[6].Key);
            Assert.AreEqual(5, list[7].Key);
            Assert.AreEqual(9, list[8].Key);
            Assert.AreEqual(11, list[9].Key);
            Assert.AreEqual(24, list[10].Key);
            Assert.AreEqual(6, list[11].Key);
            Assert.AreEqual(8, list[12].Key);
        }
    }
}
