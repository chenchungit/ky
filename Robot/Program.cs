using HSGameEngine.GameEngine.Network;
using HSGameEngine.GameEngine.Network.Protocol;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Robot
{
    class Program
    {
        public static readonly short[] ClientViewGridArray = {
                    0,0,1,0,0,1,0,-1,-1,0,-1,1,1,-1,0,-2,2,0,0,2,-1,-1,1,1,-2,0,-2,1,-2,-1,-1,-2,0,-3,-3,0,1,-2,1,2,
                    -1,2,0,3,2,-1,3,0,2,1,-4,0,0,-4,-3,-1,-1,-3,-1,3,-2,-2,3,-1,1,3,2,2,0,4,-3,1,1,-3,-2,2,2,-2,4,0,
                    3,1,-5,0,2,-3,3,2,-3,-2,4,1,-4,-1,-4,1,1,-4,5,0,2,3,1,4,-3,2,-1,4,3,-2,-1,-4,-2,-3,-2,3,0,5,4,-1,
                    0,-5,-6,0,4,-2,2,-4,-4,2,-3,-3,-4,-2,-2,-4,5,-1,-3,3,-1,5,3,-3,-5,-1,-2,4,0,6,1,5,-5,1,4,2,5,1,6,0,
                    1,-5,-1,-5,0,-6,2,4,3,3,-7,0,-6,1,-2,-5,0,7,-1,-6,4,-3,-5,-2,-3,4,-6,-1,-2,5,1,-6,5,-2,-4,-3,-3,-4,2,-5,
                    7,0,4,3,3,4,-5,2,2,5,1,6,-1,6,6,1,0,-7,6,-1,3,-4,-4,3,5,2,2,6,7,1,-1,7,5,-3,0,8,-2,6,7,-1,
                    8,0,3,5,4,4,6,-2,6,2,1,7,5,3,2,-6,-1,-7,1,-7,-6,2,-4,4,4,-4,-5,3,3,-5,0,-8,-4,-4,-2,-6,-3,-5,-7,-1,
                    -7,1,-5,-3,-6,-2,-3,5,-8,0,-1,8,8,-1,3,-6,2,7,6,-3,1,8,7,-2,-6,3,5,4,-7,2,6,3,5,-4,2,-7,3,6,-4,5,
                    7,2,4,-5,-5,4,-3,6,8,1,0,9,0,-9,-2,7,4,5,9,0,1,-8,9,-1,-1,9,0,10,1,-9,-4,6,6,-4,2,-8,9,1,8,2,
                    -5,5,4,-6,4,6,-6,4,-3,7,7,3,7,-3,5,5,6,4,-2,8,8,-2,10,0,5,-5,3,-7,3,7,2,8,1,9,0,11,11,0,1,10,
                    6,5,5,6,4,7,8,3,9,2,2,9,10,-1,5,-6,9,-2,-4,7,3,-8,-2,9,8,-3,-3,8,4,-7,1,-10,6,-5,-1,10,2,-9,7,-4,
                    -5,6,12,0,7,-5,8,-4,-4,8,6,-6,5,-7,0,12,4,-8,-2,10,3,-9,11,-1,-1,11,2,-10,9,-3,-3,9,10,-2,-2,11,8,-5,11,-2,
                    2,-11,7,-6,12,-1,4,-9,5,-8,-1,12,9,-4,6,-7,-3,10,3,-10,10,-3,-2,12,-11,2,-11,3,-10,3,-9,4,-9,5,-8,5,-8,6,-7,6,
                    -7,7,-6,7,-5,8,-5,9,-4,9,-4,10,-10,2,-9,3,-8,4,-7,5,-6,6,-5,7,-9,2,-8,3,-7,4,-6,5,-10,1,-9,1,-8,2,-7,3,
                    -8,1,-9,0,};
        //public event void SocketConnectCallBack(object sender, SocketConnectEventArgs e)
        //{

        //}

        public static void xxxxx()
        {
            int Max = 0;
            int Min = 0;
           foreach(var i in ClientViewGridArray)
            {
                if (i > Max)
                    Max = i;
                if (i < Min)
                    Min = i;
            }
            Console.WriteLine(string.Format("Max:{0}  Min:{1}", Max, Min));
            int x, y,x1,y1;
            x = y = x1= y1= 0;
            for (int i=0; i< ClientViewGridArray.Length;i+=2)
            {
                if (ClientViewGridArray[i] > x )
                {
                    x = ClientViewGridArray[i];
                    
                }
                if( ClientViewGridArray[i + 1] > y)
                    y = ClientViewGridArray[i+1];
                if (ClientViewGridArray[i] < x1)
                {
                    x1 = ClientViewGridArray[i];

                }
                if (ClientViewGridArray[i + 1] < y1)
                    y1 = ClientViewGridArray[i + 1];

            }
            Console.WriteLine(string.Format("x:{0}  y:{1}", x, y));
            Console.WriteLine(string.Format("x1:{0}  y1:{1}", x1, y1));
        }

        public static void  xxxx()
        {
            for(int i =0;i<=3;i++)
            {
                int f = i;
                if (f == 0)
                {
                    for (int j = 0; j <= 3; j++)
                    {
                        Console.WriteLine(string.Format("X:{0}  Y:{1}", f, j));

                    }
                    for (int j = -1; j >= -3; j--)
                    {
                        Console.WriteLine(string.Format("X:{0}  Y:{1}", f, j));

                    }
                }
                else
                {
                    for (int j = 0; j <= 3; j++)
                    {
                        Console.WriteLine(string.Format("X:{0}  Y:{1}", f, j));

                    }
                    for (int j = -1; j >= -3; j--)
                    {
                        Console.WriteLine(string.Format("X:{0}  Y:{1}", f, j));

                    }
                    f = -i;
                    for (int j = 0; j <= 3; j++)
                    {
                        Console.WriteLine(string.Format("X:{0}  Y:{1}", f, j));


                    }
                    for (int j = -1; j >= -3; j--)
                    {
                        Console.WriteLine(string.Format("X:{0}  Y:{1}", f, j));

                    }
                }
              
                

            }
          
        }
        public static string ToString()
        {
            return StringUtil.substitute("{0}:{1}:{2}:{3}:{4}:{5}", 20191113, 1, 1, 1, 1, 1);
        }

        static void Main(string[] args)
        {
            //  xxxxx();
            ClientManager g_clientManager = new ClientManager();
            g_clientManager.InitClient(1);
            //NetManager g_NetManager = new NetManager();
            //g_NetManager.InitClient();
            //g_NetManager.ConnectSer();
            // g_NetManager.SendData(g_NetManager.FormatData(),1);


            while (true)
            {
                //HttpReposn();
                Thread.Sleep(1000);
            }
        }
    }
}
