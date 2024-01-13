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

            Assert.That(dictionary.Count, Is.EqualTo(0));

            dictionary.Add(2, 7);
            dictionary.Add(1, 4);
            dictionary.Add(10, 2);
            dictionary.Add(4, 1);
            dictionary.Add(3, 2);
            dictionary.Add(11, 2);
            dictionary.Add(5, 2);

            Assert.That(dictionary.Count, Is.EqualTo(7));

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

            Assert.That(list.Count, Is.EqualTo(dictionary.Count));
            Assert.That(list[0].Key, Is.EqualTo(2));
            Assert.That(list[1].Key, Is.EqualTo(1));
            Assert.That(list[2].Key, Is.EqualTo(4));
            Assert.That(list[3].Key, Is.EqualTo(3));
            Assert.That(list[4].Key, Is.EqualTo(10));
            Assert.That(list[5].Key, Is.EqualTo(5));
            Assert.That(list[6].Key, Is.EqualTo(11));
        }

        [Test]
        public void EnsureRemoveIntegrity()
        {
            TreeDictionary<int, int> dictionary = new();

            Assert.That(dictionary.Count, Is.EqualTo(0));

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
            Assert.That(dictionary.Count, Is.EqualTo(13));

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
            Assert.That(list.Count, Is.EqualTo(dictionary.Count));
            Assert.That(list[0].Key, Is.EqualTo(4));
            Assert.That(list[1].Key, Is.EqualTo(2));
            Assert.That(list[2].Key, Is.EqualTo(10));
            Assert.That(list[3].Key, Is.EqualTo(1));
            Assert.That(list[4].Key, Is.EqualTo(3));
            Assert.That(list[5].Key, Is.EqualTo(7));
            Assert.That(list[6].Key, Is.EqualTo(13));
            Assert.That(list[7].Key, Is.EqualTo(5));
            Assert.That(list[8].Key, Is.EqualTo(9));
            Assert.That(list[9].Key, Is.EqualTo(11));
            Assert.That(list[10].Key, Is.EqualTo(24));
            Assert.That(list[11].Key, Is.EqualTo(6));
            Assert.That(list[12].Key, Is.EqualTo(8));

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
            Assert.That(list[0].Key, Is.EqualTo(4));
            Assert.That(list[1].Key, Is.EqualTo(2));
            Assert.That(list[2].Key, Is.EqualTo(10));
            Assert.That(list[3].Key, Is.EqualTo(1));
            Assert.That(list[4].Key, Is.EqualTo(3));
            Assert.That(list[5].Key, Is.EqualTo(6));
            Assert.That(list[6].Key, Is.EqualTo(13));
            Assert.That(list[7].Key, Is.EqualTo(5));
            Assert.That(list[8].Key, Is.EqualTo(9));
            Assert.That(list[9].Key, Is.EqualTo(11));
            Assert.That(list[10].Key, Is.EqualTo(24));
            Assert.That(list[11].Key, Is.EqualTo(8));

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
            Assert.That(list[0].Key, Is.EqualTo(4));
            Assert.That(list[1].Key, Is.EqualTo(2));
            Assert.That(list[2].Key, Is.EqualTo(9));
            Assert.That(list[3].Key, Is.EqualTo(1));
            Assert.That(list[4].Key, Is.EqualTo(3));
            Assert.That(list[5].Key, Is.EqualTo(6));
            Assert.That(list[6].Key, Is.EqualTo(13));
            Assert.That(list[7].Key, Is.EqualTo(5));
            Assert.That(list[8].Key, Is.EqualTo(8));
            Assert.That(list[9].Key, Is.EqualTo(11));
            Assert.That(list[10].Key, Is.EqualTo(24));
        }

        [Test]
        public void EnsureOverwriteIntegrity()
        {
            TreeDictionary<int, int> dictionary = new();

            Assert.That(dictionary.Count, Is.EqualTo(0));

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
            Assert.That(dictionary.Count, Is.EqualTo(13));

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

            Assert.That(list.Count, Is.EqualTo(dictionary.Count));
            Assert.That(list[0].Key, Is.EqualTo(4));
            Assert.That(list[1].Key, Is.EqualTo(2));
            Assert.That(list[2].Key, Is.EqualTo(10));
            Assert.That(list[3].Key, Is.EqualTo(1));
            Assert.That(list[4].Key, Is.EqualTo(3));
            Assert.That(list[5].Key, Is.EqualTo(7));
            Assert.That(list[6].Key, Is.EqualTo(13));
            Assert.That(list[7].Key, Is.EqualTo(5));
            Assert.That(list[8].Key, Is.EqualTo(9));
            Assert.That(list[9].Key, Is.EqualTo(11));
            Assert.That(list[10].Key, Is.EqualTo(24));
            Assert.That(list[11].Key, Is.EqualTo(6));
            Assert.That(list[12].Key, Is.EqualTo(8));

            Assert.That(list[4].Value, Is.EqualTo(2));

            dictionary.Add(3, 4);

            list = dictionary.AsLevelOrderList();

            Assert.That(list[4].Value, Is.EqualTo(4));


            // Assure that none of the nodes locations have been modified.
            Assert.That(list[0].Key, Is.EqualTo(4));
            Assert.That(list[1].Key, Is.EqualTo(2));
            Assert.That(list[2].Key, Is.EqualTo(10));
            Assert.That(list[3].Key, Is.EqualTo(1));
            Assert.That(list[4].Key, Is.EqualTo(3));
            Assert.That(list[5].Key, Is.EqualTo(7));
            Assert.That(list[6].Key, Is.EqualTo(13));
            Assert.That(list[7].Key, Is.EqualTo(5));
            Assert.That(list[8].Key, Is.EqualTo(9));
            Assert.That(list[9].Key, Is.EqualTo(11));
            Assert.That(list[10].Key, Is.EqualTo(24));
            Assert.That(list[11].Key, Is.EqualTo(6));
            Assert.That(list[12].Key, Is.EqualTo(8));
        }
    }
}
