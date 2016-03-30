using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

namespace Server29._10
{

    class ClientSocket
    {
        public delegate void ClientSocketActionEventHandlerForClientCommand();
        const int MAX_BUFFER_SIZE = 1000;
        Thread workerThread;
        protected TcpClient tcp;
        byte[] buffer;
        NetworkStream stream;
        int length = 0;
        protected status currentStatus = status.off;        
        public event ClientSocketActionEventHandlerForClientCommand EventHandlersListForClientCommand;        
        public ClientSocket(TcpClient client)
        {
            tcp = client;
            tcp.NoDelay = true;
            stream = tcp.GetStream();
            currentStatus = status.on;
            buffer = new byte[MAX_BUFFER_SIZE];
            workerThread = new Thread(new ThreadStart(MessageReceiver));
            workerThread.Start();
        }
        ~ClientSocket()
        {
            Disconnect();
        }
        public status CurrentStatus
        {
            get { return currentStatus; }
            set { currentStatus = value; }
        }
        public int Length
        {
            get
            {
                return length;
            }
        }
        public byte[] Buffer
        {
            get
            {
                return buffer;
            }
        }
        public void ClearBuffer()
        {
            Array.Clear(this.buffer, 0, this.buffer.Length);
            length = 0;
        }
        void MessageReceiver()
        {
            byte[] localBuffer = new byte[MAX_BUFFER_SIZE];
            while (true)
            {
                try
                {
                    int lenghtNewMessage = stream.Read(localBuffer, 0, MAX_BUFFER_SIZE);
                    if (lenghtNewMessage == 0)
                    {
                        currentStatus = status.error;
                        break;
                    }                    
                    lock (buffer)
                    {
                        Array.Copy(localBuffer, 0, buffer, 0, lenghtNewMessage);
                        length = lenghtNewMessage;
                    }
                    
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    currentStatus = status.error;
                    return;
                }
                if (EventHandlersListForClientCommand != null)
                {
                    EventHandlersListForClientCommand();
                }
            }
        }
        public bool IsConnected()
        {
            if (currentStatus == status.on)
                return true;
            return false;
        }
        protected string ReceiveMessage()
        {
            lock (buffer)
            {
                string str;
                lock (buffer)
                {
                    str = System.Text.Encoding.Unicode.GetString(buffer, 0, length);
                }
                ClearBuffer();
                return str;
            }
        }
        public bool Send(string data)
        {
            if (!IsConnected())
                return false;
            if (data.Length != 0)
            {
                byte[] bytes = Encoding.Unicode.GetBytes(data);
                try
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }
                catch (Exception)
                {
                    currentStatus = status.error;
                    return false;
                }
            }
            return true;
        }
        public void Disconnect()
        {
            if (!IsConnected())
                return;
            tcp.Close();
            stream = null;
            currentStatus = status.off;
        }
    }
}
