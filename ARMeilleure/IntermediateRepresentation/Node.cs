using System;
using System.Collections.Generic;

namespace ARMeilleure.IntermediateRepresentation
{
    class Node : IIntrusiveListNode<Node>
    {
        public Node ListPrevious { get; set; }
        public Node ListNext { get; set; }

        public Operand Destination
        {
            get => _destinations.Count != 0 ? GetDestination(0) : null;
            set => SetDestination(value);
        }

        private readonly List<Operand> _destinations;
        private readonly List<Operand> _sources;

        public int DestinationsCount => _destinations.Count;
        public int SourcesCount      => _sources.Count;

        private void Resize(List<Operand> list, int size)
        {
            if (list.Count > size)
            {
                list.RemoveRange(size, list.Count - size);
            } 
            else
            {
                while (list.Count < size)
                {
                    list.Add(null);
                }
            }
        }

        public Node()
        {
            _destinations = new List<Operand>();
            _sources = new List<Operand>();
        }

        public Node(Operand destination, int sourcesCount) : this()
        {
            Destination = destination;

            Resize(_sources, sourcesCount);
        }

        private void Reset(int sourcesCount)
        {
            _sources.Clear();
            ListPrevious = null;
            ListNext = null;

            Resize(_sources, sourcesCount);
        }

        public Node With(Operand destination, int sourcesCount)
        {
            Reset(sourcesCount);
            Destination = destination;

            return this;
        }

        public Node With(Operand[] destinations, int sourcesCount)
        {
            Reset(sourcesCount);
            SetDestinations(destinations ?? throw new ArgumentNullException(nameof(destinations)));

            return this;
        }

        public Operand GetDestination(int index)
        {
            return _destinations[index];
        }

        public Operand GetSource(int index)
        {
            return _sources[index];
        }

        public void SetDestination(int index, Operand destination)
        {
            _destinations[index] = destination;
        }

        public void SetSource(int index, Operand source)
        {
            _sources[index] = source;
        }

        public void SetDestination(Operand destination)
        {
            if (destination == null)
            {
                _destinations.Clear();
            }
            else
            {
                Resize(_destinations, 1);

                _destinations[0] = destination;
            }
        }

        public void SetDestinations(Operand[] destinations)
        {
            Resize(_destinations, destinations.Length);

            for (int index = 0; index < destinations.Length; index++)
            {
                Operand newOp = destinations[index];

                _destinations[index] = newOp;
            }
        }

        public void SetSource(Operand source)
        {
            if (source == null)
            {
                _sources.Clear();
            }
            else
            {
                Resize(_sources, 1);

                _sources[0] = source;
            }
        }

        public void SetSources(Operand[] sources)
        {
            Resize(_sources, sources.Length);

            for (int index = 0; index < sources.Length; index++)
            {
                _sources[index] = sources[index];
            }
        }
    }
}