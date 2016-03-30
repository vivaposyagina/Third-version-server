using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server29._10
{
    class Server
    {
        private class ListOfCommands
        {
            ClientCommand client;
            public Queue<BaseCommand> queueOfCommands;

            public ListOfCommands(ClientCommand cl)
            {
                client = cl;
                queueOfCommands = new Queue<BaseCommand>();
            }

            public void AddNewCommand(BaseCommand cmd)
            {
                queueOfCommands.Enqueue(cmd);
            }
        }
        GameData dataOfThisGame;
        ServerCommand serverCommand;
        Dictionary<int, ClientCommand> clients;
        Dictionary<int, string> listOfPlayersAndTheirNickname;
        List<ListOfCommands> listOfCmd;
        public status currentStatus = status.off;
        Thread workerThread;
        public Server()
        {
            serverCommand = new ServerCommand();
            serverCommand.EventHandlerListForServer += new ServerSocket.TcpClientActionEventHandler(AddNewClientCommand);
            clients = new Dictionary<int, ClientCommand>();
            listOfPlayersAndTheirNickname = new Dictionary<int, string>();
            workerThread = new Thread(new ThreadStart(WorkerThread));
            listOfCmd = new List<ListOfCommands>();
        }

        public void Process(ClientCommand client)
        {
            foreach (var it in clients)
            {
                if (it.Value == client)
                {
                    while (listOfCmd[it.Key - 1].queueOfCommands.Count > 0)
                    {
                        BaseCommand bcmd = listOfCmd[it.Key - 1].queueOfCommands.Dequeue();
                        switch (bcmd.ID)
                        {
                            case 2:
                                Intro command = bcmd as Intro;
                                Response answer = null;
                                if (dataOfThisGame.phaseOfGame == phase.game || dataOfThisGame.phaseOfGame == phase.result)
                                {
                                    answer = new Response("notok", "Game has already start");
                                    client.SendNewCommand(answer as BaseCommand);
                                    break;
                                }
                                bool flag = true;

                                foreach (KeyValuePair<int, string> player in listOfPlayersAndTheirNickname)
                                {
                                    if (player.Value == command.Name)
                                    {
                                        answer = new Response("notok", "Player with this nickname already exists");
                                        client.SendNewCommand(answer as BaseCommand);
                                        flag = false;
                                        break;
                                    }
                                }
                                if (flag)
                                {
                                    answer = new Response("ok", "welcome, " + command.Name);
                                    listOfPlayersAndTheirNickname.Add(client.ID, command.Name);
                                    dataOfThisGame.AddNewPlayer(command.Name);

                                    client.SendNewCommand(answer as BaseCommand);
                                    client.SendNewCommand(dataOfThisGame.FormCommandOfTimeLeft() as BaseCommand);

                                    foreach (var item in clients)
                                    {
                                        item.Value.SendNewCommand(dataOfThisGame.FormCommandOfPlayersList() as BaseCommand);
                                    }
                                }

                                break;
                            case 4:
                                Chat messageCommand = bcmd as Chat;
                                foreach (var item in clients)
                                {
                                    item.Value.SendNewCommand(messageCommand as BaseCommand);
                                }
                                break;
                            case 10:
                                PlayerMove movement = bcmd as PlayerMove;
                                foreach (var item in clients)
                                {
                                    if (client == item.Value)
                                    {
                                        dataOfThisGame.PlayerMoved(movement.Direction, listOfPlayersAndTheirNickname[item.Key]);
                                        item.Value.SendNewCommand(dataOfThisGame.FormCommandOfPlayerCoords(listOfPlayersAndTheirNickname[item.Key]) as BaseCommand);
                                        item.Value.SendNewCommand(dataOfThisGame.FormCommandOfVisibleObjects(listOfPlayersAndTheirNickname[item.Key]) as BaseCommand);
                                    }
                                    item.Value.SendNewCommand(dataOfThisGame.FormCommandOfVisiblePlayers(listOfPlayersAndTheirNickname[item.Key]) as BaseCommand);
                                }
                                break;
                            case 12:
                                PlayerDisconnect disconnectCommand = bcmd as PlayerDisconnect;

                                foreach (var item in clients)
                                {
                                    if (client == item.Value)
                                    {
                                        item.Value.Disconnect();
                                        clients.Remove(item.Key);
                                        Console.WriteLine("Клиент " + listOfPlayersAndTheirNickname[item.Key] + " отключился");
                                        dataOfThisGame.DeletePlayer(listOfPlayersAndTheirNickname[item.Key]);
                                        listOfPlayersAndTheirNickname.Remove(item.Key);
                                    }
                                }
                                client.Disconnect();
                                foreach (var item in clients)
                                {
                                    item.Value.SendNewCommand(dataOfThisGame.FormCommandOfPlayersList() as BaseCommand);
                                }
                                break;
                            default:
                                Console.WriteLine("Неизвестная команда");
                                break;
                        }
                    }
                }
            }
        }
        public void CheckClients()
        {
            Queue<int> key = new Queue<int>();
            int x;
            foreach (var item in clients)
            {
                if (item.Value.CurrentStatus == status.error)
                {
                    item.Value.Disconnect();
                    key.Enqueue(item.Key);
                    dataOfThisGame.DeletePlayer(listOfPlayersAndTheirNickname[item.Key]);
                    Console.WriteLine("Клиент " + listOfPlayersAndTheirNickname[item.Key] + " перестал отвечать");
                    listOfPlayersAndTheirNickname.Remove(item.Key);
                    foreach (var flag in clients)
                    {
                        flag.Value.SendNewCommand(dataOfThisGame.FormCommandOfPlayersList() as BaseCommand);
                    }
                }
                else
                    Process(item.Value);
            }
            while (key.Count > 0)
            {
                x = key.Dequeue();
                clients.Remove(x);
            }
        }
        public void WorkerThread()
        {
            bool WhetherDataIsSentToStartGame = false;
            bool WhetherDataIsSentToFinishGame = false;
            DateTime timeForSendingLeftTime = DateTime.Now.AddSeconds(10);
            while (currentStatus == status.on)
            {
                Thread.Sleep(30);
                CheckClients();

                if (dataOfThisGame.phaseOfGame == phase.waiting || dataOfThisGame.phaseOfGame == phase.game)
                {
                    if (DateTime.Compare(DateTime.Now, timeForSendingLeftTime) > 0)
                    {
                        SendTimeLeft();
                        timeForSendingLeftTime = timeForSendingLeftTime.AddSeconds(10);
                    }
                }
                if (DateTime.Now < dataOfThisGame.TimeOfEndingPhaseGame && DateTime.Now > dataOfThisGame.TimeOfEndingThisWaiting && !WhetherDataIsSentToStartGame)
                {
                    dataOfThisGame.StartGame();
                    foreach (var item in clients)
                    {
                        item.Value.SendNewCommand(dataOfThisGame.FormCommandOfMapSize() as BaseCommand);
                        item.Value.SendNewCommand(dataOfThisGame.FormCommandOfPlayerCoords(listOfPlayersAndTheirNickname[item.Key]) as BaseCommand);
                        item.Value.SendNewCommand(dataOfThisGame.FormCommandOfVisibleObjects(listOfPlayersAndTheirNickname[item.Key]) as BaseCommand);
                        item.Value.SendNewCommand(dataOfThisGame.FormCommandOfVisiblePlayers(listOfPlayersAndTheirNickname[item.Key]) as BaseCommand);
                    }
                    WhetherDataIsSentToStartGame = true;
                }

                if ((DateTime.Now > dataOfThisGame.TimeOfEndingPhaseGame
                    && DateTime.Now < dataOfThisGame.TimeOfEndingPhaseResult
                    || dataOfThisGame.phaseOfGame == phase.result)
                    && !WhetherDataIsSentToFinishGame)
                {
                    dataOfThisGame.FinishGame();
                    foreach (var item in clients)
                    {
                        item.Value.SendNewCommand(dataOfThisGame.FormCommandOfGameOver(listOfPlayersAndTheirNickname[item.Key]) as BaseCommand);
                    }
                    WhetherDataIsSentToFinishGame = true;
                }
                if (DateTime.Now > dataOfThisGame.TimeOfEndingPhaseResult)
                {
                    FinalizationWorkingServer();
                }
            }
        }
        public void SendTimeLeft()
        {
            TimeLeft tl = dataOfThisGame.FormCommandOfTimeLeft();
            foreach (var item in clients)
            {
                item.Value.SendNewCommand(tl);
            }
        }
        public void InitializationServer()
        {
            serverCommand.StartListener();
            dataOfThisGame = new GameData();
            currentStatus = status.on;
            workerThread.Start();
        }
        public void FinalizationWorkingServer()
        {
            serverCommand.StopListener();
            foreach (var item in clients)
            {
                item.Value.Disconnect();
            }
            currentStatus = status.off;
        }
        public void UpdateQueueOfCommands(ClientCommand client)
        {
            Queue<BaseCommand> que = client.ReceiveLastCommands();
            
            foreach(var item in clients)
            {
                if (item.Value == client)
                {
                    while (que.Count > 0)
                    {
                        listOfCmd[item.Key - 1].AddNewCommand(que.Dequeue());
                    }
                }

            }
        }
        public void AddNewClientCommand()
        {
            ClientCommand cl = serverCommand.AcceptClientCommand();
            if (cl != null)
            {
                clients.Add(cl.ID, cl);
                listOfCmd.Add(new ListOfCommands(cl));
                clients.Last().Value.EventHandlersListForServer += new ClientCommand.ClientCommandActionEventHandlerForServer(UpdateQueueOfCommands);
            }
        }
    }
}
