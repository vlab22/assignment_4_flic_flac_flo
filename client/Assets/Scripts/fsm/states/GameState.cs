using System;
using System.Linq;
using shared;

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

    private string[] _userNames = new string[2];
    
    public override void EnterState()
    {
        base.EnterState();

        var requestPlayerInfoMessage = new PlayersInfoRequest();
        fsm.channel.SendMessage(requestPlayerInfoMessage);
        
        view.gameBoard.OnCellClicked += _onCellClicked;
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
        if (pMessage is MakeMoveResult makeMoveResult)
        {
            handleMakeMoveResult(makeMoveResult);
        }
        else if (pMessage is PlayersInfoResponse playersInfoResponse)
        {
            handlePlayersInfoResponse(playersInfoResponse);
        }
    }

    private void handlePlayersInfoResponse(PlayersInfoResponse playersInfoResponse)
    {
        var playersInfos = playersInfoResponse.playersInfo;
        
        for (int i = 0; i < _userNames.Length; i++)
        {
            var playerInfo = playersInfos[i];
            if (playerInfo == null || string.IsNullOrWhiteSpace(playerInfo.userName))
            {
                throw new Exception("PlayersInfoResponse with null playerInfo or empty username");
            }

            _userNames[i] = playerInfo.userName;
        }
        
        view.playerLabel1.text = $"P1 {_userNames[0]}";
        view.playerLabel2.text = $"P1 {_userNames[1]}";
        
    }

    private void handleMakeMoveResult(MakeMoveResult pMakeMoveResult)
    {
        view.gameBoard.SetBoardData(pMakeMoveResult.boardData);

        //some label display
        if (pMakeMoveResult.whoMadeTheMove == 1)
        {
            player1MoveCount++;
            view.playerLabel1.text = $"P1 {_userNames[0]} (Movecount: {player1MoveCount})";
        }
        if (pMakeMoveResult.whoMadeTheMove == 2)
        {
            player2MoveCount++;
            view.playerLabel2.text = $"P2 {_userNames[1]} (Movecount: {player2MoveCount})";
        }
    }
}
