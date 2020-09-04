using System;
using System.Collections.Generic;

namespace Analyzer
{
    // Makes a lot of junk, is faster tho
    // backburner TM

    public class LinkedQueue<T>
    {
        internal class Node
        {
            public T data;
            internal Node next = null;
            internal Node previous;

            public Node(T data)
            {
                this.data = data;
                next = null;
                previous = null;
            }

        }

        internal Node head; // the head of the queue is actually the 'oldest' member
        internal Node tail; // the tail of the queue is the 'newest' member
        public int MaxValues { get; set; } = int.MaxValue;
        private int count = 0;
        public int Count => count;

        private Tuple<Node, int> Current { get; set; }

        public LinkedQueue()
        {
            head = null;
            tail = head;
        }
        public LinkedQueue(int maxValue)
        {
            tail = null;
            head = tail;
            this.MaxValues = maxValue;
        }

        public LinkedQueue(IEnumerable<T> collection, int maxValue = int.MaxValue)
        {
            tail = null;
            head = tail;
            this.MaxValues = maxValue;
            foreach (T item in collection)
            {
                Enqueue(item);
            }
        }

        public LinkedQueue<T> Enqueue(T data)
        {
            Node toAddNode = new Node(data);

            toAddNode.next = tail;
            toAddNode.previous = null;

            if (tail != null)
                tail.previous = toAddNode;
            else
                head = toAddNode;

            tail = toAddNode;

            if (count == MaxValues) // pop excessive values
            {
                head = head.previous;
                head.next = null;
                return this;
            }

            count++;

            return this;
        }
        
        public T At(int index)
        {
            if (index > count || index < 0)
                return default(T);

            if (Current != null && (index - Current.Item2) * (index - Current.Item2) <= (count / 2) * (count / 2))
            {
                if (index >= Current.Item2)
                {
                    return SearchDown(Current.Item2, Current.Item1, index);
                }
                else
                {
                    return SearchUp(Current.Item2, Current.Item1, index);
                }
            }
            else if (index >= count / 2)
            {
                return SearchUp(count, tail, index);
            }
            else
            {
                return SearchDown(0, head, index);
            }
        }

        private T SearchDown(int startLocation, Node node, int target)
        {
            while (startLocation != target)
            {
                node = node.previous;
                startLocation++;
            }

            Current = new Tuple<Node, int>(node, target);
            return node.data;
        }
        private T SearchUp(int startLocation, Node node, int target)
        {
            while (startLocation != target)
            {
                node = node.next;
                startLocation--;
            }

            Current = new Tuple<Node, int>(node, target);
            return node.data;
        }
        public bool Remove(T data)
        {
            Node node = head;
            while (node != null)
            {
                if (node.data.Equals(data)) //mfor structs
                {
                    Node nextNode = node.next;
                    Node prevNode = node.previous;

                    if (nextNode != null)
                    {
                        nextNode.previous = prevNode;
                    }
                    if (prevNode != null)
                    {
                        prevNode.next = nextNode;
                    }

                    node = null;
                    count--;
                    return true;
                }
            }
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Enumerator();
        }
        public IEnumerator<T> Enumerator()
        {
            Node current = head;
            while (current != null)
            {
                yield return current.data;
                current = current.previous;
            }
        }
        public IEnumerator<Tuple<T, int>> EnumeratorIndexed()
        {
            Node current = head;
            int localCount = 0;
            while (current != null)
            {
                yield return new Tuple<T, int>(current.data, localCount++);
                current = current.previous;
            }
        }

        public IEnumerator<T> ReverseEnumerator()
        {
            Node current = tail;
            while (current != null)
            {
                yield return current.data;
                current = current.next;
            }
        }
        public IEnumerator<Tuple<T, int>> ReverseEnumeratorIndexed()
        {
            Node current = tail;
            int localCount = count;
            while (current != null)
            {
                yield return new Tuple<T, int>(current.data, localCount--);
                current = current.next;
            }
        }

        public List<T> AsList()
        {
            List<T> returnList = new List<T>(count);
            Node node = head;
            while (node != null)
            {
                returnList.Add(node.data);
                node = node.next;
            }
            return returnList;
        }
        public List<T> AsSortedList()
        {
            List<T> list = AsList();
            list.Sort();
            return list;
        }

        public void Clear()
        {
            tail = null;
            head = tail;
        }
    }

    public static class LinkedQueueExtensions
    {
        public static double Average(this LinkedQueue<double> queue, int count)
        {
            double sum = 0, i = 0;
            LinkedQueue<double>.Node node = queue.tail;

            while (node != null && i < count)
            {
                sum = sum + node.data;
                node = queue.tail.next;
                i++;
            }

            return sum;
        }

        public static int Average(this LinkedQueue<int> queue, int count)
        {
            int sum = 0, i = 0;
            LinkedQueue<int>.Node node = queue.tail;

            while (node != null && i < count)
            {
                sum = sum + node.data;
                node = queue.tail.next;
                i++;
            }

            return sum;
        }

        public static int Max(this LinkedQueue<int> queue)
        {
            int Highest = queue.tail.data;
            LinkedQueue<int>.Node node = queue.tail;
            while (node != null)
            {
                node = queue.tail.next;
                if (Highest < node.data)
                    Highest = node.data;
            }

            return Highest;
        }

        public static double Max(this LinkedQueue<double> queue)
        {
            double Highest = queue.tail.data;
            LinkedQueue<double>.Node node = queue.tail;
            while (node != null)
            {
                node = queue.tail.next;
                if (Highest < node.data)
                    Highest = node.data;
            }

            return Highest;
        }
    }
}
