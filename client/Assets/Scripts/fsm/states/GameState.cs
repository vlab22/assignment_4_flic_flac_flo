using System;
using System.Linq;
using shared;
using UnityEngine.Events;

/**
 * This is where we 'play' a game.
 */
public class GameState : ApplicationStateWithView<GameView>
{
    //just for fun we keep track of how many times a player clicked the board
    //note that in the current application you have no idea whether you are player 1 or 2
    //normally it would be better to maintain this sort of info on the server if it is actually important information
    private int player1MoveCount = 0;
    private int player2MoveCount = 0;

    private PlayerInfo[] _players = new PlayerInfo[2];

    private PlayerInfo _thisPlayer;

    public UnityEvent notifyReceiveWinnerResponse;

    public override void EnterState()
    {
        base.EnterState();

        view.resultLabel.text = "";

        var requestPlayerInfoMessage = new PlayersInfoRequest();
        fsm.channel.SendMessage(requestPlayerInfoMessage);

        var whoAmIRequest = new WhoAmIRequest();
        fsm.channel.SendMessage(whoAmIRequest);

        view.gameBoard.OnCellClicked += _onCellClicked;

        view.gameBoard.SetBoardData(new TicTacToeBoardData()
        {
            board = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0}
        });
    }

    private void _onCellClicked(int pCellIndex)
    {
        MakeMoveRequest makeMoveRequest = new MakeMoveRequest();
        makeMoveRequest.move = pCellIndex;

        fsm.channel.SendMessage(makeMoveRequest);
    }

    public override void ExitState()
    {
        base.ExitState();
        view.gameBoard.OnCellClicked -= _onCellClicked;
    }


    private void Update()
    {
        receiveAndProcessNetworkMessages();
    }

    protected override void handleNetworkMessage(ASerializable pMessage)
    {
        switch (pMessage)
        {
            case MakeMoveResult makeMoveResult:
                handleMakeMoveResult(makeMoveResult);
                break;
            case PlayersInfoResponse playersInfoResponse:
                handlePlayersInfoResponse(playersInfoResponse);
                break;
            case WinnerConditionResponse winnerResponse:
                handleWinnerConditionResponse(winnerResponse);
                break;
            case WhoAmIResponse whoAmIResponse:
                _thisPlayer = new PlayerInfo()
                {
                    id = whoAmIResponse.idInRoom,
                    userName = whoAmIResponse.userName,
                };
                break;
            case RoomJoinedEvent roomJoinedEvent:
                if (roomJoinedEvent.room == RoomJoinedEvent.Room.LOBBY_ROOM)
                {
                    fsm.ChangeState<LobbyState>();
                }

                break;
        }
    }

    private void handleWinnerConditionResponse(WinnerConditionResponse winnerResponse)
    {
        var winnerUser = winnerResponse.winnerUser;
        var winnerId = winnerResponse.winnerId;
        var gameRoomId = winnerResponse.gameRoomId;

        var resultMsg = "You lose!";
        if (winnerId == _thisPlayer.id)
        {
            resultMsg = "You Won!";
        }

        view.resultLabel.text = resultMsg;
        
        notifyReceiveWinnerResponse?.Invoke();
    }

    private void handlePlayersInfoResponse(PlayersInfoResponse playersInfoResponse)
    {
        _players = playersInfoResponse.playersInfo;

        view.playerLabel1.text = $"P1 {_players[0].userName}";
        view.playerLabel2.text = $"P1 {_players[1].userName}";
    }

    private void handleMakeMoveResult(MakeMoveResult pMakeMoveResult)
    {
        view.gameBoard.SetBoardData(pMakeMoveResult.boardData);

        //some label display
        if (pMakeMoveResult.whoMadeTheMove == 1)
        {
            player1MoveCount++;
            view.playerLabel1.text = $"P1 {_players[0].userName} (Movecount: {player1MoveCount})";
        }

        if (pMakeMoveResult.whoMadeTheMove == 2)
        {
            player2MoveCount++;
            view.playerLabel2.text = $"P2 {_players[1].userName} (Movecount: {player2MoveCount})";
        }
    }

    public void RequestToLeave()
    {
        fsm.channel.SendMessage(new LeaveGameRequest());
    }
}