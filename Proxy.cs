using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Net.Sockets;

namespace TcpForwarding
{
    public class Proxy
    {
        const int READ_TIMEOUT = 120000;
        const int BUFF_SIZE = 81920;
        const int CONNECT_TIMEOUT = 5000;
        private IPEndPoint listen;
        private IList<IPEndPoint> forwardingList;
        volatile NetworkStream listenStream = null;
        volatile NetworkStream forwardingStream = null;
        
        public Proxy(IPEndPoint listen, IList<IPEndPoint> forwardingList)
        {
            this.listen = listen;
            this.forwardingList = forwardingList;

        }

        public void Start()
        {
            Thread listen = new Thread(new ThreadStart(ListenThread));
            Thread forwarding = new Thread(new ThreadStart(ForwardingThread));
            listen.IsBackground = forwarding.IsBackground = true;
            listen.Start();
            forwarding.Start();
        }

        private void Forwarding(byte[] bytes)
        {
            try
            {
                if (forwardingStream == null)
                {
                    Console.WriteLine("Forwarding: forwardingStream == null");
                    return;
                }
                forwardingStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Forwarding:" + ex.Message);
               
            }
        }

        private void Response(byte[] bytes)
        {
            try
            {
                if (listenStream == null)
                {
                    Console.WriteLine("Response: listenStream == null");
                    return;
                }
                listenStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Response:" + ex.Message);

            }
        }

        void ListenThread()
        {
            byte[] buffer = new Byte[BUFF_SIZE];
            while (true)
            {
                TcpListener server = null;
                TcpClient client = null;
                try
                {
                    server = new TcpListener(listen);
                    server.Start(1);
                    client = server.AcceptTcpClient();
                    server.Stop();
                    server = null;
                    listenStream = client.GetStream();
                    listenStream.ReadTimeout = READ_TIMEOUT;
                    while (true)
                    {
                        int len = listenStream.Read(buffer, 0, BUFF_SIZE);
                        if (len > 0)
                        {
                            byte[] tmp = new byte[len];
                            Array.Copy(buffer, tmp, len);
                            Forwarding(tmp);
                        }
                        else
                        {
                            Console.WriteLine("listener read {0}", len);
                            break;
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("ListenThread: " + ex.Message);
                    Thread.Sleep(1);
                }
                finally
                {
                    if (server != null)
                    {
                        server.Stop();
                        server = null;
                    }
                    if (client != null)
                    {
                        client.Close();
                        client = null;
                    }
                }
            }
        }


        void ForwardingThread()
        {

            byte[] buffer = new Byte[BUFF_SIZE];
            int index = -1;

            while (true)
            {
                index++;
                index = index % forwardingList.Count;
                var currentEndPoint = forwardingList[index];

                TcpClient client = null;
                try
                {
                    client = new TcpClient();
                    var result = client.BeginConnect(currentEndPoint.Address, currentEndPoint.Port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(CONNECT_TIMEOUT));
                    if (!success)
                    {
                        Console.WriteLine("connect {0} timeout", currentEndPoint);
                        continue;
                    }
                    client.EndConnect(result);

                    forwardingStream = client.GetStream();
                    forwardingStream.ReadTimeout = READ_TIMEOUT;
                   
                    while (true)
                    {
                        int len = forwardingStream.Read(buffer, 0, BUFF_SIZE);
                        if (len > 0)
                        {
                            byte[] tmp = new byte[len];
                            Array.Copy(buffer, tmp, len);
                            Response(tmp);
                        }
                        else
                        {
                            Console.WriteLine("forwarding read {0}", len);
                            break;
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("ForwardingThread: " + ex.Message);
                    Thread.Sleep(1);
                }
                finally
                {
 
                    if (client != null)
                    {
                        client.Close();
                        client = null;
                    }
                }
            }
        }
    }



   
}
