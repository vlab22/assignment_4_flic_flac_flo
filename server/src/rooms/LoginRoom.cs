using System.Collections.Generic;
using shared;

namespace server
{
	/**
	 * The LoginRoom is the first room clients 'enter' until the client identifies himself with a PlayerJoinRequest. 
	 * If the client sends the wrong type of request, it will be kicked.
	 *
	 * A connected client that never sends anything will be stuck in here for life,
	 * unless the client disconnects (that will be detected in due time).
	 */ 
	class LoginRoom : SimpleRoom
	{
		//arbitrary max amount just to demo the concept
		private const int MAX_MEMBERS = 50;
		
		public LoginRoom(TCPGameServer pOwner) : base(pOwner)
		{
		}

		protected override void addMember(TcpMessageChannel pMember)
		{
			base.addMember(pMember);
			
			//notify the client that (s)he is now in the login room, clients can wait for that before doing anything else
			RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent();
			roomJoinedEvent.room = RoomJoinedEvent.Room.LOGIN_ROOM;
			pMember.SendMessage(roomJoinedEvent);
		}

		protected override void handleNetworkMessage(ASerializable pMessage, TcpMessageChannel pSender)
		{
			if (pMessage is PlayerJoinRequest joinRequest)
			{
				handlePlayerJoinRequest(joinRequest, pSender);
			}
			else //if member sends something else than a PlayerJoinRequest
			{
				Log.LogInfo("Declining client, auth request not understood", this);

				//don't provide info back to the member on what it is we expect, just close and remove
				removeAndCloseMember(pSender);
			}
		}

		/**
		 * Tell the client he is accepted and move the client to the lobby room.
		 */
		private void handlePlayerJoinRequest (PlayerJoinRequest pMessage, TcpMessageChannel pSender)
		{
			PlayerJoinResponse playerJoinResponse;
			var userName = pMessage.name?.Trim();
			
			if (string.IsNullOrWhiteSpace(userName))
			{
				return;
			}

			var players = _server.GetPlayerInfo(p => p.userName.ToLower() == userName.ToLower());
			if (players.Count > 0) 
			{
				playerJoinResponse = new PlayerJoinResponse
				{
					result = PlayerJoinResponse.RequestResult.USERNAME_NOT_UNIQUE
				};
				pSender.SendMessage(playerJoinResponse);
				return;
			}

			var playerInfo = _server.GetPlayerInfo(pSender);
			playerInfo.userName = userName;

			Log.LogInfo("Moving new client to accepted...", this);

			playerJoinResponse = new PlayerJoinResponse();
			playerJoinResponse.result = PlayerJoinResponse.RequestResult.ACCEPTED;
			pSender.SendMessage(playerJoinResponse);

			removeMember(pSender);
			
			_server.GetLobbyRoom().AddMember(pSender);
		}

	}
}
