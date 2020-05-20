using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DubsAnalyzer
{
    // To eventually be used as the data structure for holding Log & Frametime History

    public class LinkedQueue<T>
    {
        private class Node
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

        private Node head; // the head of the queue is actually the 'newest' member
        private Node tail; // the tail of the queue is the 'oldest' member
        private int maxValues = int.MaxValue;
        public int MaxValues { get; set; }
        private int count = 0;
        public int Count => count;

        public LinkedQueue()
        {
            head = null;
            tail = head;
        }
        public LinkedQueue(int maxValue)
        {
            head = null;
            tail = head;
            this.MaxValues = maxValue;
        }

        public LinkedQueue<T> Enqueue(T data)
        {
            Node toAddNode = new Node(data);
            toAddNode.next = head;
            toAddNode.previous = null;

            if (head != null)
                head.previous = toAddNode;

            head = toAddNode;
            count++;

            if (count > maxValues) // pop excessive values for memory
            {
                tail = tail.next;
                tail.previous = null;
            }
            return this;
        }
        public T Peak()
        {
            try
            {
                return head.data;
            } catch (Exception)
            {
                return default(T);
            }
        }
        public T Pop()
        {
            try
            {
                T returnValue = tail.data;
                tail = null;
                count--;
                return returnValue;
            } catch (Exception)
            {
                return default(T);
            }
        }
        public bool Remove(T data)
        {
            Node node = head;
            while(node != null)
            {
                if(node.data.Equals(data)) // we can use linkedqueues for structs
                {
                    Node nextNode = node.next;
                    Node prevNode = node.previous;

                    if(nextNode != null)
                        nextNode.previous = prevNode;
                    if(prevNode != null)
                        prevNode.next = nextNode;

                    node = null;
                    count--;
                    return true;
                }
            }
            return false;
        }

        public IEnumerator<T> Enumerator()
        {
            Node current = head;
            while (current != null)
            {
                yield return current.data;
                current = current.next;
            }
        }
        public IEnumerator<Tuple<T, int>> EnumeratorIndexed()
        {
            Node current = head;
            int localCount = 0;
            while (current != null)
            {
                yield return new Tuple<T, int>(current.data, localCount++);
                current = current.next;
            }
        }

        public IEnumerator<T> ReverseEnumerator()
        {
            Node current = tail;
            while (current != null)
            {
                yield return current.data;
                current = current.previous;
            }
        }
        public IEnumerator<Tuple<T, int>> ReverseEnumeratorIndexed()
        {
            Node current = tail;
            int localCount = count;
            while (current != null)
            {
                yield return new Tuple<T, int>(current.data, localCount--);
                current = current.previous;
            }
        }

        public List<T> AsList()
        {
            List<T> returnList = new List<T>(count);
            Node node = head;
            while(node != null)
            {
                returnList.Add(node.data);
                node = node.next;
            }
            return returnList;
        }
        public List<T> AsSortedList()
        {
            var list = AsList();
            list.Sort();
            return list;
        }
    }
}
