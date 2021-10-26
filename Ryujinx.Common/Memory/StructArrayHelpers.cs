using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Common.Memory
{
    public struct Array1<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        public int Length => 1;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 1);
    }
    public struct Array2<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array1<T> _other;
#pragma warning restore CS0169
        public int Length => 2;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 2);
    }
    public struct Array3<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array2<T> _other;
#pragma warning restore CS0169
        public int Length => 3;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 3);
    }
    public struct Array4<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array3<T> _other;
#pragma warning restore CS0169
        public int Length => 4;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 4);
    }
    public struct Array5<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array4<T> _other;
#pragma warning restore CS0169
        public int Length => 5;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 5);
    }
    public struct Array6<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array5<T> _other;
#pragma warning restore CS0169
        public int Length => 6;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 6);
    }
    public struct Array7<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array6<T> _other;
#pragma warning restore CS0169
        public int Length => 7;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 7);
    }
    public struct Array8<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array7<T> _other;
#pragma warning restore CS0169
        public int Length => 8;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 8);
    }
    public struct Array9<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array8<T> _other;
#pragma warning restore CS0169
        public int Length => 9;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 9);
    }
    public struct Array10<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array9<T> _other;
#pragma warning restore CS0169
        public int Length => 10;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 10);
    }
    public struct Array11<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array10<T> _other;
#pragma warning restore CS0169
        public int Length => 11;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 11);
    }
    public struct Array12<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array11<T> _other;
#pragma warning restore CS0169
        public int Length => 12;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 12);
    }
    public struct Array13<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array12<T> _other;
#pragma warning restore CS0169
        public int Length => 13;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 13);
    }
    public struct Array14<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array13<T> _other;
#pragma warning restore CS0169
        public int Length => 14;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 14);
    }
    public struct Array15<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array14<T> _other;
#pragma warning restore CS0169
        public int Length => 15;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 15);
    }
    public struct Array16<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array15<T> _other;
#pragma warning restore CS0169
        public int Length => 16;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 16);
    }
    public struct Array17<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array16<T> _other;
#pragma warning restore CS0169
        public int Length => 17;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 17);
    }
    public struct Array18<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array17<T> _other;
#pragma warning restore CS0169
        public int Length => 18;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 18);
    }
    public struct Array19<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array18<T> _other;
#pragma warning restore CS0169
        public int Length => 19;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 19);
    }
    public struct Array20<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array19<T> _other;
#pragma warning restore CS0169
        public int Length => 20;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 20);
    }
    public struct Array21<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array20<T> _other;
#pragma warning restore CS0169
        public int Length => 21;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 21);
    }
    public struct Array22<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array21<T> _other;
#pragma warning restore CS0169
        public int Length => 22;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 22);
    }
    public struct Array23<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array22<T> _other;
#pragma warning restore CS0169
        public int Length => 23;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 23);
    }
    public struct Array24<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array23<T> _other;
#pragma warning restore CS0169
        public int Length => 24;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 24);
    }
    public struct Array25<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array24<T> _other;
#pragma warning restore CS0169
        public int Length => 25;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 25);
    }
    public struct Array26<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array25<T> _other;
#pragma warning restore CS0169
        public int Length => 26;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 26);
    }
    public struct Array27<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array26<T> _other;
#pragma warning restore CS0169
        public int Length => 27;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 27);
    }
    public struct Array28<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array27<T> _other;
#pragma warning restore CS0169
        public int Length => 28;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 28);
    }
    public struct Array29<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array28<T> _other;
#pragma warning restore CS0169
        public int Length => 29;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 29);
    }
    public struct Array30<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array29<T> _other;
#pragma warning restore CS0169
        public int Length => 30;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 30);
    }
    public struct Array31<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array30<T> _other;
#pragma warning restore CS0169
        public int Length => 31;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 31);
    }
    public struct Array32<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array31<T> _other;
#pragma warning restore CS0169
        public int Length => 32;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 32);
    }
    public struct Array33<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array32<T> _other;
#pragma warning restore CS0169
        public int Length => 33;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 33);
    }
    public struct Array34<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array33<T> _other;
#pragma warning restore CS0169
        public int Length => 34;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 34);
    }
    public struct Array35<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array34<T> _other;
#pragma warning restore CS0169
        public int Length => 35;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 35);
    }
    public struct Array36<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array35<T> _other;
#pragma warning restore CS0169
        public int Length => 36;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 36);
    }
    public struct Array37<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array36<T> _other;
#pragma warning restore CS0169
        public int Length => 37;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 37);
    }
    public struct Array38<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array37<T> _other;
#pragma warning restore CS0169
        public int Length => 38;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 38);
    }
    public struct Array39<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array38<T> _other;
#pragma warning restore CS0169
        public int Length => 39;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 39);
    }
    public struct Array40<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array39<T> _other;
#pragma warning restore CS0169
        public int Length => 40;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 40);
    }
    public struct Array41<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array40<T> _other;
#pragma warning restore CS0169
        public int Length => 41;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 41);
    }
    public struct Array42<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array41<T> _other;
#pragma warning restore CS0169
        public int Length => 42;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 42);
    }
    public struct Array43<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array42<T> _other;
#pragma warning restore CS0169
        public int Length => 43;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 43);
    }
    public struct Array44<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array43<T> _other;
#pragma warning restore CS0169
        public int Length => 44;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 44);
    }
    public struct Array45<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array44<T> _other;
#pragma warning restore CS0169
        public int Length => 45;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 45);
    }
    public struct Array46<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array45<T> _other;
#pragma warning restore CS0169
        public int Length => 46;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 46);
    }
    public struct Array47<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array46<T> _other;
#pragma warning restore CS0169
        public int Length => 47;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 47);
    }
    public struct Array48<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array47<T> _other;
#pragma warning restore CS0169
        public int Length => 48;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 48);
    }
    public struct Array49<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array48<T> _other;
#pragma warning restore CS0169
        public int Length => 49;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 49);
    }
    public struct Array50<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array49<T> _other;
#pragma warning restore CS0169
        public int Length => 50;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 50);
    }
    public struct Array51<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array50<T> _other;
#pragma warning restore CS0169
        public int Length => 51;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 51);
    }
    public struct Array52<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array51<T> _other;
#pragma warning restore CS0169
        public int Length => 52;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 52);
    }
    public struct Array53<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array52<T> _other;
#pragma warning restore CS0169
        public int Length => 53;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 53);
    }
    public struct Array54<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array53<T> _other;
#pragma warning restore CS0169
        public int Length => 54;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 54);
    }
    public struct Array55<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array54<T> _other;
#pragma warning restore CS0169
        public int Length => 55;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 55);
    }
    public struct Array56<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array55<T> _other;
#pragma warning restore CS0169
        public int Length => 56;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 56);
    }
    public struct Array57<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array56<T> _other;
#pragma warning restore CS0169
        public int Length => 57;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 57);
    }
    public struct Array58<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array57<T> _other;
#pragma warning restore CS0169
        public int Length => 58;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 58);
    }
    public struct Array59<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array58<T> _other;
#pragma warning restore CS0169
        public int Length => 59;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 59);
    }
    public struct Array60<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array59<T> _other;
#pragma warning restore CS0169
        public int Length => 60;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 60);
    }
    public struct Array61<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array60<T> _other;
#pragma warning restore CS0169
        public int Length => 61;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 61);
    }
    public struct Array62<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array61<T> _other;
#pragma warning restore CS0169
        public int Length => 62;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 62);
    }
    public struct Array63<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array62<T> _other;
#pragma warning restore CS0169
        public int Length => 63;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 63);
    }
    public struct Array64<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array63<T> _other;
#pragma warning restore CS0169
        public int Length => 64;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 64);
    }
    public struct Array65<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array64<T> _other;
#pragma warning restore CS0169
        public int Length => 65;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 65);
    }
    public struct Array66<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array65<T> _other;
#pragma warning restore CS0169
        public int Length => 66;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 66);
    }
    public struct Array67<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array66<T> _other;
#pragma warning restore CS0169
        public int Length => 67;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 67);
    }
    public struct Array68<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array67<T> _other;
#pragma warning restore CS0169
        public int Length => 68;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 68);
    }
    public struct Array69<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array68<T> _other;
#pragma warning restore CS0169
        public int Length => 69;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 69);
    }
    public struct Array70<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array69<T> _other;
#pragma warning restore CS0169
        public int Length => 70;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 70);
    }
    public struct Array71<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array70<T> _other;
#pragma warning restore CS0169
        public int Length => 71;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 71);
    }
    public struct Array72<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array71<T> _other;
#pragma warning restore CS0169
        public int Length => 72;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 72);
    }
    public struct Array73<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array72<T> _other;
#pragma warning restore CS0169
        public int Length => 73;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 73);
    }
    public struct Array74<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array73<T> _other;
#pragma warning restore CS0169
        public int Length => 74;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 74);
    }
    public struct Array75<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array74<T> _other;
#pragma warning restore CS0169
        public int Length => 75;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 75);
    }
    public struct Array76<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array75<T> _other;
#pragma warning restore CS0169
        public int Length => 76;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 76);
    }
    public struct Array77<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array76<T> _other;
#pragma warning restore CS0169
        public int Length => 77;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 77);
    }
    public struct Array78<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array77<T> _other;
#pragma warning restore CS0169
        public int Length => 78;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 78);
    }
    public struct Array79<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array78<T> _other;
#pragma warning restore CS0169
        public int Length => 79;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 79);
    }
    public struct Array80<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array79<T> _other;
#pragma warning restore CS0169
        public int Length => 80;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 80);
    }
    public struct Array81<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array80<T> _other;
#pragma warning restore CS0169
        public int Length => 81;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 81);
    }
    public struct Array82<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array81<T> _other;
#pragma warning restore CS0169
        public int Length => 82;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 82);
    }
    public struct Array83<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array82<T> _other;
#pragma warning restore CS0169
        public int Length => 83;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 83);
    }
    public struct Array84<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array83<T> _other;
#pragma warning restore CS0169
        public int Length => 84;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 84);
    }
    public struct Array85<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array84<T> _other;
#pragma warning restore CS0169
        public int Length => 85;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 85);
    }
    public struct Array86<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array85<T> _other;
#pragma warning restore CS0169
        public int Length => 86;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 86);
    }
    public struct Array87<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array86<T> _other;
#pragma warning restore CS0169
        public int Length => 87;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 87);
    }
    public struct Array88<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array87<T> _other;
#pragma warning restore CS0169
        public int Length => 88;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 88);
    }
    public struct Array89<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array88<T> _other;
#pragma warning restore CS0169
        public int Length => 89;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 89);
    }
    public struct Array90<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array89<T> _other;
#pragma warning restore CS0169
        public int Length => 90;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 90);
    }
    public struct Array91<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array90<T> _other;
#pragma warning restore CS0169
        public int Length => 91;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 91);
    }
    public struct Array92<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array91<T> _other;
#pragma warning restore CS0169
        public int Length => 92;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 92);
    }
    public struct Array93<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array92<T> _other;
#pragma warning restore CS0169
        public int Length => 93;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 93);
    }
    public struct Array94<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array93<T> _other;
#pragma warning restore CS0169
        public int Length => 94;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 94);
    }
    public struct Array95<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array94<T> _other;
#pragma warning restore CS0169
        public int Length => 95;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 95);
    }
    public struct Array96<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array95<T> _other;
#pragma warning restore CS0169
        public int Length => 96;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 96);
    }
    public struct Array97<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array96<T> _other;
#pragma warning restore CS0169
        public int Length => 97;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 97);
    }
    public struct Array98<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array97<T> _other;
#pragma warning restore CS0169
        public int Length => 98;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 98);
    }
    public struct Array99<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array98<T> _other;
#pragma warning restore CS0169
        public int Length => 99;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 99);
    }
    public struct Array100<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array99<T> _other;
#pragma warning restore CS0169
        public int Length => 100;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 100);
    }
    public struct Array101<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array100<T> _other;
#pragma warning restore CS0169
        public int Length => 101;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 101);
    }
    public struct Array102<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array101<T> _other;
#pragma warning restore CS0169
        public int Length => 102;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 102);
    }
    public struct Array103<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array102<T> _other;
#pragma warning restore CS0169
        public int Length => 103;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 103);
    }
    public struct Array104<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array103<T> _other;
#pragma warning restore CS0169
        public int Length => 104;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 104);
    }
    public struct Array105<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array104<T> _other;
#pragma warning restore CS0169
        public int Length => 105;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 105);
    }
    public struct Array106<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array105<T> _other;
#pragma warning restore CS0169
        public int Length => 106;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 106);
    }
    public struct Array107<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array106<T> _other;
#pragma warning restore CS0169
        public int Length => 107;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 107);
    }
    public struct Array108<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array107<T> _other;
#pragma warning restore CS0169
        public int Length => 108;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 108);
    }
    public struct Array109<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array108<T> _other;
#pragma warning restore CS0169
        public int Length => 109;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 109);
    }
    public struct Array110<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array109<T> _other;
#pragma warning restore CS0169
        public int Length => 110;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 110);
    }
    public struct Array111<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array110<T> _other;
#pragma warning restore CS0169
        public int Length => 111;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 111);
    }
    public struct Array112<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array111<T> _other;
#pragma warning restore CS0169
        public int Length => 112;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 112);
    }
    public struct Array113<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array112<T> _other;
#pragma warning restore CS0169
        public int Length => 113;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 113);
    }
    public struct Array114<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array113<T> _other;
#pragma warning restore CS0169
        public int Length => 114;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 114);
    }
    public struct Array115<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array114<T> _other;
#pragma warning restore CS0169
        public int Length => 115;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 115);
    }
    public struct Array116<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array115<T> _other;
#pragma warning restore CS0169
        public int Length => 116;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 116);
    }
    public struct Array117<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array116<T> _other;
#pragma warning restore CS0169
        public int Length => 117;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 117);
    }
    public struct Array118<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array117<T> _other;
#pragma warning restore CS0169
        public int Length => 118;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 118);
    }
    public struct Array119<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array118<T> _other;
#pragma warning restore CS0169
        public int Length => 119;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 119);
    }
    public struct Array120<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array119<T> _other;
#pragma warning restore CS0169
        public int Length => 120;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 120);
    }
    public struct Array121<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array120<T> _other;
#pragma warning restore CS0169
        public int Length => 121;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 121);
    }
    public struct Array122<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array121<T> _other;
#pragma warning restore CS0169
        public int Length => 122;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 122);
    }
    public struct Array123<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array122<T> _other;
#pragma warning restore CS0169
        public int Length => 123;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 123);
    }
    public struct Array124<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array123<T> _other;
#pragma warning restore CS0169
        public int Length => 124;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 124);
    }
    public struct Array125<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array124<T> _other;
#pragma warning restore CS0169
        public int Length => 125;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 125);
    }
    public struct Array126<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array125<T> _other;
#pragma warning restore CS0169
        public int Length => 126;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 126);
    }
    public struct Array127<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array126<T> _other;
#pragma warning restore CS0169
        public int Length => 127;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 127);
    }
    public struct Array128<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array127<T> _other;
#pragma warning restore CS0169
        public int Length => 128;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 128);
    }
    public struct Array129<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array128<T> _other;
#pragma warning restore CS0169
        public int Length => 129;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 129);
    }
    public struct Array130<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array129<T> _other;
#pragma warning restore CS0169
        public int Length => 130;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 130);
    }
    public struct Array131<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array130<T> _other;
#pragma warning restore CS0169
        public int Length => 131;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 131);
    }
    public struct Array132<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array131<T> _other;
#pragma warning restore CS0169
        public int Length => 132;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 132);
    }
    public struct Array133<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array132<T> _other;
#pragma warning restore CS0169
        public int Length => 133;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 133);
    }
    public struct Array134<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array133<T> _other;
#pragma warning restore CS0169
        public int Length => 134;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 134);
    }
    public struct Array135<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array134<T> _other;
#pragma warning restore CS0169
        public int Length => 135;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 135);
    }
    public struct Array136<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array135<T> _other;
#pragma warning restore CS0169
        public int Length => 136;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 136);
    }
    public struct Array137<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array136<T> _other;
#pragma warning restore CS0169
        public int Length => 137;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 137);
    }
    public struct Array138<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array137<T> _other;
#pragma warning restore CS0169
        public int Length => 138;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 138);
    }
    public struct Array139<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array138<T> _other;
#pragma warning restore CS0169
        public int Length => 139;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 139);
    }
    public struct Array140<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array139<T> _other;
#pragma warning restore CS0169
        public int Length => 140;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 140);
    }
    public struct Array141<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array140<T> _other;
#pragma warning restore CS0169
        public int Length => 141;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 141);
    }
    public struct Array142<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array141<T> _other;
#pragma warning restore CS0169
        public int Length => 142;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 142);
    }
    public struct Array143<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array142<T> _other;
#pragma warning restore CS0169
        public int Length => 143;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 143);
    }
    public struct Array144<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array143<T> _other;
#pragma warning restore CS0169
        public int Length => 144;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 144);
    }
    public struct Array145<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array144<T> _other;
#pragma warning restore CS0169
        public int Length => 145;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 145);
    }
    public struct Array146<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array145<T> _other;
#pragma warning restore CS0169
        public int Length => 146;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 146);
    }
    public struct Array147<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array146<T> _other;
#pragma warning restore CS0169
        public int Length => 147;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 147);
    }
    public struct Array148<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array147<T> _other;
#pragma warning restore CS0169
        public int Length => 148;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 148);
    }
    public struct Array149<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array148<T> _other;
#pragma warning restore CS0169
        public int Length => 149;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 149);
    }
    public struct Array150<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array149<T> _other;
#pragma warning restore CS0169
        public int Length => 150;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 150);
    }
    public struct Array151<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array150<T> _other;
#pragma warning restore CS0169
        public int Length => 151;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 151);
    }
    public struct Array152<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array151<T> _other;
#pragma warning restore CS0169
        public int Length => 152;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 152);
    }
    public struct Array153<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array152<T> _other;
#pragma warning restore CS0169
        public int Length => 153;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 153);
    }
    public struct Array154<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array153<T> _other;
#pragma warning restore CS0169
        public int Length => 154;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 154);
    }
    public struct Array155<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array154<T> _other;
#pragma warning restore CS0169
        public int Length => 155;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 155);
    }
    public struct Array156<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array155<T> _other;
#pragma warning restore CS0169
        public int Length => 156;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 156);
    }
    public struct Array157<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array156<T> _other;
#pragma warning restore CS0169
        public int Length => 157;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 157);
    }
    public struct Array158<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array157<T> _other;
#pragma warning restore CS0169
        public int Length => 158;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 158);
    }
    public struct Array159<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array158<T> _other;
#pragma warning restore CS0169
        public int Length => 159;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 159);
    }
    public struct Array160<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array159<T> _other;
#pragma warning restore CS0169
        public int Length => 160;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 160);
    }
    public struct Array161<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array160<T> _other;
#pragma warning restore CS0169
        public int Length => 161;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 161);
    }
    public struct Array162<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array161<T> _other;
#pragma warning restore CS0169
        public int Length => 162;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 162);
    }
    public struct Array163<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array162<T> _other;
#pragma warning restore CS0169
        public int Length => 163;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 163);
    }
    public struct Array164<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array163<T> _other;
#pragma warning restore CS0169
        public int Length => 164;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 164);
    }
    public struct Array165<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array164<T> _other;
#pragma warning restore CS0169
        public int Length => 165;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 165);
    }
    public struct Array166<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array165<T> _other;
#pragma warning restore CS0169
        public int Length => 166;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 166);
    }
    public struct Array167<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array166<T> _other;
#pragma warning restore CS0169
        public int Length => 167;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 167);
    }
    public struct Array168<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array167<T> _other;
#pragma warning restore CS0169
        public int Length => 168;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 168);
    }
    public struct Array169<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array168<T> _other;
#pragma warning restore CS0169
        public int Length => 169;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 169);
    }
    public struct Array170<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array169<T> _other;
#pragma warning restore CS0169
        public int Length => 170;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 170);
    }
    public struct Array171<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array170<T> _other;
#pragma warning restore CS0169
        public int Length => 171;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 171);
    }
    public struct Array172<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array171<T> _other;
#pragma warning restore CS0169
        public int Length => 172;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 172);
    }
    public struct Array173<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array172<T> _other;
#pragma warning restore CS0169
        public int Length => 173;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 173);
    }
    public struct Array174<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array173<T> _other;
#pragma warning restore CS0169
        public int Length => 174;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 174);
    }
    public struct Array175<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array174<T> _other;
#pragma warning restore CS0169
        public int Length => 175;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 175);
    }
    public struct Array176<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array175<T> _other;
#pragma warning restore CS0169
        public int Length => 176;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 176);
    }
    public struct Array177<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array176<T> _other;
#pragma warning restore CS0169
        public int Length => 177;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 177);
    }
    public struct Array178<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array177<T> _other;
#pragma warning restore CS0169
        public int Length => 178;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 178);
    }
    public struct Array179<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array178<T> _other;
#pragma warning restore CS0169
        public int Length => 179;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 179);
    }
    public struct Array180<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array179<T> _other;
#pragma warning restore CS0169
        public int Length => 180;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 180);
    }
    public struct Array181<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array180<T> _other;
#pragma warning restore CS0169
        public int Length => 181;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 181);
    }
    public struct Array182<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array181<T> _other;
#pragma warning restore CS0169
        public int Length => 182;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 182);
    }
    public struct Array183<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array182<T> _other;
#pragma warning restore CS0169
        public int Length => 183;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 183);
    }
    public struct Array184<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array183<T> _other;
#pragma warning restore CS0169
        public int Length => 184;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 184);
    }
    public struct Array185<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array184<T> _other;
#pragma warning restore CS0169
        public int Length => 185;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 185);
    }
    public struct Array186<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array185<T> _other;
#pragma warning restore CS0169
        public int Length => 186;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 186);
    }
    public struct Array187<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array186<T> _other;
#pragma warning restore CS0169
        public int Length => 187;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 187);
    }
    public struct Array188<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array187<T> _other;
#pragma warning restore CS0169
        public int Length => 188;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 188);
    }
    public struct Array189<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array188<T> _other;
#pragma warning restore CS0169
        public int Length => 189;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 189);
    }
    public struct Array190<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array189<T> _other;
#pragma warning restore CS0169
        public int Length => 190;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 190);
    }
    public struct Array191<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array190<T> _other;
#pragma warning restore CS0169
        public int Length => 191;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 191);
    }
    public struct Array192<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array191<T> _other;
#pragma warning restore CS0169
        public int Length => 192;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 192);
    }
    public struct Array193<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array192<T> _other;
#pragma warning restore CS0169
        public int Length => 193;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 193);
    }
    public struct Array194<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array193<T> _other;
#pragma warning restore CS0169
        public int Length => 194;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 194);
    }
    public struct Array195<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array194<T> _other;
#pragma warning restore CS0169
        public int Length => 195;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 195);
    }
    public struct Array196<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array195<T> _other;
#pragma warning restore CS0169
        public int Length => 196;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 196);
    }
    public struct Array197<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array196<T> _other;
#pragma warning restore CS0169
        public int Length => 197;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 197);
    }
    public struct Array198<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array197<T> _other;
#pragma warning restore CS0169
        public int Length => 198;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 198);
    }
    public struct Array199<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array198<T> _other;
#pragma warning restore CS0169
        public int Length => 199;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 199);
    }
    public struct Array200<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array199<T> _other;
#pragma warning restore CS0169
        public int Length => 200;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 200);
    }
    public struct Array201<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array200<T> _other;
#pragma warning restore CS0169
        public int Length => 201;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 201);
    }
    public struct Array202<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array201<T> _other;
#pragma warning restore CS0169
        public int Length => 202;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 202);
    }
    public struct Array203<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array202<T> _other;
#pragma warning restore CS0169
        public int Length => 203;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 203);
    }
    public struct Array204<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array203<T> _other;
#pragma warning restore CS0169
        public int Length => 204;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 204);
    }
    public struct Array205<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array204<T> _other;
#pragma warning restore CS0169
        public int Length => 205;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 205);
    }
    public struct Array206<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array205<T> _other;
#pragma warning restore CS0169
        public int Length => 206;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 206);
    }
    public struct Array207<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array206<T> _other;
#pragma warning restore CS0169
        public int Length => 207;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 207);
    }
    public struct Array208<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array207<T> _other;
#pragma warning restore CS0169
        public int Length => 208;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 208);
    }
    public struct Array209<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array208<T> _other;
#pragma warning restore CS0169
        public int Length => 209;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 209);
    }
    public struct Array210<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array209<T> _other;
#pragma warning restore CS0169
        public int Length => 210;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 210);
    }
    public struct Array211<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array210<T> _other;
#pragma warning restore CS0169
        public int Length => 211;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 211);
    }
    public struct Array212<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array211<T> _other;
#pragma warning restore CS0169
        public int Length => 212;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 212);
    }
    public struct Array213<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array212<T> _other;
#pragma warning restore CS0169
        public int Length => 213;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 213);
    }
    public struct Array214<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array213<T> _other;
#pragma warning restore CS0169
        public int Length => 214;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 214);
    }
    public struct Array215<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array214<T> _other;
#pragma warning restore CS0169
        public int Length => 215;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 215);
    }
    public struct Array216<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array215<T> _other;
#pragma warning restore CS0169
        public int Length => 216;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 216);
    }
    public struct Array217<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array216<T> _other;
#pragma warning restore CS0169
        public int Length => 217;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 217);
    }
    public struct Array218<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array217<T> _other;
#pragma warning restore CS0169
        public int Length => 218;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 218);
    }
    public struct Array219<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array218<T> _other;
#pragma warning restore CS0169
        public int Length => 219;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 219);
    }
    public struct Array220<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array219<T> _other;
#pragma warning restore CS0169
        public int Length => 220;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 220);
    }
    public struct Array221<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array220<T> _other;
#pragma warning restore CS0169
        public int Length => 221;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 221);
    }
    public struct Array222<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array221<T> _other;
#pragma warning restore CS0169
        public int Length => 222;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 222);
    }
    public struct Array223<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array222<T> _other;
#pragma warning restore CS0169
        public int Length => 223;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 223);
    }
    public struct Array224<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array223<T> _other;
#pragma warning restore CS0169
        public int Length => 224;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 224);
    }
    public struct Array225<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array224<T> _other;
#pragma warning restore CS0169
        public int Length => 225;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 225);
    }
    public struct Array226<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array225<T> _other;
#pragma warning restore CS0169
        public int Length => 226;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 226);
    }
    public struct Array227<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array226<T> _other;
#pragma warning restore CS0169
        public int Length => 227;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 227);
    }
    public struct Array228<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array227<T> _other;
#pragma warning restore CS0169
        public int Length => 228;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 228);
    }
    public struct Array229<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array228<T> _other;
#pragma warning restore CS0169
        public int Length => 229;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 229);
    }
    public struct Array230<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array229<T> _other;
#pragma warning restore CS0169
        public int Length => 230;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 230);
    }
    public struct Array231<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array230<T> _other;
#pragma warning restore CS0169
        public int Length => 231;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 231);
    }
    public struct Array232<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array231<T> _other;
#pragma warning restore CS0169
        public int Length => 232;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 232);
    }
    public struct Array233<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array232<T> _other;
#pragma warning restore CS0169
        public int Length => 233;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 233);
    }
    public struct Array234<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array233<T> _other;
#pragma warning restore CS0169
        public int Length => 234;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 234);
    }
    public struct Array235<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array234<T> _other;
#pragma warning restore CS0169
        public int Length => 235;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 235);
    }
    public struct Array236<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array235<T> _other;
#pragma warning restore CS0169
        public int Length => 236;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 236);
    }
    public struct Array237<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array236<T> _other;
#pragma warning restore CS0169
        public int Length => 237;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 237);
    }
    public struct Array238<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array237<T> _other;
#pragma warning restore CS0169
        public int Length => 238;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 238);
    }
    public struct Array239<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array238<T> _other;
#pragma warning restore CS0169
        public int Length => 239;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 239);
    }
    public struct Array240<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array239<T> _other;
#pragma warning restore CS0169
        public int Length => 240;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 240);
    }
    public struct Array241<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array240<T> _other;
#pragma warning restore CS0169
        public int Length => 241;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 241);
    }
    public struct Array242<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array241<T> _other;
#pragma warning restore CS0169
        public int Length => 242;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 242);
    }
    public struct Array243<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array242<T> _other;
#pragma warning restore CS0169
        public int Length => 243;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 243);
    }
    public struct Array244<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array243<T> _other;
#pragma warning restore CS0169
        public int Length => 244;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 244);
    }
    public struct Array245<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array244<T> _other;
#pragma warning restore CS0169
        public int Length => 245;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 245);
    }
    public struct Array246<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array245<T> _other;
#pragma warning restore CS0169
        public int Length => 246;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 246);
    }
    public struct Array247<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array246<T> _other;
#pragma warning restore CS0169
        public int Length => 247;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 247);
    }
    public struct Array248<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array247<T> _other;
#pragma warning restore CS0169
        public int Length => 248;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 248);
    }
    public struct Array249<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array248<T> _other;
#pragma warning restore CS0169
        public int Length => 249;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 249);
    }
    public struct Array250<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array249<T> _other;
#pragma warning restore CS0169
        public int Length => 250;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 250);
    }
    public struct Array251<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array250<T> _other;
#pragma warning restore CS0169
        public int Length => 251;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 251);
    }
    public struct Array252<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array251<T> _other;
#pragma warning restore CS0169
        public int Length => 252;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 252);
    }
    public struct Array253<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array252<T> _other;
#pragma warning restore CS0169
        public int Length => 253;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 253);
    }
    public struct Array254<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array253<T> _other;
#pragma warning restore CS0169
        public int Length => 254;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 254);
    }
    public struct Array255<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array254<T> _other;
#pragma warning restore CS0169
        public int Length => 255;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 255);
    }
    public struct Array256<T> : IArray<T> where T : unmanaged
    {
#pragma warning disable CS0169
        T _e0;
        Array255<T> _other;
#pragma warning restore CS0169
        public int Length => 256;
        public ref T this[int index] => ref ToSpan()[index];
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref _e0, 256);
    }
}
