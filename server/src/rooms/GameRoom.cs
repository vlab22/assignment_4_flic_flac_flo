using shared;
using System;
using System.Collections;
using System.Linq;
using System.Threading;

namespace server
{
    /**
	 * This room runs a single Game (at a time). 
	 * 
	 * The 'Game' is very simple at the moment:
	 *	- all client moves are broadcasted to all clients
	 *	
	 * The game has no end yet (that is up to you), in other words:
	 * all players that are added to this room, stay in here indefinitely.
	 */
    class GameRoom : Room
    {
        public bool IsGameInPlay { get; private set; }

        public bool matchEnds;

        //wraps the board to play on...
        private TicTacToeBoard _board = new TicTacToeBoard();

        public GameRoom(TCPGameServer pOwner) : base(pOwner)
        {
        }

        public void StartGame(TcpMessageChannel pPlayer1, TcpMessageChannel pPlayer2)
        {
            if (IsGameInPlay) throw new Exception("Programmer error duuuude.");

            IsGameInPlay = true;
            addMember(pPlayer1);
            addMember(pPlayer2);
        }

        protected override void addMember(TcpMessageChannel pMember)
        {
            base.addMember(pMember);

            //notify client he has joined a game room 
            RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent();
            roomJoinedEvent.room = RoomJoinedEvent.Room.GAME_ROOM;
            pMember.SendMessage(roomJoinedEvent);
        }

        public override void Update()
        {
            //demo of how we can tell people have left the game...
            int oldMemberCount = memberCount;
            base.Update();
            int newMemberCount = memberCount;

            if (oldMemberCount != newMemberCount)
            {
                Log.LogInfo("People left the game...", this);
            }
        }

        protected override void handleNetworkMessage(ASerializable pMessage, TcpMessageChannel pSender)
        {
            switch (pMessage)
            {
                case MakeMoveRequest makeMoveRequest:
                    handleMakeMoveRequest(makeMoveRequest, pSender);
                    break;
                case PlayersInfoRequest playersInfoRequest:
                    handlePlayersInfoRequest(pSender);
                    break;
                case WhoAmIRequest whoAmIRequest:
                    var whoAmIResponse = new WhoAmIResponse()
                    {
                        idInRoom = Members.IndexOf(pSender) + 1,
                        userName = _server.GetPlayerInfo(pSender).userName
                    };
                    pSender.SendMessage(whoAmIResponse);
                    break;
                case LeaveGameRequest leaveGameRequest:
                    //The winner is the player in the room that didn't request to leave
                    var winnerMember = Members.FirstOrDefault(m => m != pSender);
                    int winnerId = _server.GetPlayerInfo((winnerMember)).id;
                    SendWinnerMsgAndPlayerToLobby(winnerId, " by \"Conceding\"");

                    break;
            }
        }

        private void handlePlayersInfoRequest(TcpMessageChannel pSender)
        {
            var playersInfo = GetPlayersInfos();

            var playerInfoResponse = new PlayersInfoResponse();

            int i = 0;
            foreach (var info in playersInfo)
            {
                info.id = i + 1;
                playerInfoResponse.playersInfo[i] = info;
                i++;
            }

            pSender.SendMessage(playerInfoResponse);
        }

        private void handleMakeMoveRequest(MakeMoveRequest pMessage, TcpMessageChannel pSender)
        {
            if (matchEnds)
                return;

            //we have two players, so index of sender is 0 or 1, which means playerID becomes 1 or 2
            int playerID = indexOfMember(pSender) + 1;
            //make the requested move (0-8) on the board for the player
            _board.MakeMove(pMessage.move, playerID);

            ProcessWinCondition();

            //and send the result of the boardstate back to all clients
            MakeMoveResult makeMoveResult = new MakeMoveResult();
            makeMoveResult.whoMadeTheMove = playerID;
            makeMoveResult.boardData = _board.GetBoardData();
            sendToAll(makeMoveResult);
        }

        private void ProcessWinCondition()
        {
            //Check Win Condition
            var boardData = _board.GetBoardData();
            int winnerId = boardData.WhoHasWon();
            if (winnerId > 0)
            {
                SendWinnerMsgAndPlayerToLobby(winnerId);
            }
        }

        /// <summary>
        /// Send a WinnerConditionResponse, wait, move player to lobby and send a chat msg to all
        /// </summary>
        /// <param name="winnerId"></param>
        private void SendWinnerMsgAndPlayerToLobby(int winnerId, string msg = "")
        {
            var gameRoomId = _server.GameRooms.IndexOf(this);
            var winnerUser = _server.GetPlayerInfo(p => p.id == winnerId).FirstOrDefault()?.userName;
            var winnerResponse = new WinnerConditionResponse()
            {
                winnerId = winnerId,
                winnerUser = winnerUser,
                gameRoomId = gameRoomId
            };

            Members.ForEach(pChannel => pChannel.SendMessage(winnerResponse));

            matchEnds = true;

            var looserInfo = GetOtherPlayerInfoById(winnerId);
            var loserUserName = looserInfo?.userName; 
            
            CoroutineManager.StartCoroutine(WaitAndSendWinLoseMessage(msg, winnerUser, loserUserName, gameRoomId), this);
            
            // var thread = new Thread(delegate(object pO)
            // {
            //     Log.LogInfo("Waiting 2 secs", this);
            //
            //     Thread.Sleep(2000);
            //
            //     Log.LogInfo("Waited 2 secs", this);
            //
            //     var message = $"===> {winnerUser} won {loserUserName}{msg} in GameRoom {gameRoomId}";
            //
            //     SendPlayersToLobby();
            //
            //     var chatMsg = new ChatMessage()
            //     {
            //         message = message
            //     };
            //     _server.GetLobbyRoom().sendToAll(chatMsg);
            // });
            //
            // thread.Start();
        }

        private IEnumerator WaitAndSendWinLoseMessage(string pMsg, string pWinnerUser, string pLooserName, int pRoomId)
        {
            Log.LogInfo("Waiting 2 secs", this);

            yield return new WaitForMilliSeconds(2000);
            
            Log.LogInfo("Waited 2 secs", this);
            
            var message = $"===> {pWinnerUser} won {pLooserName}{pMsg} in GameRoom {pRoomId}";
            
            SendPlayersToLobby();
            
            var chatMsg = new ChatMessage()
            {
                message = message
            };
            _server.GetLobbyRoom().sendToAll(chatMsg);
        }

        protected override void clientDisconnected(TcpMessageChannel pChannel)
        {
            base.clientDisconnected(pChannel);

            var winnerMember = Members.FirstOrDefault(m => m != pChannel);
            var winnerId = _server.GetPlayerInfo(winnerMember).id;
            SendWinnerMsgAndPlayerToLobby(winnerId, " by Rage Quit");
        }

        private void SendPlayersToLobby()
        {
            for (int i = memberCount - 1; i >= 0; i--)
            {
                var member = Members[i];
                removeMember(Members[i]);
                _server.GetLobbyRoom().AddMember(member);
            }
        }
    }
}