using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server29._10
{
    public enum phase { waiting, game, result };
    class GameData
    {

        private class Player
        {
            public string name;
            public int row;
            public int col;
            public Color color;
            public DateTime timeOfLastMovement;
            public Player(string name, int col, int row, Color color)
            {
                this.name = name;
                this.col = col;
                this.row = row;
                this.color = color;
                this.timeOfLastMovement = DateTime.Now;
            }
        }
        DateTime timeOfEndingPhaseWaiting;
        DateTime timeOfEndingPhaseGame;
        DateTime timeOfEndingPhaseResult;
        public phase phaseOfGame;
        Dictionary<string, Player> players; 
        List<VisiblePlayers.Player> playersAndCoords;
        List<VisibleObjects.MapObject> mapObjects;
        private Random rand;
        private int[,] labyrinth;
        private int sizeH, sizeW;
        int coordExitCol, coordExitRow;
        System.IO.StreamReader read;
        List<string> namesOfFileLabyrinths;
        public bool successfulFinish = false;
        public GameData()
        {   
            namesOfFileLabyrinths = new List<string>();
            namesOfFileLabyrinths.Add("labyrinth1.txt");
            players = new Dictionary<string, Player>();           
            playersAndCoords = new List<VisiblePlayers.Player>();
            mapObjects = new List<VisibleObjects.MapObject>();
            rand = new Random();            
<<<<<<< HEAD
            timeOfEndingPhaseWaiting = DateTime.Now.AddSeconds(60);
            timeOfEndingPhaseGame = DateTime.Now.AddSeconds(120);
=======
            timeOfEndingPhaseWaiting = DateTime.Now.AddSeconds(40);
            timeOfEndingPhaseGame = DateTime.Now.AddSeconds(50);
>>>>>>> 2eac0e9a599abe3d595c7af7cfdde125fcbf7f70
            timeOfEndingPhaseResult = DateTime.Now.AddSeconds(60);
            ReadLabyrinth("labyrinth1.txt");
            phaseOfGame = phase.waiting;

        }
        public void PlayerMoved(direction movement, string name)
        {
            Player pl = players[name];
            TimeSpan time = DateTime.Now - pl.timeOfLastMovement;
            if (time.Milliseconds > 350)
            {
                if (movement == direction.S && labyrinth[pl.col, pl.row + 1] == 0)
                {
                    labyrinth[pl.col, pl.row] = 0;
                    labyrinth[pl.col, pl.row + 1] += 2;
                    pl.row += 1;
                    pl.timeOfLastMovement = DateTime.Now;
                }
                if (movement == direction.N && labyrinth[pl.col, pl.row - 1] == 0)
                {
                    labyrinth[pl.col, pl.row] = 0;
                    labyrinth[pl.col, pl.row - 1] += 2;
                    pl.row -= 1;
                    pl.timeOfLastMovement = DateTime.Now;
                }
                if (movement == direction.W && labyrinth[pl.col - 1, pl.row] == 0)
                {
                    labyrinth[pl.col, pl.row] = 0;
                    labyrinth[pl.col - 1, pl.row] += 2;
                    pl.col -= 1;
                    pl.timeOfLastMovement = DateTime.Now;
                }
                if (movement == direction.E && labyrinth[pl.col + 1, pl.row] == 0)
                {
                    labyrinth[pl.col, pl.row] = 0;
                    labyrinth[pl.col + 1, pl.row] += 2;
                    pl.col += 1;
                    pl.timeOfLastMovement = DateTime.Now;
                }
                if (pl.row == coordExitRow && pl.col == coordExitCol)
                {
                    successfulFinish = true;
                    FinishGame();
                    timeOfEndingPhaseGame = DateTime.Now;
                }
            }
        }
        public DateTime TimeOfEndingThisWaiting
        {
            get { return timeOfEndingPhaseWaiting; }
            set { timeOfEndingPhaseWaiting = value; }
        }
        public DateTime TimeOfEndingPhaseGame
        {
            get { return timeOfEndingPhaseGame; }
            set { timeOfEndingPhaseGame = value; }
        }
        public DateTime TimeOfEndingPhaseResult
        {
            get { return timeOfEndingPhaseResult; }
            set { timeOfEndingPhaseResult = value; }
        }
        
        public PlayerList FormCommandOfPlayersList()
        {
            List<PlayerList.Player> playersAndColors = new List<PlayerList.Player>();
            foreach (var kvp in players)
            {
                playersAndColors.Add(new PlayerList.Player(kvp.Key, kvp.Value.color));
            }
            return new PlayerList(playersAndColors);
        }

        public TimeLeft FormCommandOfTimeLeft()
        {
            if (phaseOfGame == phase.waiting)
                return new TimeLeft(timeOfEndingPhaseWaiting - DateTime.Now);
            else if (phaseOfGame == phase.game)
                return new TimeLeft(timeOfEndingPhaseGame - DateTime.Now);
            else 
                return new TimeLeft(timeOfEndingPhaseResult - DateTime.Now);
        }

        public PlayerCoords FormCommandOfPlayerCoords(string name)
        {
            Player pl = players[name];
            return new PlayerCoords(pl.col, pl.row);
        }

        public void ReadLabyrinth(string newString)
        {
            read = new System.IO.StreamReader(namesOfFileLabyrinths[0]);
            sizeH = Convert.ToInt32(read.ReadLine());
            sizeW = Convert.ToInt32(read.ReadLine());
            labyrinth = new int[sizeH, sizeW];
            for (int i = 0; i < sizeH; i++)
            {
                newString = read.ReadLine();
                for(int j = 0; j < sizeW; j++)
                {
                    labyrinth[i, j] = Convert.ToInt32(newString[j]) - Convert.ToInt32('0');
                    if (labyrinth[i, j] == 3)
                    {
                        coordExitCol = i;
                        coordExitRow = j;
                    }
                }
            }            
        }

        public Tuple<int, int> GetSizeMaps()
        {
            return new Tuple<int, int>(sizeH, sizeW);
        }

        public MapSize FormCommandOfMapSize()
        {            
            return new MapSize(GetSizeMaps().Item1, GetSizeMaps().Item2);
        }

        public VisiblePlayers FormCommandOfVisiblePlayers(string name)
        {
            Player pl = players[name];
            List<VisiblePlayers.Player> list = new List<VisiblePlayers.Player>();
            foreach (var kvp in players)
            {
                if ((pl.col + 5 > kvp.Value.col) && 
                   (pl.col - 5 < kvp.Value.col) && 
                   (pl.row + 5 > kvp.Value.row) &&
                   (pl.row - 5 < kvp.Value.row) && 
                   (kvp.Value != pl))
                { 
                    list.Add(new VisiblePlayers.Player(kvp.Value.name, kvp.Value.col, kvp.Value.row));
                }      
            }   
            return new VisiblePlayers(list);  
        }

        public VisibleObjects FormCommandOfVisibleObjects(string name)
        {
            List<VisibleObjects.MapObject> list = new List<VisibleObjects.MapObject>();
            Player pl = players[name];
            int ColStart = Math.Max(0, pl.col - 5);
            int ColEnd = Math.Min(sizeW - 1, pl.col + 5);
            int RowStart = Math.Max(0, pl.row - 5);
            int RowEnd = Math.Min(sizeH - 1, pl.row + 5);
            for (int i = ColStart; i < ColEnd; i++)
            {
                for (int j = RowStart; j < RowEnd; j++)
                {
                    if (labyrinth[i, j] == 1)
                    {
                        list.Add(new VisibleObjects.MapObject(types.WALL, i, j));
                    }
                }
            }
            return new VisibleObjects(list);
        }
        public GameOver FormCommandOfGameOver(string name)
        {
            int result = -1;
            Player pl = players[name];
            
            if (pl.row == coordExitRow && pl.col == coordExitCol)
            { 
                result = 1;
            }
            
            return new GameOver(result);
        }

        public void StartGame()
        {
            phaseOfGame = phase.game;            
        }
        public void FinishGame()
        {
            phaseOfGame = phase.result;
        }    

        public void AddNewPlayer(string name)
        {
            Color cl = Color.FromArgb(rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255));            
            int coordRow = 0, coordCol = 0;

            while (labyrinth[coordCol, coordRow] == 1)
            {
                coordCol = rand.Next(0, GetSizeMaps().Item1);
                coordRow = rand.Next(0, GetSizeMaps().Item2);
                foreach (var kvp in players)
                {
                    if (kvp.Value.col == coordCol && kvp.Value.row == coordRow)
                    {
                        coordRow = 0;
                        coordCol = 0;
                    }
                } 
            }
            labyrinth[coordCol, coordRow] = 2;
            players.Add(name, new Player(name, coordCol, coordRow, cl));
        }
        public void DeletePlayer(string name)
        {
            players.Remove(name);
        }
    }
}
