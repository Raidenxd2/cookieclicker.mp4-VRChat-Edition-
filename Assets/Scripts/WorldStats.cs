
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class WorldStats : UdonSharpBehaviour
{

    //Tick
    public int TicksPerSecond = 1;
    private float _t;

    //UI
    public Text playerCountText;
    public Text playersText;
    
    //VRCPlayerApi
    VRCPlayerApi[] players = new VRCPlayerApi[20];

    //Other
    string joinedPlayers;

    void Start()
    {
        
    }
    
    void Update()
    {
        
        float dur = 1f / this.TicksPerSecond;
        _t += Time.deltaTime;
        while(_t >= dur)
        {
            _t -= dur;
            var playersInt = VRCPlayerApi.GetPlayerCount();
            playerCountText.text = "Player Count: " + playersInt;
            VRCPlayerApi.GetPlayers(players);
            joinedPlayers = "";
            
            foreach(VRCPlayerApi player in players) 
            {
                if(player == null) continue;
                joinedPlayers = joinedPlayers + ", " + player.displayName;
            }
            playersText.text = "Players: " + joinedPlayers;
        }
    }
}
