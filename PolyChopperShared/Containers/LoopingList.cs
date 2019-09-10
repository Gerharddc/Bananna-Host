using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace PolyChopper.Containers
{
    /// <summary>
    /// This class is used to manipulate a list in a fassion where it can be considdered endless, as in tied at both ends
    /// </summary>
    public class LoopingList<T>
    {
        private List<T> elementList;

        /// <summary>
        /// This method creates a new endless list instance that allows the data in the specified list to be
        /// endlessly looped
        /// </summary>
        /// <param name="refList">The list whose data should be endlessly looped</param>
        public LoopingList(ref List<T> refList)
        {
            elementList = refList;
        }

        /// <summary>
        /// This finds the shortest distance between two elements in a list.
        /// </summary>
        /// <param name="elementA">The starting element</param>
        /// <param name="elementB">The element to which the distance should be determined</param>
        /// <param name="newIndexB">This will be the new index of element B that will be considdered endless</param>
        /// <returns>A positive number if B is higher than A, a negative number is B is lower than a, 0 if they are the same or
        /// if some other error occured</returns>
        public int distanceBetweenElements(T elementA, T elementB, out int newIndexB)
        {
            //First of all determine the indices of the two elements in the local list
            int indexA = elementList.IndexOf(elementA);
            int indexB = elementList.IndexOf(elementB);

            //If they are the same then return 0
            if (indexA == indexB)
            {
                newIndexB = indexB;
                return 0;
            }

            //Next determine the distance from A to B going forward in a looping list
            int forwardDistance = 0;

            if (indexB > indexA)
                //If the index is higher simply determine how much higher it is
                forwardDistance = indexB - indexA;
            else
                //If the index is lower, determine how far it will be to first go to the last point in the list and then
                //by starting at the begining going to the index of B
                forwardDistance = (elementList.Count - indexA) + indexB;

            //Now determine the distance from A to B going backwards in a looping list
            int backwardsDistance = 0;

            if (indexB < indexA)
                //If the index is lower simply determine how much lower it is
                backwardsDistance = indexA - indexB;
            else
                //If the index is higher, determine how far it will be to first go to the first point in the list and then
                //by starting at the end going to the index of B
                backwardsDistance = indexA + (elementList.Count - indexB);

            if (forwardDistance < backwardsDistance)
            {
                newIndexB = indexA + forwardDistance;
                return forwardDistance;
            }
            else
            {
                newIndexB = indexA - backwardsDistance;
                return -backwardsDistance;
            }
        }

        /// <summary>
        /// This mehtod returns the element at the given point in an endless looping list
        /// </summary>
        /// <param name="index">The index of the requested element</param>
        /// <returns>The element at the given point in an endless looping list</returns>
        public T elementAtIndex(int index)
        {
            //To return an item at a position in an endless looping list we simply have to add or subrtact the length
            //of the list to the requested index until it is within range of the list

            while (index > elementList.Count - 1)
                index -= elementList.Count;

            while (index < 0)
                index += elementList.Count;

            return elementList[index];
        }
    }
}
