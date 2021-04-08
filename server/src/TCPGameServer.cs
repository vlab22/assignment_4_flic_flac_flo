using System;
using System.Net.Sockets;
using System.Net;
using shared;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace server
{
    /**
	 * Basic TCPGameServer that runs our game.
	 * 
	 * Server is made up out of different rooms that can hold different members.
	 * Each member is identified by a TcpMessageChannel, which can also be used for communication.
	 * In this setup each client is only member of ONE room, but you could change that of course.
	 * 
	 * Each room is responsible for cleaning up faulty clients (since it might involve gameplay, status changes etc).
	 * 
	 * As you can see this setup is limited/lacking:
	 * - only 1 game can be played at a time
	 */
    class TCPGameServer
    {
        public static void Main(string[] args)
        {
            TCPGameServer tcpGameServer = new TCPGameServer();
            tcpGameServer.run();
        }

        //we have 3 different rooms at the moment (aka simple but limited)

        private LoginRoom _loginRoom; //this is the room every new user joins
        private LobbyRoom _lobbyRoom; //this is the room a user moves to after a successful 'login'
        private List<GameRoom> _gameRooms; //this is the room a user moves to when a game is succesfully started

        //stores additional info for a player
        private Dictionary<TcpMessageChannel, PlayerInfo> _playerInfo = new Dictionary<TcpMessageChannel, PlayerInfo>();

        //Simple timer count
        private Stopwatch _stopwatch;

        //Heartbeat check fields

        // dictionary to record the last heartbeat sent time to clients
        private Dictionary<TcpMessageChannel, float> _heartBeatClientTime;

        private float _heartBeatElapsedTime;
        private const float _TIMEOUT = 3000;

        private TCPGameServer()
        {
            //we have only one instance of each room, this is especially limiting for the game room (since this means you can only have one game at a time).
            _loginRoom = new LoginRoom(this);
            _lobbyRoom = new LobbyRoom(this);
            _gameRooms = new List<GameRoom>();

            _stopwatch = Stopwatch.StartNew();
            _heartBeatClientTime = new Dictionary<TcpMessageChannel, float>();
            _heartBeatElapsedTime = _stopwatch.ElapsedMilliseconds;
        }

        private void run()
        {
            Log.LogInfo("Starting server on port 55555", this, ConsoleColor.Gray);

            //start listening for incoming connections (with max 50 in the queue)
            //we allow for a lot of incoming connections, so we can handle them
            //and tell them whether we will accept them or not instead of bluntly declining them
            TcpListener listener = new TcpListener(IPAddress.Any, 55555);
            listener.Start(50);

            while (true)
            {
                //check for new members	
                if (listener.Pending())
                {
                    //get the waiting client
                    Log.LogInfo("Accepting new client...", this, ConsoleColor.White);
                    TcpClient client = listener.AcceptTcpClient();
                    //and wrap the client in an easier to use communication channel
                    TcpMessageChannel channel = new TcpMessageChannel(client);
                    //and add it to the login room for further 'processing'

                    _loginRoom.AddMember(channel);
                }

                //now update every single room
                _loginRoom.Update();
                _lobbyRoom.Update();

                foreach (var gameRoom in _gameRooms)
                {
                    gameRoom.Update();
                }

                HeartBeatClients();

                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Send heart beat request to clients, 
        /// </summary>
        private void HeartBeatClients()
        {
            var now = _stopwatch.ElapsedMilliseconds;
            if (_playerInfo.Count > 0 && now - _heartBeatElapsedTime > _TIMEOUT)
            {
                foreach (var kv in _playerInfo)
                {
                    var client = kv.Key;
                    client.SendMessage(new HeartBeatRequest());
                }

                _heartBeatElapsedTime = _stopwatch.ElapsedMilliseconds;
                
                Log.LogInfo($"HeartBeat clients at {now * 0.001f} secs", this);
            }
        }

        //provide access to the different rooms on the server 
        public LoginRoom GetLoginRoom()
        {
            return _loginRoom;
        }

        public LobbyRoom GetLobbyRoom()
        {
            return _lobbyRoom;
        }

        public GameRoom GetGameRoom(int index)
        {
            return index > -1 && index < _gameRooms.Count ? _gameRooms[index] : null;
        }

        /**
		 * Returns a handle to the player info for the given client 
		 * (will create new player info if there was no info for the given client yet)
		 */
        public PlayerInfo GetPlayerInfo(TcpMessageChannel pClient)
        {
            if (!_playerInfo.ContainsKey(pClient))
            {
                _playerInfo[pClient] = new PlayerInfo();
            }

            return _playerInfo[pClient];
        }

        /**
		 * Returns a list of all players that match the predicate, e.g. to get a list of 
		 * all players named bob, you would do:
		 *	GetPlayerInfo((playerInfo) => playerInfo.name == "bob");
		 */
        public List<PlayerInfo> GetPlayerInfo(Predicate<PlayerInfo> pPredicate)
        {
            return _playerInfo.Values.ToList<PlayerInfo>().FindAll(pPredicate);
        }

        /**
		 * Should be called by a room when a member is closed and removed.
		 */
        public void RemovePlayerInfo(TcpMessageChannel pClient)
        {
            _playerInfo.Remove(pClient);
        }

        public GameRoom CreateAGameRoom()
        {
            var gameRoom = new GameRoom(this);
            _gameRooms.Add(gameRoom);

            Log.LogInfo($"GameRoom {_gameRooms.Count} created", this);

            return gameRoom;
        }

        public void DestroyGameRoom(GameRoom pGameRoom)
        {
            int gameRoomId = _gameRooms.IndexOf(pGameRoom) + 1;
            _gameRooms.Remove(pGameRoom);

            Log.LogInfo($"GameRoom {gameRoomId} destroyed", this);
        }

        public ReadOnlyCollection<GameRoom> GameRooms => _gameRooms.AsReadOnly();
    }
}