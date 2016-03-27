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
    class ClientCommand : ClientSocket
    {
        static int nextID = 1;
        int id;
        public delegate void ClientCommandActionEventHandlerForServer(ClientCommand client);
        Queue<BaseCommand> commands;
        StringBuilder StringBufferForCommands;
        StringBuilder SendBuffer;
        private int counterOfSentCommand = 0;
        private int counterOfReceivedCommand = 0;
        public event ClientCommandActionEventHandlerForServer EventHandlersListForServer;

        public int ID
        {
            get { return id; }
        }

        public static int MaxID
        {
            get { return nextID; }
        }
        public ClientCommand(TcpClient tcp) : base(tcp)
        {
            id = nextID;
            nextID++;
            StringBufferForCommands = new StringBuilder();
            SendBuffer = new StringBuilder();
            commands = new Queue<BaseCommand>();            
            counterOfReceivedCommand = 0;
            counterOfSentCommand = 0;
            EventHandlersListForClientCommand += new ClientSocketActionEventHandlerForClientCommand(ReceiveNewCommand);
        }   
        public void ReceiveNewCommand()
        {
            string messageFromClientSocket;
            messageFromClientSocket = ReceiveMessage();
            if (messageFromClientSocket.Length == 0)
            {
                CurrentStatus = status.error;
                return;
            }
            bool found = true;
            StringBufferForCommands.Append(messageFromClientSocket);            
            while (found)
            {
                int id;
                int size;
                id = (int)StringBufferForCommands[0];
                size = (int)StringBufferForCommands[1];
                if (size <= StringBufferForCommands.Length - 2)
                {
                    commands.Enqueue(BaseCommand.Deserialize(id, StringBufferForCommands.ToString(2, size)));
                    counterOfReceivedCommand++;
                    if (EventHandlersListForServer != null)
                    {
                        EventHandlersListForServer(this);                        
                    }
                    StringBufferForCommands.Remove(0, size + 2);
                }                
                if (StringBufferForCommands.Length == 0 || (int)StringBufferForCommands[1] > StringBufferForCommands.Length - 2 )
                {
                    found = false;
                }                       
            }
        }        

        public Queue<BaseCommand> ReceiveLastCommands()
        {
            Queue<BaseCommand> queue = new Queue<BaseCommand>();
            if (commands.Count > 0)
            {
                while (commands.Count > 0)
                {
                    queue.Enqueue(commands.Dequeue());
                }
                return queue;
            }
            return null;
        }
                
        public bool SendNewCommand(BaseCommand newCommand)
        {
            SendBuffer.Clear();
            string newXmlMessage = newCommand.Serialize();
            SendBuffer.Append((char)newCommand.ID);
            SendBuffer.Append((char)newXmlMessage.Length);
            SendBuffer.Append(newXmlMessage);
            counterOfSentCommand++;
            //Console.WriteLine("Уровень 2");
            return Send(SendBuffer.ToString());        
        }
    }
}
