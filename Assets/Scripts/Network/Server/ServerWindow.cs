//Sanchay Ravindiran 2020

/*
    The server window extends the server and implements
    functionality needed to process each individual message
    with a different procedure depending on its specified
    type. While the server class is responsible for abstracting
    the handling of each different client's connectivity and
    packet communication, the server window decides what to do
    with the clusters of messages within these packets and
    communicates the changes that need to be made to the states
    of every connected client after modifying its own internal
    server state.
*/

using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[System.Obsolete]
public class ServerWindow : Server
{
    [Range(0, .15f)]
    [SerializeField] private float sendRate;

    private Dictionary<int, ServerPlayer> serverPlayers = new Dictionary<int, ServerPlayer>();
    private bool beastAlive;

    private class ServerPlayer
    {
        public PlayerSpawn Spawn;
        public PlayerState State = new PlayerState();
        public List<PlayerItem> Items = new List<PlayerItem>();
    }

    private IEnumerator Start()
    {
        Debugging = true;
        On();

        for (; ; )
        {
            List<PlayerState> playerStates = new List<PlayerState>();
            foreach (ServerPlayer serverPlayer in serverPlayers.Values)
            {
                playerStates.Add(serverPlayer.State);
            }
            Broadcast(playerStates.ToArray(), Unreliable);
            yield return new WaitForSeconds(sendRate);
        }
    }

    protected override void UserLeft(int user)
    {
        if (!serverPlayers[user].Spawn.Human)
        {
            beastAlive = false;
        }
        serverPlayers.Remove(user);
    }

    protected override void UserMessage(Message message)
    {
        switch (message.MessageType)
        {
            case Type.PlayerSpawn:
                AddPlayerToReferenceAndBroadcast((PlayerSpawn)message);
                break;
            case Type.PlayerState:
                PlayerState playerState = (PlayerState)message;
                serverPlayers[playerState.User].State = playerState;
                break;
            case Type.PlayerItemAdd:
                PlayerItemAdd itemAdd = (PlayerItemAdd)message;
                PlayerItem item = new PlayerItem
                {
                    ItemID = itemAdd.ItemID,
                    ItemIndex = 0
                };
                serverPlayers[itemAdd.User].Items.Add(item);
                Enqueue(itemAdd);
                break;
            case Type.PlayerItemEquip:
                PlayerItemEquip itemEquip = (PlayerItemEquip)message;
                serverPlayers[itemEquip.User].Spawn.EquippedItemIndex = itemEquip.ItemIndex;
                Enqueue(itemEquip);
                break;
            case Type.PlayerItemTrigger:
                PlayerItemTrigger itemTrigger = (PlayerItemTrigger)message;
                serverPlayers[itemTrigger.User].Spawn.EquippedItemTrigger = itemTrigger.ItemTrigger;
                Enqueue(itemTrigger);
                break;
            case Type.PlayerBeastGrab:
                Enqueue(message);
                break;
            case Type.PlayerDamage:
                Enqueue(message);
                break;
            case Type.PlayerDeath:
                Enqueue(message);
                StartCoroutine(RevivePlayer(message.User));
                break;
            default:
                break;
        }
    }

    private IEnumerator RevivePlayer(int deadUser)
    {
        ServerPlayer deadPlayer = serverPlayers[deadUser];

        yield return new WaitForSeconds(3);

        if (deadPlayer.Spawn.Human)
        {
            deadPlayer.Items.Clear();
            deadPlayer.Spawn.Items = deadPlayer.Spawn.Items.ToArray();

            PlayerRevive playerRevive = new PlayerRevive
            {
                User = deadUser
            };

            Enqueue(playerRevive);
        }
        else
        {
            PlayerRevive playerRevive = new PlayerRevive
            {
                User = deadUser
            };

            Enqueue(playerRevive);
        }
    }

    private void AddPlayerToReferenceAndBroadcast(PlayerSpawn newPlayerSpawn)
    {
        if (serverPlayers.ContainsKey(newPlayerSpawn.User))
        {
            return;
        }


        #region First Beast Picker

        if (Users.Count.Equals(1) && !beastAlive) //if 2nd user and no beast is alive/ selected
        {
            print("Making beast");
            newPlayerSpawn.Human = false;
            beastAlive = true;
        }
        else
        {
            newPlayerSpawn.Human = true;
        }

        ServerPlayer newServerPlayer = new ServerPlayer
        {
            Spawn = newPlayerSpawn,
        };

        #endregion

        serverPlayers.Add(newPlayerSpawn.User, newServerPlayer);

        List<PlayerSpawn> playerSpawns = new List<PlayerSpawn>();
        foreach (ServerPlayer serverPlayer1 in serverPlayers.Values)
        {
            serverPlayer1.Spawn.X = serverPlayer1.State.X;
            serverPlayer1.Spawn.Y = serverPlayer1.State.Y;
            serverPlayer1.Spawn.Items = serverPlayer1.Items.ToArray();
            playerSpawns.Add(serverPlayer1.Spawn);
        }

        PlayerSpawn[] playerSpawnArray = playerSpawns.ToArray();

        Send(newPlayerSpawn.User, playerSpawnArray, Reliable);

        for (int i = 0; i < Users.Count; i++)
        {
            if (!Users[i].Equals(newPlayerSpawn.User))
            {
                Send(Users[i], playerSpawnArray, Reliable);
            }
        }
    }

}
