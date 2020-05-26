using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.Algorithm
{
    public class UsedStateList<T>
    {
        LinkedList<T> usedLinkedList = new LinkedList<T>();
        LinkedList<T> unusedLinkedList = new LinkedList<T>();

        public bool SetNodeState(LinkedListNode<T> item, bool state)
        {
            LinkedList<T> list = item.List;
            if (list == usedLinkedList)
            {
                if (state)
                {
                    return true;
                }

                usedLinkedList.Remove(item);
            }
            else if (list == unusedLinkedList)
            {
                if (!state)
                {
                    return true;
                }

                unusedLinkedList.Remove(item);
            }

            if (state)
            {
                usedLinkedList.AddLast(item);
            }
            else
            {
                unusedLinkedList.AddLast(item);
            }

            return true;
        }
    }
}
