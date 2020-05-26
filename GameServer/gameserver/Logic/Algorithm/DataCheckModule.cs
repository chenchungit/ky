using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.Algorithm
{
    public class DataCheckModule
    {
        public class CheckLinerValue
        {
            public int[] valueArray;
            private int maxSize;
            private int maxPos;
            private int dataPos;
            private int dataCount;
            private object mutex = new object();

            public CheckLinerValue(int _maxSize)
            {
                maxSize = _maxSize;
                maxPos = maxSize - 1;
                valueArray = new int[maxSize];
            }

            public void Clear()
            {
                Array.Clear(valueArray, 0, valueArray.Length);
            }

            public bool Push(int v, int num, int limit)
            {
                if (num > maxSize)
                {
                    //用法错误,不能忍
                    return false;
                }

                lock (mutex)
                {
                    //数据个数足够才做验证
                    if (num <= dataCount)
                    {
                        int sum = 0;
                        int pos = dataPos;
                        for (int i = 1; i < num; i++)
                        {
                            pos = (pos == 0) ? maxPos : (pos - 1);
                            sum += valueArray[pos];
                        }

                        sum += v;
                        if (sum > limit)
                        {
                            //超标了
                            return false;
                        }
                    }

                    valueArray[dataPos] = v;
                    dataPos = dataPos >= maxPos ? 0 : dataPos + 1;
                    if (dataCount < maxSize) dataCount++;
                }
                
                return true;
            }
        }


    }
}
